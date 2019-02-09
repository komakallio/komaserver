/*
 * Copyright (c) 2017 Jari Saukkonen
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
'use strict';

const _ = require('underscore');

const express = require('express');
const expressLogging = require('express-logging');
const logger = require('logops');
logger.setLevel('DEBUG');

const redis = require('redis');
const roofMotor = require('./roofmotor');
const request = require('request');
const roundTo = require('round-to');
const config = require('./config');

const app = express();
app.use(expressLogging(logger));

const REDIS_PORT = process.env.REDIS_PORT || 6379;
const redisClient = redis.createClient(REDIS_PORT);

const defaultRoofState = { openRequestedBy:[], users:{} };

var physicalState = "STOPPED";
var lastRoofData = {};
var lastRoofReportTime = 0;

redisClient.on("error", function(err) {
    console.log("Redis error: " + err);
});

roofMotor.setEncoderCallback(function(encoder) {
    redisClient.publish('roof-encoder', encoder);
});

roofMotor.setCurrentCallback(function(roofCurrent, lockCurrent) {
    redisClient.publish('roof-current', JSON.stringify({ 'roof': roofCurrent, 'lock': lockCurrent }));
});

roofMotor.setStateCallback(function(state) {
    logger.info('roof is ' + state);
    switch (state) {
        case "STOPPED": break;
        case "OPEN": {
            redisClient.get('roof-state', function(error, result) {
               let roofState = JSON.parse(result) || defaultRoofState;
                roofState.openRequestedBy.forEach((user) => { roofState.users[user] = true });
                roofState.openRequestedBy = [];
                redisClient.set('roof-state', JSON.stringify(roofState));
           });
           break;
        }
        case "CLOSED": {
            redisClient.get('roof-state', function(error, result) {
                let roofState = JSON.parse(result) || defaultRoofState;
                roofState.users = {};
                redisClient.set('roof-state', JSON.stringify(roofState));
            });
            break;
        }
        case "OPENING": break;
        case "CLOSING": break;
        case "STOPPING": break;
        case "ERROR": break;
    }

    physicalState = state;
});

function batteryReporter() {
    var status = roofMotor.status();
    var data = {
        'Type': 'Battery',
        'Battery': {
            'Voltage': [ roundTo(parseFloat(status['BATTERYVOLTAGE']), 2), 'V' ],
            'Temperature': [ roundTo(parseFloat(status['TEMP1']), 1) , 'C' ]
        }
    };

    request.post({
        url: 'http://localhost:9001/api',
        body: data,
        json: true
    }, function (error, response) {
        if (!response || response.statusCode != 200) {
            logger.warn(error);
        }
    });

    setTimeout(batteryReporter, 30000);
}

function roofReporter() {
    var status = roofMotor.status();
    var data = {
        'Type': 'Roof',
        'Roof': {
            'State': status['ROOF']
        }
    };

    if (!_.isEqual(data, lastRoofData) || (Date.now() - lastRoofReportTime) > config.ROOF_REPORT_INTERVAL) {
        lastRoofReportTime = Date.now();
        request.post({
            url: 'http://localhost:9001/api',
            body: data,
            json: true
        }, function (error, response) {
            if (!response || response.statusCode != 200) {
                logger.warn(error);
            }
            lastRoofData = data;
        });
    }

    setTimeout(roofReporter, 1000);
}

app.param('user', function(req, res, next, user) {
    req.user = user;
    next();
});

app.all('/roof/*', function(req, res, next) {
     redisClient.get('roof-state', function(error, result) {
        if (error) {
            logger.error('error reading redis: ' + error + ' result: ' + result);
            return res.status(500).end();
        }
        req.roofState = JSON.parse(result) || defaultRoofState;
        var state = JSON.stringify(req.roofState);
        next();
        var newState = JSON.stringify(req.roofState);
        if (!_.isEqual(newState, state)) {
            redisClient.set('roof-state', newState);
        }
    });
});

app.get('/roof/:user', function(req, res) {
    var reportedState;
    if (physicalState == "OPENING" || physicalState == "CLOSING" || physicalState == "ERROR" || physicalState == "STOPPING" || physicalState == "STOPPED") {
        reportedState = physicalState;
    } else {
        reportedState = req.roofState.users[req.user] ? "OPEN" : "CLOSED";
    }
    res.json({
        state: reportedState,
        open: req.roofState.users[req.user] ? true : false,
    });
});

app.post('/roof/:user/open', function(req, res) {
    switch (physicalState) {
        case "OPEN": {
            // no need to move roof, just mark as open unless we are closing down
            req.roofState.users[req.user] = true;
            break;
        }
        case "OPENING": {
            // roof is already opening; register us to openers list
            if (!req.roofState.openRequestedBy.includes(req.user)) {
                req.roofState.openRequestedBy.push(req.user);
            }
            break;
        }
        case "STOPPED":
        case "CLOSED": {
            // physical roof is closed; register us to openers list and open physical roof
            req.roofState.openRequestedBy.push(req.user);
            logger.info('open physical roof');
            roofMotor.open();
            break;
        }
        case "CLOSING": {
            // register us to openers list, so that roof will be opened again
            req.roofState.openRequestedBy.push(req.user);
            break;
        }
        case "ERROR": {
            res.status(400).json({message:"ROOF IN ERROR",error:true});
            return;
        }
    }
    res.status(200).json({message:"OK"});
});

app.post('/roof/:user/close', function(req, res) {
    var otherusers = _.any(_.mapObject(req.roofState.users), function(open, user) { 
        return user != req.user && open;
    });

    switch (physicalState) {
        case "STOPPED":
        case "OPEN": {
            // mark us closed; if we are the last user close the roof
            req.roofState.users[req.user] = false;
            if (!otherusers) {
                logger.info('close physical roof');
                roofMotor.close();
            }
            break;
        }
        case "OPENING": {
            // roof opening; remove our open request if we have one and mark us closed
            req.roofState.users[req.user] = false;
            req.roofState.openRequestedBy = _.without(req.roofState.openRequestedBy, req.user);
            break;
        }
        case "CLOSED": {
            // physical roof is closed and we should be too; do nothing
            break;
        }
        case "CLOSING": {
            // already closed; no need to do anything
            break;
        }
        case "ERROR": {
            res.status(400).json({message:"ROOF IN ERROR",error:true});
            return;
        }
    }
    res.status(200).json({message:"OK"});
});

app.post('/roof/:user/stop', function(req, res) {
    roofMotor.stop();
    req.roofState.openRequestedBy = [];
    req.roofState.users = {};
    res.status(200).json({message:"OK"});
});

app.get('/motor/status', function(req, res) {
    res.json({
        power: roofMotor.powerusage(),
        lockpower: roofMotor.lockpowerusage(),
        status: roofMotor.statusline(),
        log: roofMotor.loglines()
    });
});

app.post('/motor/open', function(req, res) {
    roofMotor.open();
    res.json({success:true});
});

app.post('/motor/unlock', function(req, res) {
    roofMotor.unlock();
    res.json({success:true});
});

app.post('/motor/close', function(req, res) {
    roofMotor.close();
    res.json({success:true});
});

app.post('/motor/lock', function(req, res) {
    roofMotor.lock();
    res.json({success:true});
});

app.post('/motor/lockcurrent', function(req, res) {
    roofMotor.setlockcurrent(req.query.milliamps);
    res.json({success:true});
});

app.post('/motor/roofcurrent', function(req, res) {
    roofMotor.setroofcurrent(req.query.milliamps);
    res.json({success:true});
});

app.post('/motor/stop', function(req, res) {
    roofMotor.stop();
    res.json({success:true});
});

app.use(express.static('public'));

var server = app.listen(9000, function() {
    var host = server.address().address;
    var port = server.address().port;

    // wait for the roof controller to initialize and report current state
    setTimeout(() => {
        batteryReporter();
        roofReporter();
    }, 5000);

    logger.info('koma-roof-server listening at http://%s:%s', host, port);
    redisClient.get('roof-state', (error, result) => {
        logger.info('Current roof state: ' + result);
    });
}).on('error', function(err) {
    logger.error('on error handler');
    logger.error(err);
});

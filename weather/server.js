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

const express = require('express');
const expressLogging = require('express-logging');
const logger = require('logops');
const bodyParser = require('body-parser');
logger.setLevel('DEBUG');
const fs = require('fs');
const redis = require('redis');
const Promise = require('bluebird');
const REDIS_PORT = process.env.REDIS_PORT || 6379;

Promise.promisifyAll(redis.RedisClient.prototype);

const app = express();
const redisClient = redis.createClient(REDIS_PORT);

redisClient.on("error", function (err) {
    console.log("Redis error " + err);
});

app.use(expressLogging(logger));
app.use(express.static('public'));
app.use(bodyParser.json());

function saveData(type, req, res) {
    req.body.Timestamp = 'Timestamp' in req.body ? parseInt(req.body.Timestamp) : Date.now();
    redisClient.zadd(type, req.body.Timestamp, JSON.stringify(req.body), function(err, reply) {
        if (err) {
            logger.error(err);
            res.status(500).send(err);
        } else {
            res.sendStatus(200);
        }
    });
}

function sendLatestData(type, req, res) {
    if (req.query.since) {
        redisClient.zrangebyscore(type, req.query.since, '+inf', 'WITHSCORES', function(err, reply) {
            if (err) {
                logger.error(err);
                res.status(500).send(err);
            } else {
                var items = [];
                for (var i = 0; i < reply.length/2; i++) {
                    var data = JSON.parse(reply[i*2]);
                    data.Timestamp = parseInt(reply[i*2+1]);
                    items.push(data)
                }
                res.status(200).send(items);
            }
        });
    } else {
        redisClient.zrevrange(type, 0, 1, 'WITHSCORES', function(err, reply) {
            if (err) {
                logger.error(err);
                res.status(500).send(err);
            } else {
                var data = JSON.parse(reply[0]);
                data.Timestamp = parseInt(reply[1]);
                res.status(200).send(data);
            }
        });
    }
}

function dewpoint(humidity, temperature) {
    var a, b;
    if (temperature >= 0) {
        a = 7.5;
        b = 237.3;
    } else {
        a = 7.6;
        b = 240.7;
    }

    var sdd = 6.1078 * Math.pow(10, (a * temperature) / (b + temperature));
    var dd = sdd * (humidity / 100);
    var v = Math.log(dd / 6.1078) / Math.log(10);
    return (b * v) / (a - v);
}

function cloudcover(sky, ambient) {
    // less than -5 delta => cloudy
    // more than -15 delta => clear
    // interpolate intermediate values linearly
    var cover = 100*(1.0 - (ambient-sky-5) / 10);
    if (cover < 0) return 0;
    if (cover > 100) return 100;
    return Math.round(cover);
}

app.post('/api', function(req, res) {
    if (req.body.Type == 'PTU') {
        saveData('ptu', req, res);
    } else if (req.body.Type == 'Wind') {
        saveData('wind', req, res);
    } else if (req.body.Type == 'Rain') {
        saveData('rain', req, res);
    } else if (req.body.Type == 'RainTrigger') {
        saveData('raintrigger', req, res);
    } else if (req.body.Type == 'Interior') {
        saveData('interior', req, res);
    } else if (req.body.Type == 'Status') {
        saveData('status', req, res);
    } else if (req.body.Type == 'Radar') {
        saveData('radar', req, res);
    } else if (req.body.Type == 'Cloud') {
        saveData('cloud', req, res);
    } else if (req.body.Type == 'CPU') {
        saveData('cpu', req, res);
    } else if (req.body.Type == 'Battery') {
        saveData('battery', req, res);
    } else if (req.body.Type == 'Roof') {
        saveData('roof', req, res);
    } else if (req.body.Type == 'UPS') {
        saveData('ups', req, res);
    } else {
        res.sendStatus(400);
        return;
    }
});

const supportedQueryTypes = ['ptu', 'wind', 'rain', 'raintrigger', 'interior', 'status', 'radar', 'cloud', 'cpu', 'battery', 'roof', 'ups'];
supportedQueryTypes.forEach(type => {
    app.get('/api/' + type, function(req, res) {
        sendLatestData(type, req, res);
    });
});

app.get('/api/weather', function(req, res) {
    Promise.join(
        redisClient.zrevrangeAsync('ptu', 0 ,0),
        redisClient.zrevrangeAsync('wind', 0, 0),
        redisClient.zrevrangeAsync('rain', 0, 0),
        redisClient.zrevrangeAsync('cloud', 0, 0),

        function(...replies) {
            if (replies.find(reply => reply.length === 0)) {
                logger.error('Reading Redis failed');
                res.status(500).send('{"error":"Reading redis failed"}');
                return;
            }

            var ptu = JSON.parse(replies[0][0]),
                wind = JSON.parse(replies[1][0]),
                rain = JSON.parse(replies[2][0]),
                cloud = JSON.parse(replies[3][0]);

            var data = {
                temperature: ptu.PTU.Temperature.Ambient[0],
                humidity: ptu.PTU.Humidity[0],
                dewpoint: parseInt(dewpoint(ptu.PTU.Humidity[0], ptu.PTU.Temperature.Ambient[0])*100)/100,
                pressure: ptu.PTU.Pressure[0],
                windspeed: wind.Wind.Speed.average[0],
                windgust: wind.Wind.Speed.limits[1][0],
                winddir: wind.Wind.Direction.average[0],
                rainrate: rain.Rain.Rain.Intensity[0],
                cloudcover: cloudcover(cloud.Cloud.Sky, ptu.PTU.Temperature.Ambient[0]),
                skytemperature: cloud.Cloud.Sky,
                skyquality: 0
            };
            res.json(data);
        }
    ).catch(function(err) {
        logger.error('Reading Redis failed', err);
        res.status(500).send('{"error":"Reading redis failed: ' + err + '"}');
    });
});

var server = app.listen(9001, function() {
    var host = server.address().address;
    var port = server.address().port;

    logger.info('koma-weather-server listening at http://%s:%s', host, port);
}).on('error', function(err) {
    logger.error(err);
});

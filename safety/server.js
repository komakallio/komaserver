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

const express = require('express');
const expressLogging = require('express-logging');
const logger = require('logops');
const bodyParser = require('body-parser');
logger.setLevel('DEBUG');
const fs = require('fs');
const redis = require('redis');
const Promise = require('bluebird');
const REDIS_PORT = process.env.REDIS_PORT || 6379;
const SunCalc = require('suncalc');
const roundTo = require('round-to');

const latitude = 60.172867;
const longitude = 24.388552;

Promise.promisifyAll(redis.RedisClient.prototype);

const app = express();
const redisClient = redis.createClient(REDIS_PORT);

var lastSafe = true;

redisClient.on("error", function (err) {
    console.log("Redis error " + err);
});

app.use(expressLogging(logger));
app.use(express.static('public'));
app.use(bodyParser.json());

app.get('/safety', function(req, res) {

    Promise.join(
        redisClient.zrevrangeAsync('ptu', 0 ,0),
        redisClient.zrevrangeAsync('rain', 0, 0),
        redisClient.zrevrangeAsync('raintrigger', 0, 0),
        redisClient.zrevrangeAsync('radar', 0, 0),
        redisClient.zrevrangeAsync('ups', 0, 0),
        redisClient.zrevrangeAsync('interior', 0, 0),

        function(...replies) {
            if (replies.find(reply => reply.length === 0)) {
                logger.error('Reading Redis failed');
                res.status(500).send('{"error":"Reading redis failed"}');
                return;
            }

            var ptu = JSON.parse(replies[0][0]),
                rain = JSON.parse(replies[1][0]),
                raintrigger = JSON.parse(replies[2][0]),
                radar = JSON.parse(replies[3][0]),
                ups = JSON.parse(replies[4][0]),
                interior = JSON.parse(replies[5][0]);

            var btemp = ptu.PTU.Temperature.Ambient[0] > -20;
            var braintrigger = raintrigger.RainTrigger.Rain == 0;
            var brainintensity = rain.Rain.Rain.Intensity[0] == 0;
            var bradar10 = radar.Radar["10km"][0] < 0.5;
            var bradar30 = radar.Radar["30km"][0] < 0.5;
            var bradar50 = radar.Radar["50km"][0] < 0.5;
            var bsun = SunCalc.getPosition(new Date(), latitude, longitude).altitude*180/Math.PI < -5;
            var bupscharge = ups.UPS.BCHARGE[0] >= 50;
            var benclosuretemp = interior.Interior.EnclosureTemp[0] > -15;

            var safe = btemp && braintrigger && brainintensity && bsun && bradar30 && bupscharge && benclosuretemp;
            var safetyDetails = {
                temperature: { value: ptu.PTU.Temperature.Ambient[0], safe: btemp },
                rainintensity: { value: rain.Rain.Rain.Intensity[0], safe: brainintensity },
                raintrigger: { value: raintrigger.RainTrigger.Rain, safe: braintrigger },
                rainradar10km: { value: radar.Radar["10km"][0], safe: bradar10 },
                rainradar30km: { value: radar.Radar["30km"][0], safe: bradar30 },
                rainradar50km: { value: radar.Radar["50km"][0], safe: bradar50 },
                sunaltitude: { value: roundTo(SunCalc.getPosition(new Date(), latitude, longitude).altitude*180/Math.PI, 2), safe: bsun },
                moonaltitude: { value: roundTo(SunCalc.getMoonPosition(new Date(), latitude, longitude).altitude*180/Math.PI, 2), safe: true },
                upscharge: { value: ups.UPS.BCHARGE[0], safe: bupscharge },
                enclosuretemp: { value: interior.Interior.EnclosureTemp[0], safe: benclosuretemp }
            };

            if (!safe && lastSafe) {
                logger.info('Condition now unsafe: ' + JSON.stringify(safetyDetails));
            }
            lastSafe = safe;

            var data = {
                safe: safe,
                details: safetyDetails
            };
            res.json(data);
        }
    ).catch(function(err) {
        logger.error('Reading Redis failed', err);
        res.status(500).send('{"error":"Reading redis failed: ' + err +  '"}');
    });
});

var server = app.listen(9002, function() {
    var host = server.address().address;
    var port = server.address().port;

    logger.info('koma-safety-server listening at http://%s:%s', host, port);
}).on('error', function(err) {
    logger.error(err);
});

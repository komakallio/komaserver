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

const redis = require('redis');
const Promise = require('bluebird');
const request = require('request');
const REDIS_PORT = process.env.REDIS_PORT || 6379;
Promise.promisifyAll(redis.RedisClient.prototype);
const redisClient = redis.createClient(REDIS_PORT);
const config = require('./config');

var updateTimestamps = {};

function updateTick() {
    redisClient.zrevrangeAsync('interior', 0 ,0).then(reply => {
        var interior = reply[0];
        if (interior.Timestamp == updateTimestamps[interior.Type]) {
            return;
        }

        var data = [
            { 'id': 'enclosure.temperature', 'dataPoints': [ { 'timestamp': interior.Timestamp, 'value': interior.Interior.EnclosureTemp[0] } ] },
            { 'id': 'interior.temperature', 'dataPoints': [ { 'timestamp': interior.Timestamp, 'value': interior.Interior.InteriorTemp[0] } ] },
            { 'id': 'interior.pressure', 'dataPoints': [ { 'timestamp': interior.Timestamp, 'value': interior.Interior.InteriorPressure[0] } ] },
            { 'id': 'interior.humidity', 'dataPoints': [ { 'timestamp': interior.Timestamp, 'value': interior.Interior.InteriorHumidity[0] } ] }
        ];

        request.post({
            url: config.hawkular + '/gauges/raw',
            headers: { 'Hawkular-Tenant': 'Komakallio' },
            auth: { 'username': config.username, 'password': config.password },
            json: data
        }, (error, response, body) => {
            if (!error && response.statusCode == 200) {
                updateTimestamps[interior.Type] = interior.Timestamp;
                console.log('Put ' + interior.Timestamp + ' ' + interior.Type);
            } else {
                console.log('Error: ' + body);
            }
        });
    });
    setTimeout(updateTick, 5000);
}

setTimeout(updateTick, 5000);

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
const logger = require('logops');
const REDIS_PORT = process.env.REDIS_PORT || 6379;
Promise.promisifyAll(redis.RedisClient.prototype);
const redisClient = redis.createClient(REDIS_PORT);
const config = require('./config');
const dewpoint = require('dewpoint');
const xdp = new dewpoint(config.heightAboveSeaLevel);

var updateTimestamps = {};

const gaugeTypes = ['ptu', 'wind', 'rain', 'raintrigger', 'interior', 'status', 'radar', 'cloud', 'cpu', 'battery', 'roof', 'ups'];
const counterTypes = ['rain'];
const stringTypes = ['roof', 'status'];

function convertGaugeDataForType(data) {
    switch(data.Type) {
        case 'PTU':
            var dewp = xdp.Calc(data.PTU.Temperature.Ambient[0], data.PTU.Humidity[0]).dp;
            return [
                { 'id': 'weather.temperature', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.PTU.Temperature.Ambient[0] } ] },
                { 'id': 'wxt520.temperature', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.PTU.Temperature.Internal[0] } ] },
                { 'id': 'weather.pressure', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.PTU.Pressure[0] } ] },
                { 'id': 'weather.humidity', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.PTU.Humidity[0] } ] },
                { 'id': 'weather.dewpoint', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': dewp ? parseInt(dewp*100)/100 : 0 } ] }
            ];
        case 'Interior':
            var dewp = xdp.Calc(data.Interior.InteriorTemp[0], data.Interior.InteriorHumidity[0]).dp;
            return [
                { 'id': 'enclosure.temperature', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Interior.EnclosureTemp[0] } ] },
                { 'id': 'interior.temperature', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Interior.InteriorTemp[0] } ] },
                { 'id': 'interior.pressure', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Interior.InteriorPressure[0] } ] },
                { 'id': 'interior.humidity', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Interior.InteriorHumidity[0] } ] },
                { 'id': 'interior.dewpoint', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': dewp ? parseInt(dewp*100)/100 : 0 } ] }
            ];
        case 'Battery':
            return [
                { 'id': 'roof.battery.voltage', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Battery.Voltage[0] } ] },
                { 'id': 'roof.battery.temperature', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Battery.Temperature[0] } ] }
            ];
        case 'CPU':
            return [
                { 'id': 'raspberry.cpu.temperature', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.CPU.Temperature[0] } ] },
                { 'id': 'raspberry.gpu.temperature', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.GPU.Temperature[0] } ] }
            ];
        case 'Wind':
            return [
                { 'id': 'weather.wind.speed', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Wind.Speed.average[0] } ] },
                { 'id': 'weather.wind.direction', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Wind.Direction.average[0] } ] }
            ];
        case 'Status':
            return [
                { 'id': 'wxt520.heater.temperature', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Status.Heating.Temperature[0] } ] },
                { 'id': 'wxt520.heater.voltage', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Status.Voltages.Heating[0] } ] },
                { 'id': 'wxt520.supply.voltage', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Status.Voltages.Supply[0] } ] }
            ];
        case 'RainTrigger':
            return [
                { 'id': 'weather.rain.intensity2', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.RainTrigger.Intensity } ] },
                { 'id': 'weather.rain.trigger', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.RainTrigger.Rain } ] }
            ];
        case 'Rain':
            return [
                { 'id': 'weather.rain.intensity', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Rain.Rain.Intensity[0] } ] }
            ];
        case 'Radar':
            return [
                { 'id': 'weather.radar.50km', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Radar['50km'][0] } ] },
                { 'id': 'weather.radar.30km', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Radar['30km'][0] } ] },
                { 'id': 'weather.radar.10km', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Radar['10km'][0] } ] },
                { 'id': 'weather.radar.3km', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Radar['3km'][0] } ] },
                { 'id': 'weather.radar.1km', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Radar['1km'][0] } ] },
                { 'id': 'weather.radar.distance', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Radar['rain_distance'][0] !== undefined ? data.Radar['rain_distance'][0] : 9999 } ] }
            ];
        case 'Roof':
            var roofStateToValue = { 'CLOSED':0, 'CLOSING':1, 'OPENING':2, 'OPEN':3, 'ERROR':-1 };
            return [
                { 'id': 'roof.state', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': roofStateToValue[data.Roof.State] } ] }
            ]
        case 'UPS':
            return [
                { 'id': 'ups.battery.charge', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.UPS.BCHARGE[0] } ] },
                { 'id': 'ups.battery.voltage', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.UPS.BATTV[0] } ] },
                { 'id': 'ups.load', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.UPS.LOADPCT[0] } ] },
                { 'id': 'ups.line.voltage', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.UPS.LINEV[0] } ] },
                { 'id': 'ups.timeleft', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.UPS.TIMELEFT[0] } ] },
                { 'id': 'ups.timeonbattery', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.UPS.TONBATT[0] } ] }
            ];
        default:
            return []
    }
}

function convertCounterDataForType(data) {
    switch(data.Type) {
        case 'Rain':
            return [
                { 'id': 'weather.rain.accumulation', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Rain.Rain.Accumulation[0] } ] }
            ];
        default:
            return [];
    }
}

function convertStringDataForType(data) {
    switch(data.Type) {
        case 'Roof':
            return [
                { 'id': 'roof.state', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Roof.State } ] }
            ];
        case 'Status':
            return [
                { 'id': 'wxt520.heater.status', 'dataPoints': [ { 'timestamp': data.Timestamp, 'value': data.Status.Heating.Status } ] }
            ];
        default:
            return [];
    }
}

function postToHawkular(type, hawkulardata) {
    if (hawkulardata.length == 0) {
        return;
    }

    request.post({
        url: config.endpoint + '/' + type + '/raw',
        headers: { 'Hawkular-Tenant': config.tenant },
        auth: { 'username': config.username, 'password': config.password },
        json: hawkulardata
    }, (error, response, body) => {
        if (!error && response.statusCode == 200) {
            logger.info('Sent ' + hawkulardata.length + ' ' + type + ': ' + hawkulardata.reduce((l, r) => l + r.id + ' ', ''));
        } else {
            logger.error('Error: ' + JSON.stringify(body));
        }
    });
}

function updateTick() {
    var redisQueries = gaugeTypes.map(type => redisClient.zrevrangeAsync(type, 0, 0));
    Promise.all(redisQueries).then(replies => {
        var redisdata = replies
            .filter(reply => reply.length > 0)
            .map(reply => JSON.parse(reply[0]))
            .filter(data => data.Timestamp != updateTimestamps[data.Type]);
        var gaugedata = redisdata
            .map(data => convertGaugeDataForType(data))
            .reduce((flat, arr) => flat.concat(arr), []);
        var counterdata = redisdata
            .map(data => convertCounterDataForType(data))
            .reduce((flat, arr) => flat.concat(arr), []);
        var stringdata = redisdata
            .map(data => convertStringDataForType(data))
            .reduce((flat, arr) => flat.concat(arr), []);

        postToHawkular('gauges', gaugedata);
//        postToHawkular('counters', counterdata);
//        postToHawkular('strings', stringdata);

        redisdata.forEach(data => {
            updateTimestamps[data.Type] = data.Timestamp;
        });
    });
    setTimeout(updateTick, 5000);
}

updateTick();

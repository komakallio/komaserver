/*
 * Copyright (c) 2019 Jari Saukkonen
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

const types = ['ptu', 'wind', 'rain', 'raintrigger', 'interior', 'status', 'radar', 'cloud', 'cpu', 'battery', 'roof', 'ups', 'ruuvi_jari', 'ruuvi_samuli', 'safety', 'sqm', 'allsky'];

function convertDataForType(data) {
    switch(data.Type) {
        case 'PTU':
            var dewp = xdp.Calc(data.PTU.Temperature.Ambient[0], data.PTU.Humidity[0]).dp;
            return `Weather ` +
                `temperature=${data.PTU.Temperature.Ambient[0]},` +
                `pressure=${data.PTU.Pressure[0]},` +
                `humidity=${data.PTU.Humidity[0]},` +
                `dewpoint=${dewp ? parseInt(dewp*100)/100 : 0}` +
                ` ${data.Timestamp}\n` +
                `WXT520 temperature=${data.PTU.Temperature.Internal[0]} ${data.Timestamp}\n`;

        case 'Interior':
            var dewp = xdp.Calc(data.Interior.InteriorTemp[0], data.Interior.InteriorHumidity[0]).dp;
            return `Interior ` +
                `temperature=${data.Interior.InteriorTemp[0]},` +
                `pressure=${data.Interior.InteriorPressure[0]},` +
                `humidity=${data.Interior.InteriorHumidity[0]},` +
                `dewpoint=${dewp ? parseInt(dewp*100)/100 : 0}` +
                ` ${data.Timestamp}\n` +
                `Enclosure temperature=${data.Interior.EnclosureTemp[0]} ${data.Timestamp}\n`;


        case 'Battery':
            return `Roof ` +
                `battery.voltage=${data.Battery.Voltage[0]},` +
                `battery.temperature=${data.Battery.Temperature[0]}` +
                ` ${data.Timestamp}\n`;

        case 'CPU':
            return `RaspberryPi ` +
                `cpu.temperature=${data.CPU.Temperature[0]},` +
                `gpu.temperature=${data.GPU.Temperature[0]}` +
                ` ${data.Timestamp}\n`;

        case 'Wind':
            return `Weather ` +
                `wind.speed=${data.Wind.Speed.average[0]},` +
                `wind.direction=${data.Wind.Direction.average[0]}` +
                ` ${data.Timestamp}\n`;

        case 'Status':
            return `WXT520 ` +
                `heater.temperature=${data.Status.Heating.Temperature[0]},` +
                `heater.voltage=${data.Status.Voltages.Heating[0]},` +
                `supply.voltage=${data.Status.Voltages.Supply[0]}` +
                ` ${data.Timestamp}\n`;

        case 'RainTrigger':
            return `Weather ` +
                `rain.intensity2=${data.RainTrigger.Intensity},` +
                `rain.trigger=${data.RainTrigger.Rain}` +
                ` ${data.Timestamp}\n`;

        case 'Rain':
            return `Weather rain.intensity=${data.Rain.Rain.Intensity[0]} ${data.Timestamp}\n`;

        case 'SQM':
            return `SQM sqm.magnitude=${data.SQM.SQM[0]} sqm.frequency=${data.SQM.Frequency[0]} sqm.temperature=${data.SQM.Temperature[0]} ${data.Timestamp}\n`;

        case 'Allsky':
            return `Allsky allsky.temperature=${data.Allsky.Temperature[0]} allsky.humidity=${data.Allsky.Humidity[0]} allsky.pressure=${data.Allsky.Pressure[0]} ${data.Timestamp}\n`;

        case 'Radar':
            return `Weather ` +
                `radar.50km=${data.Radar['50km'][0]},` +
                `radar.30km=${data.Radar['30km'][0]},` +
                `radar.10km=${data.Radar['10km'][0]},` +
                `radar.3km=${data.Radar['3km'][0]},` +
                `radar.1km=${data.Radar['1km'][0]},` +
                `radar.distance=${data.Radar['rain_distance'][0] !== null ? data.Radar['rain_distance'][0] : 9999}` +
                ` ${data.Timestamp}\n`;

        case 'Roof':
            return `Roof state="${data.Roof.State}" ${data.Timestamp}\n`;

        case 'UPS':
            return `UPS ` +
                `battery.charge=${data.UPS.BCHARGE[0]},` +
                `battery.voltage=${data.UPS.BATTV[0]},` +
                `load=${data.UPS.LOADPCT[0]},` +
                `line.voltage=${data.UPS.LINEV[0]},` +
                `timeleft=${data.UPS.TIMELEFT[0]},` +
                `timeonbattery=${data.UPS.TONBATT[0]}` +
                ` ${data.Timestamp}\n`;

        case 'Ruuvi_jari':
        case 'Ruuvi_samuli':
            let name = data.Type.substring(6);
            return `Ruuvi ` +
                `${name + '.atpark'}=${data[data.Type].AtPark ? 1 : 0},` +
                `${name + '.signal'}=${data[data.Type].Signal[0]},` +
                `${name + '.temperature'}=${data[data.Type].Temperature[0]},` +
                `${name + '.humidity'}=${data[data.Type].Humidity[0]},` +
                `${name + '.pressure'}=${data[data.Type].Pressure[0]},` +
                `${name + '.voltage'}=${data[data.Type].Voltage[0]}` +
                ` ${data.Timestamp}\n`;

        case 'Safety':
            let desc = 'SAFE';
            if (!data.Safety.Safe) {
                if (!data.Safety.Details.SunAltitude)         desc = 'UNSAFE_SUNALTITUDE';
                else if (!data.Safety.Details.UPSCharge)      desc = 'UNSAFE_UPS';
                else if (!data.Safety.Details.Temperature)    desc = 'UNSAFE_OUTSIDETEMP';
                else if (!data.Safety.Details.EnclosureTemp)  desc = 'UNSAFE_ENCLOSURETEMP';
                else if (!data.Safety.Details.Radar)          desc = 'UNSAFE_RADAR';
                else if (!data.Safety.Details.RainTrigger)    desc = 'UNSAFE_RAINTRIGGER';
                else if (!data.Safety.Details.RainIntensity)  desc = 'UNSAFE_RAININTENSITY';
                else desc = 'UNSAFE';
            }
            return `Safety ` +
                `safe=${data.Safety.Safe},` +
                `safe.desc="${desc}",` +
                `reason.temperature=${!data.Safety.Details.Temperature},` +
                `reason.raintrigger=${!data.Safety.Details.RainTrigger},` +
                `reason.rainintensity=${!data.Safety.Details.RainIntensity},` +
                `reason.radar=${!data.Safety.Details.Radar},` +
                `reason.sunaltitude=${!data.Safety.Details.SunAltitude},` +
                `reason.upscharge=${!data.Safety.Details.UPSCharge},` +
                `reason.enclosuretemp=${!data.Safety.Details.EnclosureTemp}` +
                ` ${data.Timestamp}\n`;

        default:
            return '';
    }
}

function postToInfluxDb(data) {
    if (data.length == 0) {
        return;
    }

    request.post({
        url: config.endpoint,
        qs: {
            db: config.db,
            precision: 'ms'
        },
        auth: { 'username': config.username, 'password': config.password },
        body: data
    }, (error, response, body) => {
        if (!error && response.statusCode == 204) {
            logger.info('Sent ' + data.split(',').length + ' entries for ' + data.split('\n').map(x => x.split(' ')[0]).filter(x => x.length > 0));
        } else {
            logger.error('Error: ' + JSON.stringify(body) + ' data was ' + data);
        }
    });
}

function updateTick() {
    var redisQueries = types.map(type => redisClient.zrevrangeAsync(type, 0, 0));
    Promise.all(redisQueries).then(replies => {
        var redisdata = replies
            .filter(reply => reply.length > 0)
            .map(reply => JSON.parse(reply[0]))
            .filter(data => data.Timestamp != updateTimestamps[data.Type]);
        var data = redisdata
            .map(data => convertDataForType(data))
            .reduce((flat, arr) => flat + arr, '');

        postToInfluxDb(data);

        redisdata.forEach(data => {
            updateTimestamps[data.Type] = data.Timestamp;
        });
    });
    setTimeout(updateTick, 5000);
}

updateTick();

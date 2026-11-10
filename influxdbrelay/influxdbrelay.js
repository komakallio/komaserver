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

const types = ['ptu', 'wind', 'rain', 'raintrigger', 'interior', 'status', 'radar', 'cloud', 'cpu', 'battery', 'roof', 'ups', 'ruuvi_jari', 'ruuvi_samuli', 'ruuvi_lauri', 'safety', 'sqm', 'allsky'];

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
            return `SQM sqm.magnitude=${data.SQM.SQM[0]},sqm.frequency=${data.SQM.Frequency[0]},sqm.temperature=${data.SQM.Temperature[0]} ${data.Timestamp}\n`;

        case 'Allsky':
            return `Allsky allsky.temperature=${data.Allsky.Temperature[0]},allsky.humidity=${data.Allsky.Humidity[0]},allsky.pressure=${data.Allsky.Pressure[0]} ${data.Timestamp}\n`;

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
        case 'Ruuvi_lauri':
            let name = data.Type.substring(6);
            let accel = data[data.Type].Acceleration;
            return `Ruuvi ` +
                (accel ? `${name}.x=${accel[0][0]},${name}.y=${accel[0][1]},${name}.z=${accel[0][2]},` : '') +
                (data[data.Type].AtPark === null || data[data.Type].AtPark === undefined ? '' : `${name + '.atpark'}=${data[data.Type].AtPark ? 1 : 0},`) +
                (Number.isInteger(data[data.Type].Sequence) ? `${name}.sequence=${data[data.Type].Sequence}i,` : '') +
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

// Catch-up: the relay remembers, per type, the timestamp (zset score) of the
// last record InfluxDB accepted, in the Redis hash SENT_KEY. Each tick sends
// every record newer than that, thinned to one per MIN_SPACING_MS (the old
// relay sent only the newest record every 5 s, so normal density is
// unchanged). If InfluxDB or the network is down, nothing advances and the
// backlog goes out once it's reachable again, oldest first, at most
// BATCH_RECORDS per type per tick.
const SENT_KEY = 'influxdbrelay:sent';
const TICK_MS = 5000;
const MIN_SPACING_MS = 5000;
const BATCH_RECORDS = 2000;
const MAX_BACKLOG_MS = 7 * 24 * 3600 * 1000;

function postToInfluxDb(data) {
    // Resolves true when InfluxDB has taken the data (or rejected it as
    // malformed, which retrying won't fix), false to retry later.
    return new Promise(resolve => {
        if (data.length == 0) {
            return resolve(true);
        }
        request.post({
            url: config.endpoint,
            qs: {
                db: config.db,
                precision: 'ms'
            },
            auth: { 'username': config.username, 'password': config.password },
            body: data,
            timeout: 30000
        }, (error, response, body) => {
            if (!error && response.statusCode == 204) {
                const lines = data.split('\n').filter(x => x.length > 0);
                const names = [...new Set(lines.map(x => x.split(' ')[0]))];
                logger.info('Sent ' + lines.length + ' lines for ' + names);
                resolve(true);
            } else if (!error && response.statusCode == 400) {
                logger.error('Rejected: ' + JSON.stringify(body) + ' data was ' + data.substring(0, 2000));
                resolve(true);
            } else {
                logger.error('Error: ' + (error ? error.message : response.statusCode + ' ' + JSON.stringify(body)) + '; will retry');
                resolve(false);
            }
        });
    });
}

function thin(records, sentScore) {
    // records: [[json, score], ...] oldest first, all newer than sentScore.
    const out = [];
    let prev = sentScore;
    for (const [json, score] of records) {
        if (prev === null || score - prev >= MIN_SPACING_MS) {
            out.push([json, score]);
            prev = score;
        }
    }
    return out;
}

function pairs(reply) {
    const out = [];
    for (let i = 0; i + 1 < reply.length; i += 2) {
        out.push([reply[i], Number(reply[i + 1])]);
    }
    return out;
}

async function pending(type, sent, now) {
    // Records to send for one type: [lines, newest score covered] or null.
    let since = sent[type] !== undefined ? Number(sent[type]) : null;
    let reply;
    if (since === null) {
        // First run for this type: start from the newest record.
        reply = await redisClient.zrevrangeAsync(type, 0, 0, 'WITHSCORES');
    } else {
        since = Math.max(since, now - MAX_BACKLOG_MS);
        reply = await redisClient.zrangebyscoreAsync(type, '(' + since, '+inf', 'WITHSCORES', 'LIMIT', 0, BATCH_RECORDS);
    }
    const records = pairs(reply);
    if (records.length == 0) {
        return null;
    }
    let lines = '';
    for (const [json, score] of thin(records, since)) {
        try {
            lines += convertDataForType(JSON.parse(json)) || '';
        } catch (e) {
            logger.error('Bad record in ' + type + ' at ' + score + ': ' + e.message);
        }
    }
    // Skipped (thinned or bad) records count as covered too.
    return [lines, records[records.length - 1][1]];
}

async function updateTick() {
    try {
        const now = Date.now();
        const sent = (await redisClient.hgetallAsync(SENT_KEY)) || {};
        const batches = [];
        for (const type of types) {
            const p = await pending(type, sent, now);
            if (p) {
                batches.push([type, p[0], p[1]]);
            }
        }
        if (batches.length > 0) {
            const ok = await postToInfluxDb(batches.map(b => b[1]).join(''));
            if (ok) {
                const args = [];
                batches.forEach(([type, , score]) => args.push(type, String(score)));
                await redisClient.hmsetAsync(SENT_KEY, args);
            }
        }
    } catch (e) {
        logger.error('Tick failed: ' + e.message);
    }
    setTimeout(updateTick, TICK_MS);
}

if (require.main === module) {
    updateTick();
}

module.exports = { thin, pairs, convertDataForType };

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
'use strict'

const logger = require('logops');
logger.setLevel('DEBUG');
const request = require('request');
const ruuvi = require('node-ruuvitag');
const config = require('./config');

let lastData = {};
let lastUpdateTimes = {};

function dot(v1, v2) {
    return v1[0]*v2[0] + v1[1]*v2[1] + v1[2]*v2[2];
}

function angleBetween(v1, v2) {
    return Math.acos(dot(v1, v2) / (Math.sqrt(Math.abs(dot(v1,v1))) * Math.sqrt(Math.abs(dot(v2, v2)))));
}

function isParked(tag) {
    let parkVector = config.parkPositions[tag.id];
    let tagVector = [tag.accelerationX, tag.accelerationY, tag.accelerationZ];

    return angleBetween(parkVector, tagVector) < (2 * Math.PI / 180.0);
}

function needToUpdate(tag, data) {
    if (lastData[tag.id] === undefined) {
        return true;
    }
    if (lastData[tag.id] && lastData[tag.id].AtPark != data.AtPark) {
        return true;
    }
    if (lastUpdateTimes[tag.id] !== undefined &&
        Date.now() - lastUpdateTimes[tag.id] > 60*1000) {
        return true;
    }

    return false;
}

ruuvi.on('found', tag => {
    let name = config.names[tag.id];
    if (name === undefined) {
        return;
    }
    logger.info('Found RuuviTag "' + name + '", id ' + tag.id);
    tag.on('updated', tagdata => {
        tagdata.id = tag.id;
        let data = {
            Type: 'Ruuvi_' + name,
            Timestamp: Date.now()
        };
        data['Ruuvi_' + name] = {
            Name: name,
            AtPark: isParked(tagdata),
            Voltage: [ parseInt(tagdata.battery*100/1000.0)/100.0, 'V' ],
            Temperature: [ tagdata.temperature, 'C' ],
            Pressure: [ tagdata.pressure/100.0, 'hPa' ],
            Humidity: [ tagdata.humidity, '%' ],
            Signal: [ tagdata.rssi, 'dBm']
        }

        if (needToUpdate(tagdata, data['Ruuvi_' + name])) {
            lastUpdateTimes[tagdata.id] = Date.now();
            lastData[tagdata.id] = data['Ruuvi_' + name];
            console.log(JSON.stringify(data));
            request.post({
                url: 'http://localhost:9001/api',
                body: data,
                json: true
            });
        }
    });
});

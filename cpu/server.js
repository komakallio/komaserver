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

const _ = require('underscore');
const logger = require('logops');
logger.setLevel('DEBUG');
const request = require('request');
const fs = require('fs');
const exec = require('child_process').exec;

let lastUpsData = {};
let lastUpsUpdateTime = 0;

function updateTemp() {
    fs.readFile('/sys/class/thermal/thermal_zone0/temp', (err, cpuStr) => {
        if (err) throw err;
        exec('vcgencmd measure_temp', (err, gpuStr, stderr) => {
            if (err) throw err;
            let matcher = /temp=([0-9.]+)/g;
            let match = matcher.exec(gpuStr);

            let data = {
                Type: 'CPU',
                Data: {
                    CPU: [ parseInt(parseFloat(cpuStr)/100)/10.0, 'C' ],
                    GPU: [ parseFloat(match[1]), 'C' ]
                }
            };
            console.log(JSON.stringify(data));
            request.post({
                url: 'http://localhost:9001/api',
                body: data,
                json: true
            });
        });
    });
    exec('apcaccess -u', (err, stdout, stderr) => {
        if (err) throw err;

        let ups = stdout
            .split('\n')
            .map(line => line.split(":").map(part => part.trim()))
            .reduce((ob, p) => { ob[p[0]] = p[1]; return ob; }, {});

        let upsdata = {
            BCHARGE: [ parseFloat(ups.BCHARGE), '%'],
            BATTV: [ parseFloat(ups.BATTV), 'V'],
            TONBATT: [ parseInt(ups.TONBATT), 's']
        }

        let now = new Date().getTime();
        if (!_.isEqual(lastUpsData, upsdata) || (now - lastUpsUpdateTime > 15*60*1000)) {
            lastUpsUpdateTime = now;
            lastUpsData = upsdata;

            upsdata.LOADPCT = [ parseFloat(ups.LOADPCT), '%' ];
            upsdata.LINEV =[ parseFloat(ups.LINEV), 'V' ];
            upsdata.TIMELEFT = [ parseFloat(ups.TIMELEFT), 'min'];

            var data = {
                Type: 'UPS',
                Data: upsdata
            };

            console.log(JSON.stringify(data));

            request.post({
                url: 'http://localhost:9001/api',
                body: data,
                json: true
            });
        }
    });
    setTimeout(updateTemp, 10000);
}

updateTemp();

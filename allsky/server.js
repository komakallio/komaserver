/*
 * Copyright (c) 2025 Jari Saukkonen
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

const logger = require('logops');
const net = require('node:net');
const request = require('request');
const config = require('./config.js');
const BME280 = require('bme280-sensor');

const bme280 = new BME280({ i2cBusNo: 1, i2cAddress: 0x76 });

const readBMEOnce = () => {
	bme280.readSensorData().then((data) => {
		const allskydata = { 'Type': 'Allsky', 'Allsky': {
			Temperature: [ data.temperature_C, 'C' ],
			Humidity: [ data.humidity, '%' ],
			Pressure: [ data.pressure_hPa, 'hPa' ]
		}};

        	logger.info(JSON.stringify(allskydata));
		request.post({
                	url: config.KOMASERVER_API_URL,
			body: allskydata,
			json: true,
			timeout: 5000
		}).on('error', (error) => { logger.error(error); });

		setTimeout(readBMEOnce, config.READ_INTERVAL_MS);
	}).catch((err) => {
		console.log(`BME280 read error: ${err}`);
		setTimeout(readBMEOnce, config.READ_INTERVAL_MS);
	});
};

bme280.init().then(() => {
	console.log('BME280 initialization succeeded');
	readBMEOnce();
}).catch((err) => {
	console.error(`BME280 initialization failed: ${err}`);
});

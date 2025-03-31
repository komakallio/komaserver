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

function readSqmOnce() {
	const socket = net.createConnection({
		host: config.SQM_HOST,
		port: config.SQM_PORT,
		timeout: 5000
	});
	socket.on('timeout', () => { socket.destroy(); });
	socket.on('connect', () => {
		socket.write('rx\r\n');
	});
	socket.on('error', (error) => {
		logger.error(error);
		socket.destroy();
	});
	socket.on('data', (buffer) => {
		const response = buffer.toString('ascii', 0, buffer.length);
		[command, magnitude, frequency, periodCounts, periodSeconds, temperature] = response.split(',');
		socket.destroy();

            	const sqmdata = { 'Type': 'SQM', 'SQM': {}};
		sqmdata.SQM.SQM = [ parseFloat(magnitude), 'mag/arcsec^2' ];
		sqmdata.SQM.Frequency = [ parseInt(frequency), 'Hz' ];
		sqmdata.SQM.Temperature = [ parseFloat(temperature), 'C' ];

        	logger.info(JSON.stringify(sqmdata));
		request.post({
                	url: config.KOMASERVER_API_URL,
			body: sqmdata,
			json: true,
			timeout: 5000
		}).on('error', (error) => { logger.error(error); });
	});
}

readSqmOnce();
setInterval(readSqmOnce, config.READ_INTERVAL_MS);

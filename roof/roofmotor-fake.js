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

const logger = require('logops');
logger.setLevel('DEBUG');

var power = [];
for (var i = 0; i < 120; i++) {
    power.push(0);
}
var statusline = "ROOF=CLOSED,PHASE=IDLE,ENCODER=0,TEMP1=19.5,BATTERYVOLTAGE=13.2";
var status = { ROOF: 'CLOSED', PHASE: 'IDLE', ENCODER: 0, TEMP1: 19.5, BATTERYVOLTAGE: 13.2 };
var loglines = ['', '', ''];
var stateCallback;

module.exports = {
    open: () => {
        status.ROOF = 'OPENING';
        stateCallback('OPENING');
        setTimeout(function() {
            status.ROOF = 'OPEN';
            stateCallback('OPEN');
        }, 5000);
    },
    close: () => {
        status.ROOF = 'CLOSING';
        stateCallback('CLOSING');
        setTimeout(() => {
            status.ROOF = 'CLOSED';
            stateCallback('CLOSED');
        }, 5000);
    },
    stop: () => {
        status.ROOF = 'STOPPED';
        stateCallback('STOPPED');
    },
    lock: () => {},
    unlock: () => {},
    state: () => {
        return currentState;
    },
    setStateCallback: (callback) => {
        stateCallback = callback;
    },
    powerusage: () => {
        return power;
    },
    statusline: () => {
        return statusline;
    },
    status: () => {
        return status;
    },
    loglines: () => {
        return loglines;
    }
}

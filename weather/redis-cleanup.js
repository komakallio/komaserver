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

const logger = require('logops');
const redis = require('redis');
const Promise = require('bluebird');
const REDIS_PORT = process.env.REDIS_PORT || 6379;

Promise.promisifyAll(redis.RedisClient.prototype);

module.exports = {
    cleanup: (type) => {
        const redisClient = redis.createClient(REDIS_PORT);
        const cleanupTimestamp = new Date().getTime() - 1000*60*60*24*7;
        redisClient.zcount(type, cleanupTimestamp, new Date().getTime(), function(err, reply) {
            if (reply > 0) {
                redisClient.zremrangebyscore(type, 0, cleanupTimestamp, function(err, reply) {
                    if (err) {
                        logger.error(err);
                    } else {
                        logger.info('Cleaned up ' + reply + ' entries from ' + type);
                    }
                    redisClient.quit();
                });
            } else {
                logger.info('Did not clean ' + type + ' as there are no newer entries');
                redisClient.quit();
            }
        });
    },

    cleanupAll: () => {
        const supportedQueryTypes = ['ptu', 'wind', 'rain', 'raintrigger', 'interior', 'status', 'radar', 'cloud', 'cpu', 'battery', 'roof', 'ups', 'ruuvi_jari', 'ruuvi_samuli', 'safety'];
        supportedQueryTypes.forEach(type => module.exports.cleanup(type));
    }
}

module.exports.cleanupAll();

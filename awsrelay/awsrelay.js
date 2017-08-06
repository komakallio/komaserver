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
const REDIS_PORT = process.env.REDIS_PORT || 6379;
Promise.promisifyAll(redis.RedisClient.prototype);
const redisClient = redis.createClient(REDIS_PORT);
const AWS = require('aws-sdk');
const config = require('./config');

AWS.config.update({
  region: "eu-central-1"
});

AWS.config.credentials = new AWS.CognitoIdentityCredentials({
    IdentityPoolId: config.IdentityPoolId,
    RoleArn: config.RoleArn
});

var dynamodb = new AWS.DynamoDB();
var docClient = new AWS.DynamoDB.DocumentClient();

Promise.join(
    redisClient.zrevrangeAsync('ptu', 0 ,0),
    redisClient.zrevrangeAsync('battery', 0 ,0),

    function(...replies) {
        if (replies.find(reply => reply.length === 0)) {
            logger.error('Reading Redis failed');
            res.status(500).send('{"error":"Reading redis failed"}');
            return;
        }

        replies.forEach(reply => {
            var data = JSON.parse(reply[0]);
            var params = {
                Key : { 'P':'1', 'Timestamp': data.Timestamp },
                TableName : "Metrics",
                UpdateExpression: 'SET #type = :data',
                ExpressionAttributeNames: {
                    "#type" : data.Type
                },
                ExpressionAttributeValues: {
                    ":data" : data[data.Type]
                }
            };

            docClient.update(params, function(err, data) {
                if (err) {
                    console.log('Error in PutItem ' + JSON.stringify(err));
                } else {
	                console.log('Put ' + data.Type);
                }
            });
        });
    }
).catch(function(err) {
    logger.error('Reading Redis failed', err);
    res.status(500).send('{"error":"Reading redis failed: ' + err + '"}');
});


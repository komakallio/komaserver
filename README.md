# Komakallio observatory automation

## Contents

- **arduino-weather** - Arduino sketch that operates DRD11A, BME280 and DS1820 sensors
- **ascom-observingconditions** - ObservingConditions ASCOM driver
- **ascom-roof** - ASCOM dome driver for roll-off roof
- **ascom-safety** - ASCOM SafetyMonitor driver
- **aws-relay** - Relay for sending metrics data to AWS DynamoDB
- **cpu** - CPU temperature monitor for Raspberry PI
- **rain** - Observatory server that provides roof and weather data
- **hawkularrelay** - Relay for sending metrics data to a Hawkular Metrics endpoint
- **influxdbrelay** - Relay for sending metrics data to an InfluxDB endpoint
- **roof** - Multi-user roof server with an REST interface
- **ruuvi** - Monitor multiple RuuviTag devices
- **safety** - SafetyMonitor REST interface
- **weather** - Server for receiving WXT520 weather data

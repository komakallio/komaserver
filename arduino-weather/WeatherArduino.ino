#include <Adafruit_Sensor.h>
#include <Adafruit_BME280.h>
#include <DallasTemperature.h>
#include <FreqCount.h>
#include <OneWire.h>

#include "NMEASerial.h"

#define PIN_DRD11A_RAIN 4
#define PIN_DRD11A_INTENSITY A0
#define PIN_DRD11A_FREQ 5
#define PIN_ONEWIRE 12
// SCL A5
// SDA A4

static int rainFrequency = 0;
static bool hasInteriorSensor = false;
static bool hasEnclosureSensor = false;
static Adafruit_BME280 bme;
static OneWire onewire(PIN_ONEWIRE);
static DallasTemperature dallasTemperature(&onewire);
static NMEASerial nmeaSerial(NULL);

static bool lastRainState = false;
static unsigned long lastReportTime = 0;

static float interiorTemp;
static float interiorHumidity;
static float interiorPressure;
static bool haveInteriorData = false;

void setup() {
    pinMode(PIN_DRD11A_RAIN, INPUT);
    pinMode(PIN_DRD11A_INTENSITY, INPUT);

    Serial.begin(57600);
    FreqCount.begin(1000);

    hasInteriorSensor = bme.begin(0x76);
    dallasTemperature.begin();
    hasEnclosureSensor = (dallasTemperature.getDeviceCount() > 0);

    if (hasInteriorSensor) {
        bme.setSampling(
            Adafruit_BME280::MODE_NORMAL,
            Adafruit_BME280::SAMPLING_X16,
            Adafruit_BME280::SAMPLING_X16,
            Adafruit_BME280::SAMPLING_X16,
            Adafruit_BME280::FILTER_OFF,
            Adafruit_BME280::STANDBY_MS_1000);
    }
}

void loop() {
    unsigned long currentTime = millis();
    unsigned long timeSinceLastReport = currentTime - lastReportTime;

    if (FreqCount.available()) {
        rainFrequency = FreqCount.read();
    }
    bool rain = (digitalRead(PIN_DRD11A_RAIN) == LOW);
    int rainIntensity = analogRead(PIN_DRD11A_INTENSITY);

    String msg = "RAIN=";
    msg += rain ? 1 : 0;
    msg += ",FREQ=";
    msg += rainFrequency;
    msg += ",INTENSITY=";
    msg += rainIntensity;

    if (hasEnclosureSensor) {
        dallasTemperature.requestTemperatures();
        float enclosureTemp = dallasTemperature.getTempCByIndex(0);
        msg += ",ENCLOSURETEMP=";
        msg += enclosureTemp;
    }
    if (hasInteriorSensor) {
        if (!haveInteriorData && timeSinceLastReport > 8000) {
            interiorTemp = bme.readTemperature();
            interiorHumidity = bme.readHumidity();
            interiorPressure = bme.readPressure();
            haveInteriorData = true;
        }
        msg += ",INTERIORTEMP=";
        msg += interiorTemp;
        msg += ",INTERIORHUMIDITY=";
        msg += interiorHumidity;
        msg += ",INTERIORPRESSURE=";
        msg += interiorPressure;
    }

    if ((lastRainState == false && rain) || timeSinceLastReport > 10000) {
        lastReportTime = currentTime;
        haveInteriorData = false;
        nmeaSerial.print(msg);
    }
    lastRainState = rain;
}

using KomaObservingConditions.WeatherRestApi;
using Moq;
using Xunit;

namespace KomaObservingConditions.Tests;

public sealed class ObservingConditionsTests : IDisposable
{
    private readonly Mock<IWeatherStatusSource> _source;
    private readonly ObservingConditions _conditions;

    public ObservingConditionsTests()
    {
        _source = new Mock<IWeatherStatusSource>();
        _conditions = new ObservingConditions(_source.Object);
    }

    public void Dispose()
    {
        _conditions.Dispose();
    }

    private static WeatherStatus CreateWeatherStatus() => new()
    {
        Temperature = 15.0,
        Humidity = 65.0,
        Pressure = 1013.25,
        WindSpeed = 5.0,
        WindGust = 8.0,
        WindDir = 180.0,
        RainRate = 0.0,
        DewPoint = 8.5,
    };

    private static TimestampedResult<WeatherStatus> CreateResult(DateTime? fetchedAt = null) =>
        new(CreateWeatherStatus(), fetchedAt ?? DateTime.UtcNow);

    #region Weather properties

    [Fact]
    public void Temperature_ReturnsValueFromSource()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateResult());
        Assert.Equal(15.0, _conditions.Temperature);
    }

    [Fact]
    public void Humidity_ReturnsValueFromSource()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateResult());
        Assert.Equal(65.0, _conditions.Humidity);
    }

    [Fact]
    public void Pressure_ReturnsValueFromSource()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateResult());
        Assert.Equal(1013.25, _conditions.Pressure);
    }

    [Fact]
    public void WindSpeed_ReturnsValueFromSource()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateResult());
        Assert.Equal(5.0, _conditions.WindSpeed);
    }

    [Fact]
    public void WindGust_ReturnsValueFromSource()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateResult());
        Assert.Equal(8.0, _conditions.WindGust);
    }

    [Fact]
    public void WindDirection_ReturnsValueFromSource()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateResult());
        Assert.Equal(180.0, _conditions.WindDirection);
    }

    [Fact]
    public void RainRate_ReturnsValueFromSource()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateResult());
        Assert.Equal(0.0, _conditions.RainRate);
    }

    [Fact]
    public void DewPoint_ReturnsValueFromSource()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateResult());
        Assert.Equal(8.5, _conditions.DewPoint);
    }

    #endregion

    #region DeviceState

    [Fact]
    public void DeviceState_ReturnsAllSensorNamesAndTimeStamp()
    {
        var fetchedAt = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateResult(fetchedAt));

        var state = _conditions.DeviceState;

        Assert.Contains(state, s => s.Name == "Temperature");
        Assert.Contains(state, s => s.Name == "Humidity");
        Assert.Contains(state, s => s.Name == "Pressure");
        Assert.Contains(state, s => s.Name == "WindSpeed");
        Assert.Contains(state, s => s.Name == "WindGust");
        Assert.Contains(state, s => s.Name == "WindDirection");
        Assert.Contains(state, s => s.Name == "RainRate");
        Assert.Contains(state, s => s.Name == "DewPoint");
        Assert.Contains(state, s => s.Name == "TimeStamp" && s.Value.ToString() == "2025-01-15T12:00:00.0000000Z");
    }

    #endregion

    #region AveragePeriod

    [Fact]
    public void AveragePeriod_Get_ReturnsZero()
    {
        Assert.Equal(0.0, _conditions.AveragePeriod);
    }

    [Fact]
    public void AveragePeriod_SetToZero_DoesNotThrow()
    {
        _conditions.AveragePeriod = 0.0;
    }

    [Fact]
    public void AveragePeriod_SetToNonZero_ThrowsDriverException()
    {
        Assert.Throws<ASCOM.DriverException>(() => _conditions.AveragePeriod = 5.0);
    }

    #endregion

    #region SensorDescription

    [Theory]
    [InlineData("Temperature")]
    [InlineData("DewPoint")]
    [InlineData("Humidity")]
    [InlineData("Pressure")]
    [InlineData("RainRate")]
    [InlineData("WindDirection")]
    [InlineData("WindGust")]
    [InlineData("WindSpeed")]
    [InlineData("temperature")]
    [InlineData("TEMPERATURE")]
    public void SensorDescription_ForSupportedSensor_ReturnsDescription(string sensorName)
    {
        var description = _conditions.SensorDescription(sensorName);
        Assert.False(string.IsNullOrEmpty(description));
    }

    [Fact]
    public void SensorDescription_ForUnsupportedSensor_ThrowsInvalidValueException()
    {
        Assert.Throws<ASCOM.InvalidValueException>(() => _conditions.SensorDescription("CloudCover"));
    }

    #endregion

    #region TimeSinceLastUpdate

    [Theory]
    [InlineData("Temperature")]
    [InlineData("Humidity")]
    [InlineData("windspeed")]
    public void TimeSinceLastUpdate_ForSupportedSensor_ReturnsElapsedSeconds(string sensorName)
    {
        var fetchedAt = DateTime.UtcNow.AddSeconds(-10);
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateResult(fetchedAt));

        var elapsed = _conditions.TimeSinceLastUpdate(sensorName);

        Assert.True(elapsed >= 10.0);
    }

    [Fact]
    public void TimeSinceLastUpdate_WithEmptyString_ReturnsElapsedSeconds()
    {
        var fetchedAt = DateTime.UtcNow.AddSeconds(-10);
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateResult(fetchedAt));

        var elapsed = _conditions.TimeSinceLastUpdate("");

        Assert.True(elapsed >= 10.0);
    }

    [Fact]
    public void TimeSinceLastUpdate_ForUnsupportedSensor_ThrowsInvalidValueException()
    {
        Assert.Throws<ASCOM.InvalidValueException>(() => _conditions.TimeSinceLastUpdate("SkyQuality"));
    }

    #endregion

    #region Not-implemented properties

    [Fact]
    public void CloudCover_ThrowsPropertyNotImplementedException()
    {
        Assert.Throws<ASCOM.PropertyNotImplementedException>(() => _conditions.CloudCover);
    }

    [Fact]
    public void SkyBrightness_ThrowsPropertyNotImplementedException()
    {
        Assert.Throws<ASCOM.PropertyNotImplementedException>(() => _conditions.SkyBrightness);
    }

    [Fact]
    public void SkyQuality_ThrowsPropertyNotImplementedException()
    {
        Assert.Throws<ASCOM.PropertyNotImplementedException>(() => _conditions.SkyQuality);
    }

    [Fact]
    public void StarFWHM_ThrowsPropertyNotImplementedException()
    {
        Assert.Throws<ASCOM.PropertyNotImplementedException>(() => _conditions.StarFWHM);
    }

    [Fact]
    public void SkyTemperature_ThrowsPropertyNotImplementedException()
    {
        Assert.Throws<ASCOM.PropertyNotImplementedException>(() => _conditions.SkyTemperature);
    }

    #endregion

    #region Basic info

    [Fact]
    public void Connected_ReturnsTrue()
    {
        Assert.True(_conditions.Connected);
    }

    #endregion
}

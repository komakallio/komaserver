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

    #region Weather properties

    [Fact]
    public void Temperature_ReturnsValueFromSource()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateWeatherStatus());
        Assert.Equal(15.0, _conditions.Temperature);
    }

    [Fact]
    public void Humidity_ReturnsValueFromSource()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateWeatherStatus());
        Assert.Equal(65.0, _conditions.Humidity);
    }

    [Fact]
    public void Pressure_ReturnsValueFromSource()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateWeatherStatus());
        Assert.Equal(1013.25, _conditions.Pressure);
    }

    [Fact]
    public void WindSpeed_ReturnsValueFromSource()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateWeatherStatus());
        Assert.Equal(5.0, _conditions.WindSpeed);
    }

    [Fact]
    public void WindGust_ReturnsValueFromSource()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateWeatherStatus());
        Assert.Equal(8.0, _conditions.WindGust);
    }

    [Fact]
    public void WindDirection_ReturnsValueFromSource()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateWeatherStatus());
        Assert.Equal(180.0, _conditions.WindDirection);
    }

    [Fact]
    public void RainRate_ReturnsValueFromSource()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateWeatherStatus());
        Assert.Equal(0.0, _conditions.RainRate);
    }

    [Fact]
    public void DewPoint_ReturnsValueFromSource()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateWeatherStatus());
        Assert.Equal(8.5, _conditions.DewPoint);
    }

    #endregion

    #region Null source handling

    [Fact]
    public void Temperature_WhenSourceReturnsNull_ThrowsDriverException()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync((WeatherStatus?)null);
        Assert.Throws<ASCOM.DriverException>(() => _conditions.Temperature);
    }

    [Fact]
    public void Humidity_WhenSourceReturnsNull_ThrowsDriverException()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync((WeatherStatus?)null);
        Assert.Throws<ASCOM.DriverException>(() => _conditions.Humidity);
    }

    [Fact]
    public void Pressure_WhenSourceReturnsNull_ThrowsDriverException()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync((WeatherStatus?)null);
        Assert.Throws<ASCOM.DriverException>(() => _conditions.Pressure);
    }

    [Fact]
    public void WindSpeed_WhenSourceReturnsNull_ThrowsDriverException()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync((WeatherStatus?)null);
        Assert.Throws<ASCOM.DriverException>(() => _conditions.WindSpeed);
    }

    [Fact]
    public void WindGust_WhenSourceReturnsNull_ThrowsDriverException()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync((WeatherStatus?)null);
        Assert.Throws<ASCOM.DriverException>(() => _conditions.WindGust);
    }

    [Fact]
    public void WindDirection_WhenSourceReturnsNull_ThrowsDriverException()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync((WeatherStatus?)null);
        Assert.Throws<ASCOM.DriverException>(() => _conditions.WindDirection);
    }

    [Fact]
    public void RainRate_WhenSourceReturnsNull_ThrowsDriverException()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync((WeatherStatus?)null);
        Assert.Throws<ASCOM.DriverException>(() => _conditions.RainRate);
    }

    [Fact]
    public void DewPoint_WhenSourceReturnsNull_ThrowsDriverException()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync((WeatherStatus?)null);
        Assert.Throws<ASCOM.DriverException>(() => _conditions.DewPoint);
    }

    [Fact]
    public void DeviceState_WhenSourceReturnsNull_ThrowsDriverException()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync((WeatherStatus?)null);
        Assert.Throws<ASCOM.DriverException>(() => _conditions.DeviceState);
    }

    #endregion

    #region DeviceState

    [Fact]
    public void DeviceState_ReturnsAllSensorNamesAndTimeStamp()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(CreateWeatherStatus());

        var state = _conditions.DeviceState;

        Assert.Contains(state, s => s.Name == "Temperature");
        Assert.Contains(state, s => s.Name == "Humidity");
        Assert.Contains(state, s => s.Name == "Pressure");
        Assert.Contains(state, s => s.Name == "WindSpeed");
        Assert.Contains(state, s => s.Name == "WindGust");
        Assert.Contains(state, s => s.Name == "WindDirection");
        Assert.Contains(state, s => s.Name == "RainRate");
        Assert.Contains(state, s => s.Name == "DewPoint");
        Assert.Contains(state, s => s.Name == "TimeStamp");
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
    public void SensorDescription_ForUnsupportedSensor_ThrowsMethodNotImplementedException()
    {
        Assert.Throws<ASCOM.MethodNotImplementedException>(() => _conditions.SensorDescription("CloudCover"));
    }

    #endregion

    #region TimeSinceLastUpdate

    [Theory]
    [InlineData("Temperature")]
    [InlineData("Humidity")]
    [InlineData("windspeed")]
    public void TimeSinceLastUpdate_ForSupportedSensor_ReturnsZero(string sensorName)
    {
        Assert.Equal(0.0, _conditions.TimeSinceLastUpdate(sensorName));
    }

    [Fact]
    public void TimeSinceLastUpdate_ForUnsupportedSensor_ThrowsMethodNotImplementedException()
    {
        Assert.Throws<ASCOM.MethodNotImplementedException>(() => _conditions.TimeSinceLastUpdate("SkyQuality"));
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

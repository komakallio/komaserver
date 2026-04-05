using Xunit;

namespace KomaDome.Tests;

public class DomeOptionsTests
{
    [Theory]
    [InlineData("West pier", "westpier")]
    [InlineData("Center pier", "centerpier")]
    [InlineData("East pier", "eastpier")]
    [InlineData("westpier", "westpier")]
    [InlineData("West-Pier!", "westpier")]
    public void ToApiUser_converts_friendly_name(string friendlyName, string expected)
    {
        Assert.Equal(expected, DomeOptions.ToApiUser(friendlyName));
    }
}

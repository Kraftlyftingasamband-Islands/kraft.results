using KRAFT.Results.Web.Client;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests;

public sealed class DisplayFormatTests
{
    [Theory]
    [InlineData(220.5, "220,5")]
    [InlineData(200.0, "200,0")]
    [InlineData(1057.5, "1057,5")]
    public void WhenWeightFormatted_UsesOneDecimalWithCommaAndNoThousandsSeparator(
        double input,
        string expected)
    {
        // Arrange
        decimal value = (decimal)input;

        // Act
        string result = DisplayFormat.Weight(value);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(82.5, "82,50")]
    [InlineData(82.0, "82,00")]
    public void WhenBodyWeightFormatted_UsesTwoDecimalsWithComma(double input, string expected)
    {
        // Arrange
        decimal value = (decimal)input;

        // Act
        string result = DisplayFormat.BodyWeight(value);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(512.3, "512,30")]
    [InlineData(100.0, "100,00")]
    public void WhenPointsFormatted_UsesTwoDecimalsWithComma(double input, string expected)
    {
        // Arrange
        decimal value = (decimal)input;

        // Act
        string result = DisplayFormat.Points(value);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void WhenDateOnlyFormatted_UsesIcelandicDotSeparatedDate()
    {
        // Arrange
        DateOnly date = new(2024, 3, 15);

        // Act
        string result = DisplayFormat.Date(date);

        // Assert
        result.ShouldBe("15.03.2024");
    }

    [Fact]
    public void WhenDateTimeFormatted_UsesIcelandicDotSeparatedDate()
    {
        // Arrange
        DateTime date = new(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        string result = DisplayFormat.Date(date);

        // Assert
        result.ShouldBe("15.03.2024");
    }

    [Fact]
    public void WhenMonthYearFormatted_UsesIcelandicAbbreviatedMonthName()
    {
        // Arrange
        DateOnly date = new(2024, 3, 15);

        // Act
        string result = DisplayFormat.MonthYear(date);

        // Assert
        result.ShouldBe("mar. 2024");
    }

    [Fact]
    public void WhenMonthNameFormatted_UsesIcelandicFullMonthName()
    {
        // Arrange
        DateOnly date = new(2024, 1, 15);

        // Act
        string result = DisplayFormat.MonthName(date);

        // Assert
        result.ShouldBe("Janúar");
    }

    [Theory]
    [InlineData("147,5", true, 147.5)]
    [InlineData("147.5", true, 147.5)]
    [InlineData("1057,5", true, 1057.5)]
    [InlineData("200", true, 200.0)]
    [InlineData("-147,5", true, -147.5)]
    [InlineData("-147.5", true, -147.5)]
    [InlineData(".5", true, 0.5)]
    public void WhenInputIsValid_TryParseWeightReturnsTrueAndParsedValue(
        string input,
        bool expectedSuccess,
        double expectedValue)
    {
        // Arrange
        // (input supplied by theory)

        // Act
        bool success = DisplayFormat.TryParseWeight(input, out decimal parsed);

        // Assert
        success.ShouldBe(expectedSuccess);
        parsed.ShouldBe((decimal)expectedValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("1.500,5")]
    public void WhenInputIsInvalid_TryParseWeightReturnsFalse(string? input)
    {
        // Arrange
        // (input supplied by theory)

        // Act
        bool success = DisplayFormat.TryParseWeight(input, out decimal parsed);

        // Assert
        success.ShouldBeFalse();
        parsed.ShouldBe(0m);
    }
}

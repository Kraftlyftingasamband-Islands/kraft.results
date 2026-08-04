using KRAFT.Results.Web.Client;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests;

public sealed class DisplayFormatTests
{
    [Theory]
    [InlineData(220.5, "220,5")]
    [InlineData(200.0, "200,0")]
    [InlineData(1057.5, "1057,5")]
    public void Weight_WhenCalled_FormatsWithOneDecimalAndCommaNoThousandsSeparator(
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
    public void BodyWeight_WhenCalled_FormatsWithTwoDecimalsAndComma(double input, string expected)
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
    public void Points_WhenCalled_FormatsWithTwoDecimalsAndComma(double input, string expected)
    {
        // Arrange
        decimal value = (decimal)input;

        // Act
        string result = DisplayFormat.Points(value);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void Date_WithDateOnly_FormatsAsDdMmYyyy()
    {
        // Arrange
        DateOnly date = new(2024, 3, 15);

        // Act
        string result = DisplayFormat.Date(date);

        // Assert
        result.ShouldBe("15.03.2024");
    }

    [Fact]
    public void Date_WithDateTime_FormatsAsDdMmYyyy()
    {
        // Arrange
        DateTime date = new(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        string result = DisplayFormat.Date(date);

        // Assert
        result.ShouldBe("15.03.2024");
    }

    [Fact]
    public void MonthYear_FormatsAsMmmYyyyWithIcelandicAbbreviation()
    {
        // Arrange
        DateOnly date = new(2024, 3, 15);

        // Act
        string result = DisplayFormat.MonthYear(date);

        // Assert
        result.ShouldBe("mar. 2024");
    }

    [Theory]
    [InlineData("147,5", true, 147.5)]
    [InlineData("147.5", true, 147.5)]
    [InlineData("1057,5", true, 1057.5)]
    [InlineData("200", true, 200.0)]
    public void TryParseWeight_WhenValidInput_ReturnsTrueAndParsedValue(
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
    public void TryParseWeight_WhenInvalidInput_ReturnsFalse(string? input)
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

using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Athletes;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Athletes.BanTests;

public sealed class Create
{
    [Fact]
    public void WhenValidDates_ReturnsBan()
    {
        // Arrange
        int athleteId = 1;
        DateTime fromDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime toDate = new(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        // Act
        Result<Ban> result = Ban.Create(athleteId, fromDate, toDate);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Ban ban = result.FromResult();
        ban.AthleteId.ShouldBe(athleteId);
        ban.FromDate.ShouldBe(fromDate);
        ban.ToDate.ShouldBe(toDate);
    }

    [Fact]
    public void WhenFromDateAfterToDate_ReturnsFailure()
    {
        // Arrange
        int athleteId = 1;
        DateTime fromDate = new(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        DateTime toDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        Result<Ban> result = Ban.Create(athleteId, fromDate, toDate);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(BanErrors.FromDateAfterToDate);
    }
}
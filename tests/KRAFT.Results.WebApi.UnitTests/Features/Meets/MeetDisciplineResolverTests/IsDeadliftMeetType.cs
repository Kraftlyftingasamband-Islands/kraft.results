using KRAFT.Results.WebApi.Features.Meets;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Meets.MeetDisciplineResolverTests;

public sealed class IsDeadliftMeetType
{
    [Fact]
    public void WhenTitleContainsRettst_ReturnsTrue()
    {
        // Arrange
        int meetTypeId = 3;
        string meetTypeTitle = "Réttstakeppni";

        // Act
        bool result = MeetDisciplineResolver.IsDeadliftMeetType(meetTypeId, meetTypeTitle);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void WhenTitleContainsDeadlift_ReturnsTrue()
    {
        // Arrange
        int meetTypeId = 3;
        string meetTypeTitle = "Deadlift Championship";

        // Act
        bool result = MeetDisciplineResolver.IsDeadliftMeetType(meetTypeId, meetTypeTitle);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void WhenBenchMeetTypeId_ReturnsFalse()
    {
        // Arrange
        int meetTypeId = 2;
        string meetTypeTitle = "Bekkpressukeppni";

        // Act
        bool result = MeetDisciplineResolver.IsDeadliftMeetType(meetTypeId, meetTypeTitle);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void WhenFullMeetTitle_ReturnsFalse()
    {
        // Arrange
        int meetTypeId = 1;
        string meetTypeTitle = "Kraftlyfting";

        // Act
        bool result = MeetDisciplineResolver.IsDeadliftMeetType(meetTypeId, meetTypeTitle);

        // Assert
        result.ShouldBeFalse();
    }
}
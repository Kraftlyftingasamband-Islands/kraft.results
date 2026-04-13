using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Meets;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Meets.MeetDisciplineResolverTests;

public sealed class MapDisciplineToRecordCategory
{
    [Fact]
    public void WhenSquatInFullMeet_ReturnsSquat()
    {
        // Arrange
        Discipline discipline = Discipline.Squat;
        int meetTypeId = 1;
        string meetTypeTitle = "Kraftlyfting";

        // Act
        RecordCategory result = MeetDisciplineResolver.MapDisciplineToRecordCategory(
            discipline, meetTypeId, meetTypeTitle);

        // Assert
        result.ShouldBe(RecordCategory.Squat);
    }

    [Fact]
    public void WhenBenchInFullMeet_ReturnsBench()
    {
        // Arrange
        Discipline discipline = Discipline.Bench;
        int meetTypeId = 1;
        string meetTypeTitle = "Kraftlyfting";

        // Act
        RecordCategory result = MeetDisciplineResolver.MapDisciplineToRecordCategory(
            discipline, meetTypeId, meetTypeTitle);

        // Assert
        result.ShouldBe(RecordCategory.Bench);
    }

    [Fact]
    public void WhenDeadliftInFullMeet_ReturnsDeadlift()
    {
        // Arrange
        Discipline discipline = Discipline.Deadlift;
        int meetTypeId = 1;
        string meetTypeTitle = "Kraftlyfting";

        // Act
        RecordCategory result = MeetDisciplineResolver.MapDisciplineToRecordCategory(
            discipline, meetTypeId, meetTypeTitle);

        // Assert
        result.ShouldBe(RecordCategory.Deadlift);
    }

    [Fact]
    public void WhenBenchInBenchOnlyMeet_ReturnsBenchSingle()
    {
        // Arrange
        Discipline discipline = Discipline.Bench;
        int meetTypeId = 2;
        string meetTypeTitle = "Bekkpressukeppni";

        // Act
        RecordCategory result = MeetDisciplineResolver.MapDisciplineToRecordCategory(
            discipline, meetTypeId, meetTypeTitle);

        // Assert
        result.ShouldBe(RecordCategory.BenchSingle);
    }

    [Fact]
    public void WhenDeadliftInDeadliftOnlyMeet_ReturnsDeadliftSingle()
    {
        // Arrange
        Discipline discipline = Discipline.Deadlift;
        int meetTypeId = 3;
        string meetTypeTitle = "Réttstakeppni";

        // Act
        RecordCategory result = MeetDisciplineResolver.MapDisciplineToRecordCategory(
            discipline, meetTypeId, meetTypeTitle);

        // Assert
        result.ShouldBe(RecordCategory.DeadliftSingle);
    }

    [Fact]
    public void WhenNoneDiscipline_ReturnsNone()
    {
        // Arrange
        Discipline discipline = Discipline.None;
        int meetTypeId = 1;
        string meetTypeTitle = "Kraftlyfting";

        // Act
        RecordCategory result = MeetDisciplineResolver.MapDisciplineToRecordCategory(
            discipline, meetTypeId, meetTypeTitle);

        // Assert
        result.ShouldBe(RecordCategory.None);
    }
}
using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Meets;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Meets.MeetCategoryExtensionsTests;

public sealed class MapDisciplineToRecordCategory
{
    [Fact]
    public void Squat_ReturnsSquat()
    {
        // Arrange
        MeetCategory category = MeetCategory.Powerlifting;

        // Act
        RecordCategory result = category.MapDisciplineToRecordCategory(Discipline.Squat);

        // Assert
        result.ShouldBe(RecordCategory.Squat);
    }

    [Fact]
    public void Bench_WhenFullMeet_ReturnsBench()
    {
        // Arrange
        MeetCategory category = MeetCategory.Powerlifting;

        // Act
        RecordCategory result = category.MapDisciplineToRecordCategory(Discipline.Bench);

        // Assert
        result.ShouldBe(RecordCategory.Bench);
    }

    [Fact]
    public void Bench_WhenSingleLift_ReturnsBenchSingle()
    {
        // Arrange
        MeetCategory category = MeetCategory.Benchpress;

        // Act
        RecordCategory result = category.MapDisciplineToRecordCategory(Discipline.Bench);

        // Assert
        result.ShouldBe(RecordCategory.BenchSingle);
    }

    [Fact]
    public void Deadlift_WhenFullMeet_ReturnsDeadlift()
    {
        // Arrange
        MeetCategory category = MeetCategory.Powerlifting;

        // Act
        RecordCategory result = category.MapDisciplineToRecordCategory(Discipline.Deadlift);

        // Assert
        result.ShouldBe(RecordCategory.Deadlift);
    }

    [Fact]
    public void Deadlift_WhenSingleLift_ReturnsDeadliftSingle()
    {
        // Arrange
        MeetCategory category = MeetCategory.Deadlift;

        // Act
        RecordCategory result = category.MapDisciplineToRecordCategory(Discipline.Deadlift);

        // Assert
        result.ShouldBe(RecordCategory.DeadliftSingle);
    }

    [Fact]
    public void Bench_WhenPushPull_ReturnsBenchSingle()
    {
        // Arrange
        MeetCategory category = MeetCategory.PushPull;

        // Act
        RecordCategory result = category.MapDisciplineToRecordCategory(Discipline.Bench);

        // Assert
        result.ShouldBe(RecordCategory.BenchSingle);
    }

    [Fact]
    public void Deadlift_WhenPushPull_ReturnsDeadliftSingle()
    {
        // Arrange
        MeetCategory category = MeetCategory.PushPull;

        // Act
        RecordCategory result = category.MapDisciplineToRecordCategory(Discipline.Deadlift);

        // Assert
        result.ShouldBe(RecordCategory.DeadliftSingle);
    }

    [Fact]
    public void Deadlift_WhenSquatCategory_ReturnsDeadlift()
    {
        // Arrange
        MeetCategory category = MeetCategory.Squat;

        // Act
        RecordCategory result = category.MapDisciplineToRecordCategory(Discipline.Deadlift);

        // Assert
        result.ShouldBe(RecordCategory.Deadlift);
    }

    [Fact]
    public void None_ReturnsNone()
    {
        // Arrange
        MeetCategory category = MeetCategory.Powerlifting;

        // Act
        RecordCategory result = category.MapDisciplineToRecordCategory(Discipline.None);

        // Assert
        result.ShouldBe(RecordCategory.None);
    }
}
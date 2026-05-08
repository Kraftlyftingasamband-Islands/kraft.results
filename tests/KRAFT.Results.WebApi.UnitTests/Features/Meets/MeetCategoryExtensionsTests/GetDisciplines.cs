using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Features.Meets;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Meets.MeetCategoryExtensionsTests;

public sealed class GetDisciplines
{
    [Fact]
    public void Powerlifting_ReturnsAllThree()
    {
        // Arrange
        MeetCategory category = MeetCategory.Powerlifting;

        // Act
        IReadOnlyList<Discipline> result = category.GetDisciplines();

        // Assert
        result.Count.ShouldBe(3);
        result[0].ShouldBe(Discipline.Squat);
        result[1].ShouldBe(Discipline.Bench);
        result[2].ShouldBe(Discipline.Deadlift);
    }

    [Fact]
    public void Benchpress_ReturnsBenchOnly()
    {
        // Arrange
        MeetCategory category = MeetCategory.Benchpress;

        // Act
        IReadOnlyList<Discipline> result = category.GetDisciplines();

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldBe(Discipline.Bench);
    }

    [Fact]
    public void Deadlift_ReturnsDeadliftOnly()
    {
        // Arrange
        MeetCategory category = MeetCategory.Deadlift;

        // Act
        IReadOnlyList<Discipline> result = category.GetDisciplines();

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldBe(Discipline.Deadlift);
    }

    [Fact]
    public void PushPull_ReturnsBenchAndDeadlift()
    {
        // Arrange
        MeetCategory category = MeetCategory.PushPull;

        // Act
        IReadOnlyList<Discipline> result = category.GetDisciplines();

        // Assert
        result.Count.ShouldBe(2);
        result[0].ShouldBe(Discipline.Bench);
        result[1].ShouldBe(Discipline.Deadlift);
    }
}
using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Features.Meets;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Meets.MeetDisciplineResolverTests;

public sealed class ResolveDisciplines
{
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    public void WhenBenchMeetType_ReturnsBenchOnly(int meetTypeId)
    {
        // Arrange
        string meetTypeTitle = "Some meet";

        // Act
        IReadOnlyList<Discipline> result = MeetDisciplineResolver.ResolveDisciplines(meetTypeId, meetTypeTitle);

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldBe(Discipline.Bench);
    }

    [Theory]
    [InlineData("Réttstaða")]
    [InlineData("Rettstaða")]
    [InlineData("Deadlift only")]
    public void WhenDeadliftMeetTitle_ReturnsDeadliftOnly(string meetTypeTitle)
    {
        // Arrange
        int meetTypeId = 99;

        // Act
        IReadOnlyList<Discipline> result = MeetDisciplineResolver.ResolveDisciplines(meetTypeId, meetTypeTitle);

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldBe(Discipline.Deadlift);
    }

    [Fact]
    public void WhenFullMeet_ReturnsAllThree()
    {
        // Arrange
        int meetTypeId = 1;
        string meetTypeTitle = "Fullveldi";

        // Act
        IReadOnlyList<Discipline> result = MeetDisciplineResolver.ResolveDisciplines(meetTypeId, meetTypeTitle);

        // Assert
        result.Count.ShouldBe(3);
        result[0].ShouldBe(Discipline.Squat);
        result[1].ShouldBe(Discipline.Bench);
        result[2].ShouldBe(Discipline.Deadlift);
    }
}
using KRAFT.Results.Contracts.Teams;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders.UpdateTeamCommandBuilderTests;

public sealed class Build
{
    [Fact]
    public void WhenMultipleInstancesAreBuilt_TitleShortsAreUnique()
    {
        // Arrange
        const int count = 100;
        HashSet<string> titleShorts = new(count);

        // Act
        for (int i = 0; i < count; i++)
        {
            UpdateTeamCommand command = new UpdateTeamCommandBuilder().Build();
            titleShorts.Add(command.TitleShort);
        }

        // Assert
        titleShorts.Count.ShouldBe(count);
    }
}
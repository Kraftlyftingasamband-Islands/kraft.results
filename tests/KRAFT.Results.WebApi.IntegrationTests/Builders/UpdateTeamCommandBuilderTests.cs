using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

public sealed class UpdateTeamCommandBuilderTests
{
    [Fact]
    public void Build_GeneratesUniqueTitleShort_AcrossMultipleInstances()
    {
        // Arrange
        const int count = 100;
        HashSet<string> titleShorts = new(count);

        // Act
        for (int i = 0; i < count; i++)
        {
            Contracts.Teams.UpdateTeamCommand command = new UpdateTeamCommandBuilder().Build();
            titleShorts.Add(command.TitleShort);
        }

        // Assert
        titleShorts.Count.ShouldBe(count);
    }
}
using KRAFT.Results.Contracts.Clubs;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders.UpdateClubCommandBuilderTests;

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
            UpdateClubCommand command = new UpdateClubCommandBuilder().Build();
            titleShorts.Add(command.TitleShort);
        }

        // Assert
        titleShorts.Count.ShouldBe(count);
    }
}

using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests;

[Collection(nameof(InfraCollection))]
public class IntegrationTestFixtureTests(CollectionFixture fixture)
{
    [Fact]
    public void TracksChildFactory_WhenCreatingAuthorizedHttpClient()
    {
        // Arrange
        int initialCount = fixture.ChildFactoryCount;

        // Act
        fixture.CreateAuthorizedHttpClient();

        // Assert
        fixture.ChildFactoryCount.ShouldBeGreaterThan(initialCount);
    }

    [Fact]
    public void TracksChildFactory_WhenCreatingNoNameClaimHttpClient()
    {
        // Arrange
        int initialCount = fixture.ChildFactoryCount;

        // Act
        fixture.CreateNoNameClaimHttpClient();

        // Assert
        fixture.ChildFactoryCount.ShouldBeGreaterThan(initialCount);
    }

    [Fact]
    public void TracksChildFactory_WhenCreatingNonAdminAuthorizedHttpClient()
    {
        // Arrange
        int initialCount = fixture.ChildFactoryCount;

        // Act
        fixture.CreateNonAdminAuthorizedHttpClient();

        // Assert
        fixture.ChildFactoryCount.ShouldBeGreaterThan(initialCount);
    }
}
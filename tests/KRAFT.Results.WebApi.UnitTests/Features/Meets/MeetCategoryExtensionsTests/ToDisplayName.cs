using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Features.Meets;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Meets.MeetCategoryExtensionsTests;

public sealed class ToDisplayName
{
    [Fact]
    public void PushPull_ReturnsConstantsPushPull()
    {
        // Arrange
        MeetCategory category = MeetCategory.PushPull;

        // Act
        string result = category.ToDisplayName();

        // Assert
        result.ShouldBe(Constants.PushPull);
    }
}
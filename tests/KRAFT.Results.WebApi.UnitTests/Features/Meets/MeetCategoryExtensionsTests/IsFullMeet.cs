using KRAFT.Results.WebApi.Features.Meets;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Meets.MeetCategoryExtensionsTests;

public sealed class IsFullMeet
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(4, true)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(5, false)]
    public void ReturnsExpected(int categoryId, bool expected)
    {
        // Arrange
        MeetCategory category = (MeetCategory)categoryId;

        // Act
        bool result = category.IsFullMeet();

        // Assert
        result.ShouldBe(expected);
    }
}
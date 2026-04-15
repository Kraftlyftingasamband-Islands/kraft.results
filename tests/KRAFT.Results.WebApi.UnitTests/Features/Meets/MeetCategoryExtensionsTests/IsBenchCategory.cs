using KRAFT.Results.WebApi.Features.Meets;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Meets.MeetCategoryExtensionsTests;

public sealed class IsBenchCategory
{
    [Theory]
    [InlineData(2, true)]
    [InlineData(5, true)]
    [InlineData(1, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    public void ReturnsExpected(int categoryId, bool expected)
    {
        // Arrange
        MeetCategory category = (MeetCategory)categoryId;

        // Act
        bool result = category.IsBenchCategory();

        // Assert
        result.ShouldBe(expected);
    }
}
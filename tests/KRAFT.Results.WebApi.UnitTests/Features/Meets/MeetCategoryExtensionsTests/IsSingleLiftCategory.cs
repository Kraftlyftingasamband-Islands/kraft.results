using KRAFT.Results.WebApi.Features.Meets;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Meets.MeetCategoryExtensionsTests;

public sealed class IsSingleLiftCategory
{
    [Theory]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(5, true)]
    [InlineData(1, false)]
    public void ReturnsExpected(int categoryId, bool expected)
    {
        // Arrange
        MeetCategory category = (MeetCategory)categoryId;

        // Act
        bool result = category.IsSingleLiftCategory();

        // Assert
        result.ShouldBe(expected);
    }
}
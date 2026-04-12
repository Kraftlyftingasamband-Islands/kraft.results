using KRAFT.Results.WebApi.Features.AgeCategories;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.AgeCategories.AgeCategoryTests;

public sealed class GetCascadeSlugs
{
    [Fact]
    public void WhenMasters4_CascadesToAllMastersAndOpen()
    {
        // Arrange
        string slug = "masters4";

        // Act
        IReadOnlyList<string> result = AgeCategory.GetCascadeSlugs(slug);

        // Assert
        result.ShouldBe(["masters4", "masters3", "masters2", "masters1", "open"]);
    }

    [Fact]
    public void WhenMasters3_CascadesToRemainingMastersAndOpen()
    {
        // Arrange
        string slug = "masters3";

        // Act
        IReadOnlyList<string> result = AgeCategory.GetCascadeSlugs(slug);

        // Assert
        result.ShouldBe(["masters3", "masters2", "masters1", "open"]);
    }

    [Fact]
    public void WhenMasters2_Cascades()
    {
        // Arrange
        string slug = "masters2";

        // Act
        IReadOnlyList<string> result = AgeCategory.GetCascadeSlugs(slug);

        // Assert
        result.ShouldBe(["masters2", "masters1", "open"]);
    }

    [Fact]
    public void WhenMasters1_Cascades()
    {
        // Arrange
        string slug = "masters1";

        // Act
        IReadOnlyList<string> result = AgeCategory.GetCascadeSlugs(slug);

        // Assert
        result.ShouldBe(["masters1", "open"]);
    }

    [Fact]
    public void WhenSubJunior_CascadesToJuniorAndOpen()
    {
        // Arrange
        string slug = "subjunior";

        // Act
        IReadOnlyList<string> result = AgeCategory.GetCascadeSlugs(slug);

        // Assert
        result.ShouldBe(["subjunior", "junior", "open"]);
    }

    [Fact]
    public void WhenJunior_CascadesToOpen()
    {
        // Arrange
        string slug = "junior";

        // Act
        IReadOnlyList<string> result = AgeCategory.GetCascadeSlugs(slug);

        // Assert
        result.ShouldBe(["junior", "open"]);
    }

    [Fact]
    public void WhenOpen_ReturnsSelfOnly()
    {
        // Arrange
        string slug = "open";

        // Act
        IReadOnlyList<string> result = AgeCategory.GetCascadeSlugs(slug);

        // Assert
        result.ShouldBe(["open"]);
    }
}
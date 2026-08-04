using KRAFT.Results.Contracts;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Contracts;

public sealed class ResolveAgeCategoryLabelTests
{
    [Fact]
    public void WhenKnownSlug_ReturnsIcelandicLabel()
    {
        // Arrange

        // Act
        string result = DisplayNames.ResolveAgeCategoryLabel("open", "Open", null);

        // Assert
        result.ShouldBe("Opinn flokkur");
    }

    [Fact]
    public void WhenUnknownSlug_ReturnsTitleFallback()
    {
        // Arrange

        // Act
        string result = DisplayNames.ResolveAgeCategoryLabel("custom-category", "Custom Title", null);

        // Assert
        result.ShouldBe("Custom Title");
    }

    [Fact]
    public void WhenNullSlug_ReturnsTitleFallback()
    {
        // Arrange

        // Act
        string result = DisplayNames.ResolveAgeCategoryLabel(null, "Open", null);

        // Assert
        result.ShouldBe("Open");
    }

    [Fact]
    public void WhenMaleSubjunior_ReturnsDrengjaflokkur()
    {
        // Arrange

        // Act
        string result = DisplayNames.ResolveAgeCategoryLabel("subjunior", "Subjunior", "m");

        // Assert
        result.ShouldBe("Drengjaflokkur");
    }

    [Fact]
    public void WhenFemaleSubjunior_ReturnsStulknaflokkur()
    {
        // Arrange

        // Act
        string result = DisplayNames.ResolveAgeCategoryLabel("subjunior", "Subjunior", "f");

        // Assert
        result.ShouldBe("Stúlknaflokkur");
    }
}

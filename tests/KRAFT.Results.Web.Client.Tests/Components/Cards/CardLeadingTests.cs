using KRAFT.Results.Web.Client.Components.Cards;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Components.Cards;

public sealed class CardLeadingTests
{
    [Fact]
    public void WhenRankFactoryCalled_ReturnsRankLeadingWithValue()
    {
        // Arrange

        // Act
        CardLeading result = CardLeading.Rank(1);

        // Assert
        CardLeading.RankLeading leading = result.ShouldBeOfType<CardLeading.RankLeading>();
        leading.Value.ShouldBe(1);
    }

    [Fact]
    public void WhenBadgeFactoryCalled_ReturnsBadgeLeadingWithText()
    {
        // Arrange

        // Act
        CardLeading result = CardLeading.Badge("NEW");

        // Assert
        CardLeading.BadgeLeading leading = result.ShouldBeOfType<CardLeading.BadgeLeading>();
        leading.Text.ShouldBe("NEW");
    }

    [Fact]
    public void WhenRankIsOne_DisplayIsGoldMedalGlyph()
    {
        // Arrange
        CardLeading.RankLeading sut = new(1);

        // Act
        string display = sut.Display;

        // Assert
        display.ShouldBe("\U0001f947");
    }

    [Fact]
    public void WhenRankIsTwo_DisplayIsSilverMedalGlyph()
    {
        // Arrange
        CardLeading.RankLeading sut = new(2);

        // Act
        string display = sut.Display;

        // Assert
        display.ShouldBe("\U0001f948");
    }

    [Fact]
    public void WhenRankIsThree_DisplayIsBronzeMedalGlyph()
    {
        // Arrange
        CardLeading.RankLeading sut = new(3);

        // Act
        string display = sut.Display;

        // Assert
        display.ShouldBe("\U0001f949");
    }

    [Fact]
    public void WhenRankIsFour_DisplayIsPlainNumber()
    {
        // Arrange
        CardLeading.RankLeading sut = new(4);

        // Act
        string display = sut.Display;

        // Assert
        display.ShouldBe("4");
    }

    [Fact]
    public void WhenRankIsGreaterThanThree_DisplayIsPlainNumber()
    {
        // Arrange
        CardLeading.RankLeading sut = new(10);

        // Act
        string display = sut.Display;

        // Assert
        display.ShouldBe("10");
    }

    [Fact]
    public void WhenRankIsZero_DisplayIsEnDash()
    {
        // Arrange
        CardLeading.RankLeading sut = new(0);

        // Act
        string display = sut.Display;

        // Assert
        display.ShouldBe("–");
    }

    [Fact]
    public void WhenRankIsNegative_DisplayIsEnDash()
    {
        // Arrange
        CardLeading.RankLeading sut = new(-1);

        // Act
        string display = sut.Display;

        // Assert
        display.ShouldBe("–");
    }

    [Fact]
    public void WhenRankIsOne_IsMedalIsTrue()
    {
        // Arrange
        CardLeading.RankLeading sut = new(1);

        // Act
        bool isMedal = sut.IsMedal;

        // Assert
        isMedal.ShouldBeTrue();
    }

    [Fact]
    public void WhenRankIsThree_IsMedalIsTrue()
    {
        // Arrange
        CardLeading.RankLeading sut = new(3);

        // Act
        bool isMedal = sut.IsMedal;

        // Assert
        isMedal.ShouldBeTrue();
    }

    [Fact]
    public void WhenRankIsFour_IsMedalIsFalse()
    {
        // Arrange
        CardLeading.RankLeading sut = new(4);

        // Act
        bool isMedal = sut.IsMedal;

        // Assert
        isMedal.ShouldBeFalse();
    }

    [Fact]
    public void WhenRankIsZero_IsMedalIsFalse()
    {
        // Arrange
        CardLeading.RankLeading sut = new(0);

        // Act
        bool isMedal = sut.IsMedal;

        // Assert
        isMedal.ShouldBeFalse();
    }

    [Fact]
    public void WhenRankIsNegative_IsMedalIsFalse()
    {
        // Arrange
        CardLeading.RankLeading sut = new(-1);

        // Act
        bool isMedal = sut.IsMedal;

        // Assert
        isMedal.ShouldBeFalse();
    }

    [Fact]
    public void WhenRankIsOne_AriaLabelIsIcelandicFirstPlace()
    {
        // Arrange
        CardLeading.RankLeading sut = new(1);

        // Act
        string? ariaLabel = sut.AriaLabel;

        // Assert
        ariaLabel.ShouldBe("1. sæti");
    }

    [Fact]
    public void WhenRankIsTwo_AriaLabelIsIcelandicSecondPlace()
    {
        // Arrange
        CardLeading.RankLeading sut = new(2);

        // Act
        string? ariaLabel = sut.AriaLabel;

        // Assert
        ariaLabel.ShouldBe("2. sæti");
    }

    [Fact]
    public void WhenRankIsThree_AriaLabelIsIcelandicThirdPlace()
    {
        // Arrange
        CardLeading.RankLeading sut = new(3);

        // Act
        string? ariaLabel = sut.AriaLabel;

        // Assert
        ariaLabel.ShouldBe("3. sæti");
    }

    [Fact]
    public void WhenRankIsFour_AriaLabelIsIcelandicFourthPlace()
    {
        // Arrange
        CardLeading.RankLeading sut = new(4);

        // Act
        string? ariaLabel = sut.AriaLabel;

        // Assert
        ariaLabel.ShouldBe("4. sæti");
    }

    [Fact]
    public void WhenRankIsGreaterThanThree_AriaLabelIsIcelandicPlacement()
    {
        // Arrange
        CardLeading.RankLeading sut = new(10);

        // Act
        string? ariaLabel = sut.AriaLabel;

        // Assert
        ariaLabel.ShouldBe("10. sæti");
    }

    [Fact]
    public void WhenRankIsZero_AriaLabelIsNull()
    {
        // Arrange
        CardLeading.RankLeading sut = new(0);

        // Act
        string? ariaLabel = sut.AriaLabel;

        // Assert
        ariaLabel.ShouldBeNull();
    }

    [Fact]
    public void WhenRankIsNegative_AriaLabelIsNull()
    {
        // Arrange
        CardLeading.RankLeading sut = new(-1);

        // Act
        string? ariaLabel = sut.AriaLabel;

        // Assert
        ariaLabel.ShouldBeNull();
    }
}

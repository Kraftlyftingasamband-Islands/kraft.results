using Bunit;

using KRAFT.Results.Web.Client.Components.Cards;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Components.Cards;

public sealed class PillTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void WhenKindIsWeightCategory_RendersEscPillClass()
    {
        // Arrange
        MetaPill pill = new(PillKind.WeightCategory, "-83kg");

        // Act
        IRenderedComponent<Pill> cut = _context.Render<Pill>(
            p => p.Add(c => c.Value, pill));

        // Assert
        cut.Find(".esc-pill").ShouldNotBeNull();
    }

    [Fact]
    public void WhenKindIsWeightCategory_RendersWeightVariantClass()
    {
        // Arrange
        MetaPill pill = new(PillKind.WeightCategory, "-83kg");

        // Act
        IRenderedComponent<Pill> cut = _context.Render<Pill>(
            p => p.Add(c => c.Value, pill));

        // Assert
        cut.Find(".esc-pill--weight").ShouldNotBeNull();
    }

    [Fact]
    public void WhenKindIsAgeCategory_RendersAgeVariantClass()
    {
        // Arrange
        MetaPill pill = new(PillKind.AgeCategory, "Open");

        // Act
        IRenderedComponent<Pill> cut = _context.Render<Pill>(
            p => p.Add(c => c.Value, pill));

        // Assert
        cut.Find(".esc-pill--age").ShouldNotBeNull();
    }

    [Fact]
    public void WhenKindIsEquipmentClassic_RendersEquipmentClassicVariantClass()
    {
        // Arrange
        MetaPill pill = new(PillKind.EquipmentClassic, "Klassískt");

        // Act
        IRenderedComponent<Pill> cut = _context.Render<Pill>(
            p => p.Add(c => c.Value, pill));

        // Assert
        cut.Find(".esc-pill--equipment-classic").ShouldNotBeNull();
    }

    [Fact]
    public void WhenKindIsEquipmentEquipped_RendersEquipmentEquippedVariantClass()
    {
        // Arrange
        MetaPill pill = new(PillKind.EquipmentEquipped, "Búnaður");

        // Act
        IRenderedComponent<Pill> cut = _context.Render<Pill>(
            p => p.Add(c => c.Value, pill));

        // Assert
        cut.Find(".esc-pill--equipment-equipped").ShouldNotBeNull();
    }

    [Fact]
    public void WhenKindIsEquipmentClassic_RendersText()
    {
        // Arrange
        MetaPill pill = new(PillKind.EquipmentClassic, "Klassískt");

        // Act
        IRenderedComponent<Pill> cut = _context.Render<Pill>(
            p => p.Add(c => c.Value, pill));

        // Assert
        cut.Find(".esc-pill").TextContent.Trim().ShouldBe("Klassískt");
    }

    [Fact]
    public void WhenKindIsEquipmentEquipped_RendersText()
    {
        // Arrange
        MetaPill pill = new(PillKind.EquipmentEquipped, "Búnaður");

        // Act
        IRenderedComponent<Pill> cut = _context.Render<Pill>(
            p => p.Add(c => c.Value, pill));

        // Assert
        cut.Find(".esc-pill").TextContent.Trim().ShouldBe("Búnaður");
    }

    [Fact]
    public void WhenKindIsNeutral_RendersNeutralVariantClass()
    {
        // Arrange
        MetaPill pill = new(PillKind.Neutral, "IPF");

        // Act
        IRenderedComponent<Pill> cut = _context.Render<Pill>(
            p => p.Add(c => c.Value, pill));

        // Assert
        cut.Find(".esc-pill--neutral").ShouldNotBeNull();
    }

    [Fact]
    public void WhenPillHasText_RendersText()
    {
        // Arrange
        MetaPill pill = new(PillKind.WeightCategory, "-83kg");

        // Act
        IRenderedComponent<Pill> cut = _context.Render<Pill>(
            p => p.Add(c => c.Value, pill));

        // Assert
        cut.Find(".esc-pill").TextContent.Trim().ShouldBe("-83kg");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

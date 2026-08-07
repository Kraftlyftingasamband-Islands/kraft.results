using Bunit;

using KRAFT.Results.Web.Client.Components.Cards;

using Microsoft.AspNetCore.Components;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Components.Cards;

public sealed class EntityStatCardTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void WhenLeadingIsRank1_RendersGoldMedalSpanWithRoleImg()
    {
        // Arrange
        CardLeading leading = CardLeading.Rank(1);

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Leading, leading)
                .Add(c => c.Name, "Athlete Name")
                .Add(c => c.Value, "500"));

        // Assert
        cut.Find(".esc-leading--medal").GetAttribute("role").ShouldBe("img");
    }

    [Fact]
    public void WhenLeadingIsRank2_RendersAriaLabelWithPlacement()
    {
        // Arrange
        CardLeading leading = CardLeading.Rank(2);

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Leading, leading)
                .Add(c => c.Name, "Athlete Name")
                .Add(c => c.Value, "500"));

        // Assert
        cut.Find(".esc-leading--medal").GetAttribute("aria-label").ShouldBe("2. sæti");
    }

    [Fact]
    public void WhenLeadingIsRank3_RendersBronzeMedalSpan()
    {
        // Arrange
        CardLeading leading = CardLeading.Rank(3);

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Leading, leading)
                .Add(c => c.Name, "Athlete Name")
                .Add(c => c.Value, "500"));

        // Assert
        cut.Find(".esc-leading--medal").ShouldNotBeNull();
    }

    [Fact]
    public void WhenLeadingIsRankGreaterThan3_RendersRankSpanWithNumber()
    {
        // Arrange
        CardLeading leading = CardLeading.Rank(4);

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Leading, leading)
                .Add(c => c.Name, "Athlete Name")
                .Add(c => c.Value, "500"));

        // Assert
        AngleSharp.Dom.IElement rankSpan = cut.Find(".esc-leading--rank");
        rankSpan.TextContent.Trim().ShouldBe("4");
    }

    [Fact]
    public void WhenLeadingIsRankGreaterThan3_DoesNotRenderMedalSpan()
    {
        // Arrange
        CardLeading leading = CardLeading.Rank(5);

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Leading, leading)
                .Add(c => c.Name, "Athlete Name")
                .Add(c => c.Value, "500"));

        // Assert
        cut.FindAll(".esc-leading--medal").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenLeadingIsBadge_RendersBadgeSpanWithText()
    {
        // Arrange
        CardLeading leading = CardLeading.Badge("NEW");

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Leading, leading)
                .Add(c => c.Name, "Athlete Name")
                .Add(c => c.Value, "500"));

        // Assert
        AngleSharp.Dom.IElement badgeSpan = cut.Find(".esc-leading--badge");
        badgeSpan.TextContent.Trim().ShouldBe("NEW");
    }

    [Fact]
    public void WhenLeadingIsBadgeWithNoHref_RendersBadgeAsSpan()
    {
        // Arrange
        CardLeading leading = CardLeading.Badge("83");

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Leading, leading)
                .Add(c => c.Name, "Athlete Name")
                .Add(c => c.Value, "500"));

        // Assert
        AngleSharp.Dom.IElement badgeEl = cut.Find(".esc-leading--badge");
        badgeEl.TagName.ShouldBe("SPAN");
    }

    [Fact]
    public void WhenLeadingIsBadgeWithHref_RendersBadgeAsAnchor()
    {
        // Arrange
        CardLeading leading = CardLeading.Badge("83", "/records/1/history");

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Leading, leading)
                .Add(c => c.Name, "Athlete Name")
                .Add(c => c.Value, "500"));

        // Assert
        AngleSharp.Dom.IElement badgeEl = cut.Find(".esc-leading--badge");
        badgeEl.TagName.ShouldBe("A");
        badgeEl.GetAttribute("href").ShouldBe("/records/1/history");
    }

    [Fact]
    public void WhenLeadingIsBadgeWithNonRelativeHref_RendersBadgeAsSpanNotAnchor()
    {
        // Arrange
        CardLeading leading = CardLeading.Badge("83", "https://evil.example.com");

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Leading, leading)
                .Add(c => c.Name, "Athlete Name")
                .Add(c => c.Value, "500"));

        // Assert
        AngleSharp.Dom.IElement badgeEl = cut.Find(".esc-leading--badge");
        badgeEl.TagName.ShouldBe("SPAN");
    }

    [Fact]
    public void WhenLeadingIsBadgeWithHrefAndAriaLabel_RendersAnchorWithAriaLabel()
    {
        // Arrange
        CardLeading leading = CardLeading.Badge("-59", "/records/7/history", "Skoða met-sögu fyrir -59 kg");

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Leading, leading)
                .Add(c => c.Name, "Athlete Name")
                .Add(c => c.Value, "500"));

        // Assert
        AngleSharp.Dom.IElement badgeEl = cut.Find("a.esc-leading--badge");
        badgeEl.GetAttribute("aria-label").ShouldBe("Skoða met-sögu fyrir -59 kg");
    }

    [Fact]
    public void WhenLeadingIsBadgeWithHrefButNoAriaLabel_RendersAnchorWithNoAriaLabelAttribute()
    {
        // Arrange
        CardLeading leading = CardLeading.Badge("83", "/records/1/history");

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Leading, leading)
                .Add(c => c.Name, "Athlete Name")
                .Add(c => c.Value, "500"));

        // Assert
        AngleSharp.Dom.IElement badgeEl = cut.Find("a.esc-leading--badge");
        badgeEl.GetAttribute("aria-label").ShouldBeNull();
    }

    [Fact]
    public void WhenLeadingIsBadgeWithNoHref_SpanHasNoAriaLabelAttribute()
    {
        // Arrange
        CardLeading leading = CardLeading.Badge("83");

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Leading, leading)
                .Add(c => c.Name, "Athlete Name")
                .Add(c => c.Value, "500"));

        // Assert
        AngleSharp.Dom.IElement badgeEl = cut.Find("span.esc-leading--badge");
        badgeEl.GetAttribute("aria-label").ShouldBeNull();
    }

    [Fact]
    public void WhenNameHrefIsNull_RendersNameAsSpan()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Jón Jónsson")
                .Add(c => c.Value, "500"));

        // Assert
        AngleSharp.Dom.IElement nameSpan = cut.Find(".esc-name");
        nameSpan.TagName.ShouldBe("SPAN");
    }

    [Fact]
    public void WhenNameHrefIsNull_RendersNoAnchorInNameRow()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Jón Jónsson")
                .Add(c => c.Value, "500"));

        // Assert
        cut.FindAll(".esc-name-row a").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenNameHrefIsSet_RendersNameAsNavLink()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Jón Jónsson")
                .Add(c => c.NameHref, "/athletes/jon-jonsson")
                .Add(c => c.Value, "500"));

        // Assert
        AngleSharp.Dom.IElement nameLink = cut.Find(".esc-name");
        nameLink.TagName.ShouldBe("A");
    }

    [Fact]
    public void WhenNameHrefIsSet_RendersNameWithCorrectHref()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Jón Jónsson")
                .Add(c => c.NameHref, "/athletes/jon-jonsson")
                .Add(c => c.Value, "500"));

        // Assert
        cut.Find(".esc-name").GetAttribute("href").ShouldBe("/athletes/jon-jonsson");
    }

    [Fact]
    public void WhenValueHrefIsNull_RendersValueAsSpan()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750"));

        // Assert
        AngleSharp.Dom.IElement valueEl = cut.Find(".esc-value");
        valueEl.TagName.ShouldBe("SPAN");
    }

    [Fact]
    public void WhenValueHrefIsSet_RendersValueAsNavLink()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750")
                .Add(c => c.ValueHref, "/meets/nationals-2025")
                .Add(c => c.ValueAriaLabel, "750 total"));

        // Assert
        AngleSharp.Dom.IElement valueEl = cut.Find(".esc-value");
        valueEl.TagName.ShouldBe("A");
    }

    [Fact]
    public void WhenValueHrefAndAriaLabelAreSet_RendersAriaLabel()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750")
                .Add(c => c.ValueHref, "/meets/nationals-2025")
                .Add(c => c.ValueAriaLabel, "750 total"));

        // Assert
        cut.Find(".esc-value").GetAttribute("aria-label").ShouldBe("750 total");
    }

    [Fact]
    public void WhenUnitIsSet_RendersUnitInsideValueElement()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750")
                .Add(c => c.Unit, "kg"));

        // Assert
        AngleSharp.Dom.IElement unitSpan = cut.Find(".esc-value .esc-unit");
        unitSpan.TextContent.Trim().ShouldBe("kg");
    }

    [Fact]
    public void WhenPillsAreOutOfOrder_RendersSortedByKind()
    {
        // Arrange
        IReadOnlyList<MetaPill> pills =
        [
            new(PillKind.Neutral, "IPF"),
            new(PillKind.WeightCategory, "-83kg"),
        ];

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750")
                .Add(c => c.Pills, pills));

        // Assert
        List<AngleSharp.Dom.IElement> pillEls = cut.FindAll(".esc-pills .esc-pill").ToList();
        pillEls.Count.ShouldBe(2);

        // WeightCategory (1) sorts before Neutral (7)
        pillEls[0].TextContent.Trim().ShouldBe("-83kg");
        pillEls[1].TextContent.Trim().ShouldBe("IPF");
    }

    [Fact]
    public void WhenPillsIsEmpty_DoesNotRenderPillsContainer()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750"));

        // Assert
        cut.FindAll(".esc-pills").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenLeadingIsNull_DoesNotRenderLeadingSlot()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750"));

        // Assert
        cut.FindAll(".esc-leading").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenImageIsNull_DoesNotRenderImageSlot()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750"));

        // Assert
        cut.FindAll(".esc-image").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenImageIsProvided_RendersImageSlot()
    {
        // Arrange
        RenderFragment imageFragment = builder =>
        {
            builder.OpenElement(0, "img");
            builder.AddAttribute(1, "src", "/logo.png");
            builder.AddAttribute(2, "alt", "team logo");
            builder.CloseElement();
        };

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750")
                .Add(c => c.Image, imageFragment));

        // Assert
        cut.Find(".esc-image").ShouldNotBeNull();
    }

    [Fact]
    public void WhenMetaTextIsNull_DoesNotRenderMetaLine()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750"));

        // Assert
        cut.FindAll(".esc-meta").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenMetaTextIsSet_RendersMetaLine()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750")
                .Add(c => c.MetaText, "Reykjavík Powerlifting Club"));

        // Assert
        cut.Find(".esc-meta").TextContent.Trim().ShouldBe("Reykjavík Powerlifting Club");
    }

    [Fact]
    public void WhenLabelIsNull_DoesNotRenderLabelElement()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750"));

        // Assert
        cut.FindAll(".esc-label").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenLabelIsSet_RendersLabelElement()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750")
                .Add(c => c.Label, "Total"));

        // Assert
        cut.Find(".esc-label").TextContent.Trim().ShouldBe("Total");
    }

    [Fact]
    public void WhenUnitIsNull_DoesNotRenderUnitElement()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750"));

        // Assert
        cut.FindAll(".esc-unit").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenValueIsSet_RendersValueText()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "842.50"));

        // Assert
        cut.Find(".esc-value").TextContent.Trim().ShouldBe("842.50");
    }

    [Fact]
    public void WhenNameIsSet_RendersNameText()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Sigríður Halldórsdóttir")
                .Add(c => c.Value, "500"));

        // Assert
        cut.Find(".esc-name").TextContent.Trim().ShouldBe("Sigríður Halldórsdóttir");
    }

    [Fact]
    public void WhenCssClassIsSet_PassesThroughToCard()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750")
                .Add(c => c.CssClass, "sc-card-first"));

        // Assert
        cut.Find(".sc-card-first").ShouldNotBeNull();
    }

    [Fact]
    public void WhenNameHrefIsJavascriptScheme_RendersNameAsSpanNotAnchor()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.NameHref, "javascript:alert(1)")
                .Add(c => c.Value, "500"));

        // Assert
        AngleSharp.Dom.IElement nameEl = cut.Find(".esc-name");
        nameEl.TagName.ShouldBe("SPAN");
    }

    [Fact]
    public void WhenValueHrefHasNoLeadingSlashOrHash_RendersValueAsSpanNotAnchor()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "500")
                .Add(c => c.ValueHref, "https://evil.example.com"));

        // Assert
        AngleSharp.Dom.IElement valueEl = cut.Find(".esc-value");
        valueEl.TagName.ShouldBe("SPAN");
    }

    [Fact]
    public void WhenValueHrefIsSetButValueAriaLabelIsEmpty_DoesNotRenderAriaLabelAttribute()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750")
                .Add(c => c.ValueHref, "/meets/nationals-2025")
                .Add(c => c.ValueAriaLabel, string.Empty));

        // Assert
        cut.Find(".esc-value").GetAttribute("aria-label").ShouldBeNull();
    }

    [Fact]
    public void WhenValueHrefIsSetAndValueAriaLabelIsNull_DoesNotRenderAriaLabelAttribute()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750")
                .Add(c => c.ValueHref, "/meets/nationals-2025"));

        // Assert
        cut.Find(".esc-value").GetAttribute("aria-label").ShouldBeNull();
    }

    [Fact]
    public void WhenNoLeadingAndNoImage_BodyAndStatArePresent()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750"));

        // Assert
        cut.Find(".esc-body").ShouldNotBeNull();
        cut.Find(".esc-stat").ShouldNotBeNull();
    }

    [Fact]
    public void WhenLeadingIsRank0_RendersRankSpanWithEnDashAndNoMedalClass()
    {
        // Arrange
        CardLeading leading = CardLeading.Rank(0);

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Leading, leading)
                .Add(c => c.Name, "Athlete Name")
                .Add(c => c.Value, "500"));

        // Assert
        AngleSharp.Dom.IElement rankSpan = cut.Find(".esc-leading--rank");
        rankSpan.TextContent.Trim().ShouldBe("–");
        cut.FindAll(".esc-leading--medal").Count.ShouldBe(0);
    }

    // Fix 1 — protocol-relative URL rejection
    [Fact]
    public void WhenNameHrefIsProtocolRelative_RendersNameAsSpanNotAnchor()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.NameHref, "//attacker.example.com")
                .Add(c => c.Value, "500"));

        // Assert
        AngleSharp.Dom.IElement nameEl = cut.Find(".esc-name");
        nameEl.TagName.ShouldBe("SPAN");
    }

    [Fact]
    public void WhenValueHrefIsProtocolRelative_RendersValueAsSpanNotAnchor()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "500")
                .Add(c => c.ValueHref, "//attacker.example.com"));

        // Assert
        AngleSharp.Dom.IElement valueEl = cut.Find(".esc-value");
        valueEl.TagName.ShouldBe("SPAN");
    }

    // Fix 2 — non-medal rank accessible label
    [Fact]
    public void WhenLeadingIsNonMedalRank_RendersRoleImgOnRankSpan()
    {
        // Arrange
        CardLeading leading = CardLeading.Rank(4);

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Leading, leading)
                .Add(c => c.Name, "Athlete Name")
                .Add(c => c.Value, "500"));

        // Assert
        cut.Find(".esc-leading--rank").GetAttribute("role").ShouldBe("img");
    }

    [Fact]
    public void WhenLeadingIsNonMedalRank_RendersAriaLabelOnRankSpan()
    {
        // Arrange
        CardLeading leading = CardLeading.Rank(4);

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Leading, leading)
                .Add(c => c.Name, "Athlete Name")
                .Add(c => c.Value, "500"));

        // Assert
        cut.Find(".esc-leading--rank").GetAttribute("aria-label").ShouldBe("4. sæti");
    }

    [Fact]
    public void WhenLeadingIsRank0_RendersNoRoleOrAriaLabelOnRankSpan()
    {
        // Arrange
        CardLeading leading = CardLeading.Rank(0);

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Leading, leading)
                .Add(c => c.Name, "Athlete Name")
                .Add(c => c.Value, "500"));

        // Assert
        AngleSharp.Dom.IElement rankSpan = cut.Find(".esc-leading--rank");
        rankSpan.GetAttribute("role").ShouldBeNull();
        rankSpan.GetAttribute("aria-label").ShouldBeNull();
    }

    // Fix 3 — clipped entity name title fallback
    [Fact]
    public void WhenNameHrefIsNull_NameSpanHasTitleAttribute()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Sigríður Halldórsdóttir")
                .Add(c => c.Value, "500"));

        // Assert
        cut.Find(".esc-name").GetAttribute("title").ShouldBe("Sigríður Halldórsdóttir");
    }

    [Fact]
    public void WhenNameHrefIsSet_NameNavLinkHasTitleAttribute()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Sigríður Halldórsdóttir")
                .Add(c => c.NameHref, "/athletes/sigridur")
                .Add(c => c.Value, "500"));

        // Assert
        cut.Find(".esc-name").GetAttribute("title").ShouldBe("Sigríður Halldórsdóttir");
    }

    [Fact]
    public void WhenMetaContentIsProvided_RendersInsideRichMetaDiv()
    {
        // Arrange
        RenderFragment metaFragment = builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddContent(1, "2024-01-15");
            builder.CloseElement();
        };

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "300")
                .Add(c => c.MetaContent, metaFragment));

        // Assert
        AngleSharp.Dom.IElement richMeta = cut.Find(".esc-meta.esc-meta--rich");
        richMeta.ShouldNotBeNull();
        richMeta.QuerySelector("span")!.TextContent.ShouldBe("2024-01-15");
    }

    [Fact]
    public void WhenMetaContentAndMetaTextAreBothSet_MetaContentTakesPrecedence()
    {
        // Arrange
        RenderFragment metaFragment = builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddContent(1, "rich content");
            builder.CloseElement();
        };

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "300")
                .Add(c => c.MetaContent, metaFragment)
                .Add(c => c.MetaText, "plain text"));

        // Assert
        cut.Find(".esc-meta--rich").ShouldNotBeNull();
        cut.FindAll(".esc-meta").Count.ShouldBe(1);
    }

    [Fact]
    public void WhenBothMetaContentAndMetaTextAreNull_DoesNotRenderAnyMetaElement()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "300"));

        // Assert
        cut.FindAll(".esc-meta").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenValueAndLabelAreBothNull_DoesNotRenderStatColumn()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete"));

        // Assert
        cut.FindAll(".esc-stat").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenValueIsPresent_RendersStatColumn()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750"));

        // Assert
        cut.Find(".esc-stat").ShouldNotBeNull();
    }

    [Fact]
    public void WhenClickableIsTrue_RendersCardClickableClass()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750")
                .Add(c => c.Clickable, true));

        // Assert
        cut.Find("article.card--clickable").ShouldNotBeNull();
    }

    [Fact]
    public void WhenClickableIsFalse_DoesNotRenderCardClickableClass()
    {
        // Arrange

        // Act
        IRenderedComponent<EntityStatCard> cut = _context.Render<EntityStatCard>(
            p => p
                .Add(c => c.Name, "Athlete")
                .Add(c => c.Value, "750")
                .Add(c => c.Clickable, false));

        // Assert
        cut.FindAll("article.card--clickable").Count.ShouldBe(0);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

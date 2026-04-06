using Bunit;

using KRAFT.Results.Web.Client.Components;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Components;

public sealed class ToggleSwitchTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void RendersLabelText()
    {
        // Arrange & Act
        IRenderedComponent<ToggleSwitch> cut = _context.Render<ToggleSwitch>(
            p => p.Add(c => c.Label, "Sýna lið")
                  .Add(c => c.Id, "show-teams"));

        // Assert
        cut.Find(".toggle-label").TextContent.ShouldBe("Sýna lið");
    }

    [Fact]
    public void ButtonHasRoleSwitchAndAriaCheckedFalse_WhenOff()
    {
        // Arrange & Act
        IRenderedComponent<ToggleSwitch> cut = _context.Render<ToggleSwitch>(
            p => p.Add(c => c.Value, false)
                  .Add(c => c.Id, "test-toggle"));

        // Assert
        AngleSharp.Dom.IElement button = cut.Find("button");
        button.GetAttribute("role").ShouldBe("switch");
        button.GetAttribute("aria-checked").ShouldBe("false");
        button.ClassList.ShouldNotContain("is-on");
    }

    [Fact]
    public void ButtonHasAriaCheckedTrueAndIsOnClass_WhenOn()
    {
        // Arrange & Act
        IRenderedComponent<ToggleSwitch> cut = _context.Render<ToggleSwitch>(
            p => p.Add(c => c.Value, true)
                  .Add(c => c.Id, "test-toggle"));

        // Assert
        AngleSharp.Dom.IElement button = cut.Find("button");
        button.GetAttribute("aria-checked").ShouldBe("true");
        button.ClassList.ShouldContain("is-on");
    }

    [Fact]
    public void ClickInvokesValueChangedWithOppositeValue()
    {
        // Arrange
        bool newValue = false;
        IRenderedComponent<ToggleSwitch> cut = _context.Render<ToggleSwitch>(
            p => p.Add(c => c.Value, false)
                  .Add(c => c.ValueChanged, (bool v) => newValue = v)
                  .Add(c => c.Id, "test-toggle"));

        // Act
        cut.Find("button").Click();

        // Assert
        newValue.ShouldBeTrue();
    }

    [Fact]
    public void ButtonIsDisabled_WhenDisabledIsTrue()
    {
        // Arrange & Act
        IRenderedComponent<ToggleSwitch> cut = _context.Render<ToggleSwitch>(
            p => p.Add(c => c.Value, false)
                  .Add(c => c.Disabled, true)
                  .Add(c => c.Id, "test-toggle"));

        // Assert
        cut.Find("button").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void AriaLabelPointsToLabel()
    {
        // Arrange & Act
        IRenderedComponent<ToggleSwitch> cut = _context.Render<ToggleSwitch>(
            p => p.Add(c => c.Id, "my-toggle")
                  .Add(c => c.Label, "Test"));

        // Assert
        AngleSharp.Dom.IElement button = cut.Find("button");
        button.GetAttribute("id").ShouldBe("my-toggle");
        button.GetAttribute("aria-label").ShouldBe("Test");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
using Bunit;

using KRAFT.Results.Web.Client.Components;

using Microsoft.AspNetCore.Components;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Components;

public sealed class DataLoaderTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void ShowsLoadingSpinner_WhenDataIsBeingFetched()
    {
        // Arrange
        TaskCompletionSource<string?> tcs = new();
        RenderFragment<string> childContent = _ => builder => { };

        // Act
        IRenderedComponent<DataLoader<string>> cut = _context.Render<DataLoader<string>>(
            p => p.Add(c => c.Loader, () => tcs.Task)
                  .Add(c => c.Noun, "gögn")
                  .Add(c => c.ChildContent, childContent));

        // Assert
        cut.Find("[role='status']").ShouldNotBeNull();
        cut.Find(".visually-hidden").TextContent.ShouldBe("Sæki gögn...");
    }

    [Fact]
    public void ShowsErrorMessage_WhenLoaderThrows()
    {
        // Arrange
        static async Task<string?> FailingLoader()
        {
            await Task.Yield();
            throw new HttpRequestException("Server error");
        }

        RenderFragment<string> childContent = _ => builder => { };

        // Act
        IRenderedComponent<DataLoader<string>> cut = _context.Render<DataLoader<string>>(
            p => p.Add(c => c.Loader, FailingLoader)
                  .Add(c => c.Noun, "gögn")
                  .Add(c => c.ChildContent, childContent));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find("[role='alert']").TextContent.ShouldContain("gögn");
        });
    }

    [Fact]
    public void ShowsNotFoundMessage_WhenLoaderReturnsNull()
    {
        // Arrange
        static Task<string?> NullLoader() => Task.FromResult<string?>(null);
        RenderFragment<string> childContent = _ => builder => { };

        // Act
        IRenderedComponent<DataLoader<string>> cut = _context.Render<DataLoader<string>>(
            p => p.Add(c => c.Loader, NullLoader)
                  .Add(c => c.Noun, "keppanda")
                  .Add(c => c.ChildContent, childContent));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find("[role='alert']").TextContent.ShouldContain("Keppanda");
            cut.Find("[role='alert']").TextContent.ShouldContain("fannst ekki");
        });
    }

    [Fact]
    public void RendersChildContent_WhenLoaderReturnsData()
    {
        // Arrange
        static Task<string?> SuccessLoader() => Task.FromResult<string?>("hello");
        RenderFragment<string> childContent = data => builder =>
            builder.AddMarkupContent(0, $"<span class='result'>{data}</span>");

        // Act
        IRenderedComponent<DataLoader<string>> cut = _context.Render<DataLoader<string>>(
            p => p.Add(c => c.Loader, SuccessLoader)
                  .Add(c => c.Noun, "gögn")
                  .Add(c => c.ChildContent, childContent));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find(".result").TextContent.ShouldBe("hello");
        });
    }

    [Fact]
    public void ErrorState_HasRetryButton_ThatResetsToLoading()
    {
        // Arrange
        TaskCompletionSource<string?> tcs = new();
        int callCount = 0;
        RenderFragment<string> childContent = _ => builder => { };

        Task<string?> Loader()
        {
            callCount++;
            if (callCount == 1)
            {
                throw new HttpRequestException("fail");
            }

            return tcs.Task;
        }

        // Act
        IRenderedComponent<DataLoader<string>> cut = _context.Render<DataLoader<string>>(
            p => p.Add(c => c.Loader, Loader)
                  .Add(c => c.Noun, "gögn")
                  .Add(c => c.ChildContent, childContent));

        cut.WaitForAssertion(() =>
        {
            cut.Find(".retry-btn").ShouldNotBeNull();
        });

        // Act — click retry resets to loading state
        cut.Find(".retry-btn").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='status']").ShouldNotBeNull();
        });
    }

    [Fact]
    public void NotFoundState_DoesNotHaveRetryButton()
    {
        // Arrange
        static Task<string?> NullLoader() => Task.FromResult<string?>(null);
        RenderFragment<string> childContent = _ => builder => { };

        // Act
        IRenderedComponent<DataLoader<string>> cut = _context.Render<DataLoader<string>>(
            p => p.Add(c => c.Loader, NullLoader)
                  .Add(c => c.Noun, "gögn")
                  .Add(c => c.ChildContent, childContent));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".retry-btn").Count.ShouldBe(0);
        });
    }

    [Fact]
    public void CapitalizesFirstLetterOfNoun_InNotFoundMessage()
    {
        // Arrange
        static Task<string?> NullLoader() => Task.FromResult<string?>(null);
        RenderFragment<string> childContent = _ => builder => { };

        // Act
        IRenderedComponent<DataLoader<string>> cut = _context.Render<DataLoader<string>>(
            p => p.Add(c => c.Loader, NullLoader)
                  .Add(c => c.Noun, "mót")
                  .Add(c => c.ChildContent, childContent));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").TextContent.ShouldContain("Mót fannst ekki");
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
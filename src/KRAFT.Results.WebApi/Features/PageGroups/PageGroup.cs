using KRAFT.Results.WebApi.Features.Pages;

namespace KRAFT.Results.WebApi.Features.PageGroups;

internal sealed class PageGroup
{
    public int PageGroupId { get; private set; }

    public string Title { get; private set; } = null!;

    public DateTime CreatedOn { get; private set; }

    public ICollection<Page> Pages { get; } = [];
}
using KRAFT.Results.Core.Features.Pages;

namespace KRAFT.Results.Core.Features.PageGroups;

internal sealed class PageGroup
{
    public int PageGroupId { get; set; }

    public string Title { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public ICollection<Page> Pages { get; } = [];
}
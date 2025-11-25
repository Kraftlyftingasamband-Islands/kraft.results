using KRAFT.Results.WebApi.Features.Pages;

namespace KRAFT.Results.WebApi.Features.PageGroups;

internal sealed class PageGroup
{
    public int PageGroupId { get; set; }

    public string Title { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public ICollection<Page> Pages { get; } = [];
}
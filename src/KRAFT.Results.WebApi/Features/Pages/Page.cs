using KRAFT.Results.WebApi.Features.PageGroups;

namespace KRAFT.Results.WebApi.Features.Pages;

internal sealed class Page
{
    public int PageId { get; private set; }

    public string Title { get; private set; } = null!;

    public string Text { get; private set; } = null!;

    public int PageGroupId { get; private set; }

    public DateTime CreatedOn { get; private set; }

    public DateTime ModifiedOn { get; private set; }

    public string ModifiedBy { get; private set; } = null!;

    public string CreatedBy { get; private set; } = null!;

    public PageGroup PageGroup { get; private set; } = null!;
}
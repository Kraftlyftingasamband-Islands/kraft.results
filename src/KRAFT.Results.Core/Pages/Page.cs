using KRAFT.Results.Core.PageGroups;

namespace KRAFT.Results.Core.Pages;

internal sealed class Page
{
    public int PageId { get; set; }

    public string Title { get; set; } = null!;

    public string Text { get; set; } = null!;

    public int PageGroupId { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public string CreatedBy { get; set; } = null!;

    public PageGroup PageGroup { get; set; } = null!;
}
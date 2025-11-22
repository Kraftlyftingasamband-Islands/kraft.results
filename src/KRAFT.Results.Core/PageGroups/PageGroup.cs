using KRAFT.Results.Core.Pages;

namespace KRAFT.Results.Core.PageGroups;

internal class PageGroup
{
    public int PageGroupId { get; set; }

    public string Title { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public virtual ICollection<Page> Pages { get; } = [];
}
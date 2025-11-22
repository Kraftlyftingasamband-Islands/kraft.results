namespace KRAFT.Results.Core.Cases;

internal class Case
{
    public int CaseId { get; set; }

    public string? FromEmail { get; set; }

    public string? FromName { get; set; }

    public string Text { get; set; } = null!;

    public string? Feedback { get; set; }

    public string? Url { get; set; }

    public int Status { get; set; }

    public DateTime? ClosedOn { get; set; }

    public DateTime CreatedOn { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime ModifiedOn { get; set; }

    public string ModifiedBy { get; set; } = null!;
}
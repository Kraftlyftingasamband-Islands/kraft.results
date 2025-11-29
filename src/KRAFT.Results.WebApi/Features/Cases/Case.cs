namespace KRAFT.Results.WebApi.Features.Cases;

internal sealed class Case
{
    public int CaseId { get; private set; }

    public string? FromEmail { get; private set; }

    public string? FromName { get; private set; }

    public string Text { get; private set; } = null!;

    public string? Feedback { get; private set; }

    public string? Url { get; private set; }

    public int Status { get; private set; }

    public DateTime? ClosedOn { get; private set; }

    public DateTime CreatedOn { get; private set; }

    public string CreatedBy { get; private set; } = null!;

    public DateTime ModifiedOn { get; private set; }

    public string ModifiedBy { get; private set; } = null!;
}
namespace KRAFT.Results.Contracts.Meets;

public sealed record MeetPhotos(string MeetTitle, IReadOnlyList<PhotoSummary> Photos);

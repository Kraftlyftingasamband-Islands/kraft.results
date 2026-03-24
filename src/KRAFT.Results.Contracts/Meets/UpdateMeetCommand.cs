namespace KRAFT.Results.Contracts.Meets;

public sealed record class UpdateMeetCommand(string Title, DateOnly StartDate, int? MeetTypeId);
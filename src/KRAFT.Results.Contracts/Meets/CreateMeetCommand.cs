namespace KRAFT.Results.Contracts.Meets;

public sealed record class CreateMeetCommand(string Title, DateOnly StartDate, int? MeetTypeId);
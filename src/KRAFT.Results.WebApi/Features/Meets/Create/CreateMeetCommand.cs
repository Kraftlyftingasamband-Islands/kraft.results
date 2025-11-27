namespace KRAFT.Results.WebApi.Features.Meets.Create;

internal sealed record class CreateMeetCommand(string Title, DateOnly StartDate, int? MeetTypeId);
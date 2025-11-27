namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class CreateMeetCommandBuilder
{
    private string _title = Guid.NewGuid().ToString();
    private DateOnly _startDate = DateOnly.FromDateTime(DateTime.UtcNow);
    private int? _meetTypeId;

    public CreateMeetCommandBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public CreateMeetCommandBuilder WithStartDate(DateOnly startDate)
    {
        _startDate = startDate;
        return this;
    }

    public CreateMeetCommandBuilder WithMeetTypeId(int? meetTypeId)
    {
        _meetTypeId = meetTypeId;
        return this;
    }

    public object Build()
    {
        return new
        {
            Title = _title,
            StartDate = _startDate,
            MeetTypeId = _meetTypeId,
        };
    }
}
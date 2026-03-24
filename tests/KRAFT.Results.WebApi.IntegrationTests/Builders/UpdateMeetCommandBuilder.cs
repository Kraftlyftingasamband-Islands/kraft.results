using KRAFT.Results.Contracts.Meets;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class UpdateMeetCommandBuilder
{
    private string _title = Guid.NewGuid().ToString();
    private DateOnly _startDate = DateOnly.FromDateTime(DateTime.UtcNow);
    private int? _meetTypeId;

    public UpdateMeetCommandBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public UpdateMeetCommandBuilder WithStartDate(DateOnly startDate)
    {
        _startDate = startDate;
        return this;
    }

    public UpdateMeetCommandBuilder WithMeetTypeId(int? meetTypeId)
    {
        _meetTypeId = meetTypeId;
        return this;
    }

    public UpdateMeetCommand Build() =>
        new(_title, _startDate, _meetTypeId);
}
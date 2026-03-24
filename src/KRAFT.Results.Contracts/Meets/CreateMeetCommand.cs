using System.ComponentModel.DataAnnotations;

namespace KRAFT.Results.Contracts.Meets;

public sealed record class CreateMeetCommand(
    [MaxLength(100, ErrorMessage = "Nafn má ekki vera lengra en 100 stafir")]
    string Title,
    DateOnly StartDate,
    int? MeetTypeId);
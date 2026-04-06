using System.ComponentModel.DataAnnotations;

namespace KRAFT.Results.Contracts.Meets;

public sealed record class AddParticipantCommand(
    string AthleteSlug,
    decimal BodyWeight,
    int? TeamId,
    [MaxLength(50, ErrorMessage = "Aldursflokkur má ekki vera lengri en 50 stafir")]
    string? AgeCategorySlug);
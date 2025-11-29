namespace KRAFT.Results.WebApi.Features.AthleteAliases;

internal sealed class AthleteAlias
{
    public int AthleteAliasId { get; private set; }

    public int AthleteId { get; private set; }

    public string Alias { get; private set; } = null!;

    public DateTime CreatedOn { get; private set; }
}
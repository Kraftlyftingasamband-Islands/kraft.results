namespace KRAFT.Results.Core.AthleteAliases;

internal sealed class AthleteAlias
{
    public int AthleteAliasId { get; set; }

    public int AthleteId { get; set; }

    public string Alias { get; set; } = null!;

    public DateTime CreatedOn { get; set; }
}
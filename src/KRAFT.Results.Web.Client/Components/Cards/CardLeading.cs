namespace KRAFT.Results.Web.Client.Components.Cards;

public abstract record CardLeading
{
    private CardLeading()
    {
    }

    public static CardLeading Rank(int value) => new RankLeading(value);

    public static CardLeading Badge(string text, string? href = null, string? ariaLabel = null) => new BadgeLeading(text, href, ariaLabel);

    // CA1034: Nested types are intentionally public — they are sealed variants of a discriminated union.
    // The private constructor prevents external subtyping; callers pattern-match on RankLeading/BadgeLeading.
#pragma warning disable CA1034
    public sealed record RankLeading(int Value) : CardLeading
    {
        public string Display => Value switch
        {
            1 => "\U0001f947",
            2 => "\U0001f948",
            3 => "\U0001f949",
            _ when Value > 0 => Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => "–",
        };

        public bool IsMedal => Value is 1 or 2 or 3;

        public string? AriaLabel => Value > 0
            ? $"{Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}. sæti"
            : null;
    }

    public sealed record BadgeLeading(string Text, string? Href = null, string? AriaLabel = null) : CardLeading;
#pragma warning restore CA1034
}

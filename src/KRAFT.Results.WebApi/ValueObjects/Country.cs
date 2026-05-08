using System.Collections.Frozen;
using System.Globalization;

using KRAFT.Results.Contracts.Countries;
using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.ValueObjects;

internal sealed class Country : ValueObject<string>
{
    private static readonly FrozenDictionary<string, RegionInfo> RegionByAlpha3 = BuildRegionLookup();

    private static readonly FrozenDictionary<string, string> IcelandicNames = new Dictionary<string, string>
    {
        // 17 in-use codes
        { "ISL", "Ísland" },
        { "NOR", "Noregur" },
        { "FIN", "Finnland" },
        { "SWE", "Svíþjóð" },
        { "DNK", "Danmörk" },
        { "GBR", "Bretland" },
        { "USA", "Bandaríkin" },
        { "HUN", "Ungverjaland" },
        { "NLD", "Holland" },
        { "FRO", "Færeyjar" },
        { "CZE", "Tékkland" },
        { "EST", "Eistland" },
        { "DEU", "Þýskaland" },
        { "ITA", "Ítalía" },
        { "POL", "Pólland" },
        { "UMI", "Smáeyjar Bandaríkjanna" },
        { "VIR", "Jómfrúaeyjar" },

        // Additional common countries
        { "AUT", "Austurríki" },
        { "BEL", "Belgía" },
        { "BGR", "Búlgaría" },
        { "BLR", "Hvíta-Rússland" },
        { "BRA", "Brasilía" },
        { "CAN", "Kanada" },
        { "CHE", "Sviss" },
        { "CHL", "Síle" },
        { "CHN", "Kína" },
        { "COL", "Kólumbía" },
        { "CYP", "Kýpur" },
        { "EGY", "Egyptaland" },
        { "ESP", "Spánn" },
        { "FRA", "Frakkland" },
        { "GRC", "Grikkland" },
        { "HRV", "Króatía" },
        { "IND", "Indland" },
        { "IRL", "Írland" },
        { "IRN", "Íran" },
        { "JPN", "Japan" },
        { "KAZ", "Kasakstan" },
        { "KOR", "Suður-Kórea" },
        { "LTU", "Litháen" },
        { "LVA", "Lettland" },
        { "MEX", "Mexíkó" },
        { "MKD", "Norður-Makedónía" },
        { "MLT", "Malta" },
        { "MNE", "Svartfjallaland" },
        { "NZL", "Nýja-Sjáland" },
        { "PRT", "Portúgal" },
        { "ROU", "Rúmenía" },
        { "RUS", "Rússland" },
        { "SRB", "Serbía" },
        { "SVK", "Slóvakía" },
        { "SVN", "Slóvenía" },
        { "TUR", "Tyrkland" },
        { "UKR", "Úkraína" },
        { "ZAF", "Suður-Afríka" },
    }
    .ToFrozenDictionary();

    private static readonly FrozenDictionary<string, (string Alpha3, string Alpha2, string EnglishName)> ObsoleteCodes =
        new Dictionary<string, (string, string, string)>
        {
            { "ANT", ("ANT", "AN", "Netherlands Antilles") },
            { "UMI", ("UMI", "UM", "U.S. Minor Outlying Islands") },
            { "VIR", ("VIR", "VI", "U.S. Virgin Islands") },
        }
        .ToFrozenDictionary();

    private Country(string alpha3, string alpha2, string englishName)
        : base(alpha3)
    {
        Alpha2 = alpha2;
        EnglishName = englishName;
    }

    public string Alpha3 => Value;

    public string Alpha2 { get; }

    public string EnglishName { get; }

    public string IcelandicName => IcelandicNames.TryGetValue(Alpha3, out string? name) ? name : EnglishName;

    public string DisplayName => IcelandicName;

    internal static Country Iceland { get; } = Parse("ISL");

    internal static Result<Country> FromCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new Error("Country.InvalidCode", "Country code must not be empty.");
        }

        string upper = code.Trim().ToUpperInvariant();

        if (ObsoleteCodes.TryGetValue(upper, out (string Alpha3, string Alpha2, string EnglishName) obsolete))
        {
            return new Country(obsolete.Alpha3, obsolete.Alpha2, obsolete.EnglishName);
        }

        if (!RegionByAlpha3.TryGetValue(upper, out RegionInfo? region))
        {
            return new Error("Country.InvalidCode", $"'{code}' is not a valid ISO 3166-1 alpha-3 country code.");
        }

        return new Country(region.ThreeLetterISORegionName, region.TwoLetterISORegionName, region.EnglishName);
    }

    internal static Country Parse(string code) => FromCode(code).FromResult();

    internal static IReadOnlyList<CountrySummary> GetAll()
    {
        List<CountrySummary> countries = RegionByAlpha3
            .Values
            .DistinctBy(r => r.ThreeLetterISORegionName)
            .Select(r => ToSummary(r.ThreeLetterISORegionName, r.EnglishName))
            .ToList();

        foreach (KeyValuePair<string, (string Alpha3, string Alpha2, string EnglishName)> obsolete in ObsoleteCodes)
        {
            if (countries.Any(c => c.Code == obsolete.Key))
            {
                continue;
            }

            countries.Add(ToSummary(obsolete.Key, obsolete.Value.EnglishName));
        }

        return countries
            .OrderBy(c => c.Name)
            .ToList();
    }

    private static CountrySummary ToSummary(string alpha3, string fallbackEnglishName)
    {
        string name = IcelandicNames.TryGetValue(alpha3, out string? icelandicName)
            ? icelandicName
            : fallbackEnglishName;

        return new CountrySummary(alpha3, name);
    }

    private static FrozenDictionary<string, RegionInfo> BuildRegionLookup()
    {
        Dictionary<string, RegionInfo> lookup = [];

        foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            RegionInfo region = new(culture.Name);
            string alpha3 = region.ThreeLetterISORegionName;

            if (!lookup.ContainsKey(alpha3))
            {
                lookup[alpha3] = region;
            }
        }

        return lookup.ToFrozenDictionary();
    }
}
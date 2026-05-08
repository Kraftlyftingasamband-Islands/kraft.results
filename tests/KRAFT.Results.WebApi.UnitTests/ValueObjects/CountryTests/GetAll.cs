using KRAFT.Results.Contracts.Countries;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.CountryTests;

public sealed class GetAll
{
    [Fact]
    public void ReturnsNonEmptyList()
    {
        // Act
        IReadOnlyList<CountrySummary> countries = Country.GetAll();

        // Assert
        countries.ShouldNotBeEmpty();
    }

    [Fact]
    public void ContainsAllInUseCountries()
    {
        // Arrange
        string[] inUseCodes = ["ISL", "NOR", "FIN", "SWE", "DNK", "GBR", "USA", "HUN", "NLD", "FRO", "CZE", "EST", "DEU", "ITA", "POL", "UMI", "VIR"];

        // Act
        IReadOnlyList<CountrySummary> countries = Country.GetAll();

        // Assert
        foreach (string code in inUseCodes)
        {
            countries.ShouldContain(c => c.Code == code, $"Missing country code: {code}");
        }
    }

    [Fact]
    public void ReturnsIcelandicNameForKnownCodes()
    {
        // Act
        IReadOnlyList<CountrySummary> countries = Country.GetAll();

        // Assert
        CountrySummary? iceland = countries.FirstOrDefault(c => c.Code == "ISL");
        iceland.ShouldNotBeNull();
        iceland.Name.ShouldBe("Ísland");
    }

    [Fact]
    public void IsOrderedByName()
    {
        // Act
        IReadOnlyList<CountrySummary> countries = Country.GetAll();

        // Assert
        List<string> names = countries.Select(c => c.Name).ToList();
        names.ShouldBeInOrder();
    }

    [Fact]
    public void HasUniqueCountryCodes()
    {
        // Act
        IReadOnlyList<CountrySummary> countries = Country.GetAll();

        // Assert
        countries.DistinctBy(c => c.Code).Count().ShouldBe(countries.Count);
    }
}
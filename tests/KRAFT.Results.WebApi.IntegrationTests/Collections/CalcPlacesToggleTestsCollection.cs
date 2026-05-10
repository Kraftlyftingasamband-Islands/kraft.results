using System.Diagnostics.CodeAnalysis;

namespace KRAFT.Results.WebApi.IntegrationTests.Collections;

[CollectionDefinition(nameof(CalcPlacesToggleTestsCollection))]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit collection definition")]
public sealed class CalcPlacesToggleTestsCollection : ICollectionFixture<CollectionFixture>
{
}
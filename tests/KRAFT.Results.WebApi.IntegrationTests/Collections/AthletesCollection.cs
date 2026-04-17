using System.Diagnostics.CodeAnalysis;

namespace KRAFT.Results.WebApi.IntegrationTests.Collections;

[CollectionDefinition(nameof(AthletesCollection))]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit collection definition")]
public class AthletesCollection : ICollectionFixture<CollectionFixture>
{
}
using System.Diagnostics.CodeAnalysis;

namespace KRAFT.Results.WebApi.IntegrationTests.Collections;

[CollectionDefinition(nameof(BackfillRecordsTestsCollection))]
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "xUnit collection definition")]
public sealed class BackfillRecordsTestsCollection
    : ICollectionFixture<CollectionFixture>
{
}
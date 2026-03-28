using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Records;

internal static class RecordErrors
{
    internal const string RecordNotFoundCode = "Records.NotFound";
    internal const string NotPendingCode = "Records.NotPending";

    internal static readonly Error RecordNotFound = new(
        RecordNotFoundCode,
        "Record not found.");

    internal static readonly Error NotPending = new(
        NotPendingCode,
        "Only pending records can be approved or rejected.");
}
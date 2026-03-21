using KRAFT.Results.Contracts.Records;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace KRAFT.Results.WebApi.Features.Records.Get;

internal static class GetRecordsEndpoint
{
    internal const string Name = "GetRecords";

    private static readonly string[] ValidAgeCategories =
    [
        "open", "subjunior", "junior", "masters1", "masters2", "masters3", "masters4",
    ];

    private static readonly string[] ValidEquipmentTypes = ["equipped", "classic"];

    internal static RouteGroupBuilder MapGetRecordsEndpoint(this RouteGroupBuilder endpoints)
    {
        endpoints.MapGet("/", static async Task<Results<Ok<List<RecordGroup>>, BadRequest<string>>> (
            [FromQuery] string gender,
            [FromQuery] string ageCategory,
            [FromQuery] string? equipmentType,
            [FromServices] GetRecordsHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (!Gender.TryParse(gender, out _))
            {
                return TypedResults.BadRequest("Invalid gender parameter.");
            }

            if (!ValidAgeCategories.Contains(ageCategory, StringComparer.OrdinalIgnoreCase))
            {
                return TypedResults.BadRequest("Invalid age category.");
            }

            string resolvedEquipmentType = equipmentType ?? "equipped";

            if (!ValidEquipmentTypes.Contains(resolvedEquipmentType, StringComparer.OrdinalIgnoreCase))
            {
                return TypedResults.BadRequest("Invalid equipment type.");
            }

            List<RecordGroup> result = await handler.Handle(
                gender,
                ageCategory,
                resolvedEquipmentType,
                cancellationToken);

            return TypedResults.Ok(result);
        })
        .WithName(Name)
        .WithSummary("Gets current records")
        .WithDescription("Gets current records filtered by gender, age category, and equipment type")
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}
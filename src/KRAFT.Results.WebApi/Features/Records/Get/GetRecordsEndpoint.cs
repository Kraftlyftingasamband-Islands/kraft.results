using KRAFT.Results.Contracts.Records;
using KRAFT.Results.WebApi.Features.Eras;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            [FromQuery] string? era,
            [FromServices] GetRecordsHandler handler,
            [FromServices] ResultsDbContext dbContext,
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

            string resolvedEquipmentType = equipmentType ?? "classic";

            if (!ValidEquipmentTypes.Contains(resolvedEquipmentType, StringComparer.OrdinalIgnoreCase))
            {
                return TypedResults.BadRequest("Invalid equipment type.");
            }

            int eraId;
            DateTime referenceDate;

            if (era is not null)
            {
                Era? matchedEra = await dbContext.Set<Era>()
                    .FirstOrDefaultAsync(e => e.Slug == era, cancellationToken);

                if (matchedEra is null)
                {
                    return TypedResults.BadRequest("Unknown era.");
                }

                eraId = matchedEra.EraId;
                referenceDate = matchedEra.EndDate
                    .ToDateTime(TimeOnly.MinValue);
            }
            else
            {
                Era? currentEra = await dbContext.Set<Era>()
                    .OrderByDescending(e => e.StartDate)
                    .FirstOrDefaultAsync(e => e.EndDate.Year > DateTime.UtcNow.Year, cancellationToken);

                if (currentEra is null)
                {
                    return TypedResults.Ok(new List<RecordGroup>());
                }

                eraId = currentEra.EraId;
                referenceDate = DateTime.UtcNow;
            }

            List<RecordGroup> result = await handler.Handle(
                gender,
                ageCategory,
                resolvedEquipmentType,
                eraId,
                referenceDate,
                cancellationToken);

            return TypedResults.Ok(result);
        })
        .WithName(Name)
        .WithSummary("Gets records")
        .WithDescription("Gets records filtered by gender, age category, equipment type, and optionally era")
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}
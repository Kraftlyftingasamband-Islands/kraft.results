using KRAFT.Results.WebApi.Enums;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Records.RecordTests;

public sealed class SetCurrent
{
    [Fact]
    public void SetsIsCurrentToTrue()
    {
        // Arrange
        WebApi.Features.Records.Record record = WebApi.Features.Records.Record.Create(
            eraId: 1,
            ageCategoryId: 1,
            weightCategoryId: 1,
            recordCategoryId: RecordCategory.Squat,
            weight: 200m,
            date: new DateOnly(2025, 1, 1),
            attemptId: 1,
            isRaw: true,
            createdBy: "testuser");

        // Act
        record.SetCurrent();

        // Assert
        record.IsCurrent.ShouldBeTrue();
    }
}
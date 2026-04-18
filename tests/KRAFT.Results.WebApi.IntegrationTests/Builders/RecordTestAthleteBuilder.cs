using System.Globalization;

using KRAFT.Results.Tests.Shared;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

/// <summary>
/// Seeds a test athlete with a participation and three attempts (squat, bench, deadlift)
/// in the test meet. All attempts are Good=1 by default; modify via EF after building
/// for scenarios that require bad attempts.
/// </summary>
internal sealed class RecordTestAthleteBuilder(ResultsDbContext dbContext, int baseId)
{
    private readonly int _meetId = TestSeedConstants.Meet.Id;

    private int _countryId = TestSeedConstants.Country.Id;
    private int _weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;
    private int _ageCategoryId = TestSeedConstants.AgeCategory.Masters4Id;
    private decimal _squat = 200m;
    private decimal _bench = 130m;
    private decimal _deadlift = 250m;
    private string _nameSuffix = baseId.ToString(CultureInfo.InvariantCulture);

    internal RecordTestAthleteBuilder WithCountryId(int countryId)
    {
        _countryId = countryId;
        return this;
    }

    internal RecordTestAthleteBuilder WithWeightCategoryId(int weightCategoryId)
    {
        _weightCategoryId = weightCategoryId;
        return this;
    }

    internal RecordTestAthleteBuilder WithAgeCategoryId(int ageCategoryId)
    {
        _ageCategoryId = ageCategoryId;
        return this;
    }

    internal RecordTestAthleteBuilder WithSquat(decimal squat)
    {
        _squat = squat;
        return this;
    }

    internal RecordTestAthleteBuilder WithBench(decimal bench)
    {
        _bench = bench;
        return this;
    }

    internal RecordTestAthleteBuilder WithDeadlift(decimal deadlift)
    {
        _deadlift = deadlift;
        return this;
    }

    internal RecordTestAthleteBuilder WithNameSuffix(string suffix)
    {
        _nameSuffix = suffix;
        return this;
    }

    internal async Task<SeedRecordAthlete> BuildAsync(CancellationToken cancellationToken = default)
    {
        int athleteId = baseId;
        int participationId = baseId;
        int squatAttemptId = baseId;
        int benchAttemptId = baseId + 1;
        int deadliftAttemptId = baseId + 2;
        decimal total = _squat + _bench + _deadlift;
        string slug = $"rectest-{baseId}";

        string squat = _squat.ToString(CultureInfo.InvariantCulture);
        string bench = _bench.ToString(CultureInfo.InvariantCulture);
        string deadlift = _deadlift.ToString(CultureInfo.InvariantCulture);
        string totalStr = total.ToString(CultureInfo.InvariantCulture);

        string seedSql =
            $"""
            DELETE FROM Records WHERE AttemptId IN ({squatAttemptId}, {benchAttemptId}, {deadliftAttemptId});
            DELETE FROM Attempts WHERE AttemptId IN ({squatAttemptId}, {benchAttemptId}, {deadliftAttemptId});
            DELETE FROM Participations WHERE ParticipationId = {participationId};
            DELETE FROM Athletes WHERE AthleteId = {athleteId};

            SET IDENTITY_INSERT Athletes ON;
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({athleteId}, 'RecTest', '{_nameSuffix}', '1950-01-01', 'm', {_countryId}, '{slug}');
            SET IDENTITY_INSERT Athletes OFF;

            SET IDENTITY_INSERT Participations ON;
            INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES ({participationId}, {athleteId}, {_meetId}, 90.0, {_weightCategoryId}, {_ageCategoryId}, 1, 0, {squat}, {bench}, {deadlift}, {totalStr}, 400.0, 90.0, 50);
            SET IDENTITY_INSERT Participations OFF;

            SET IDENTITY_INSERT Attempts ON;
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({squatAttemptId}, {participationId}, 1, 1, {squat}, 1, 'test', 'test');
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({benchAttemptId}, {participationId}, 2, 1, {bench}, 1, 'test', 'test');
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({deadliftAttemptId}, {participationId}, 3, 1, {deadlift}, 1, 'test', 'test');
            SET IDENTITY_INSERT Attempts OFF;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(seedSql, cancellationToken);

        return new SeedRecordAthlete(
            athleteId,
            participationId,
            squatAttemptId,
            benchAttemptId,
            deadliftAttemptId,
            _weightCategoryId);
    }
}
namespace KRAFT.Results.Tests.Shared;

public static class TestSeedConstants
{
    public static class Country
    {
        public const int Id = 1;
        public const string ISO2 = "IS";
        public const string ISO3 = "ISL";
        public const string Name = "Iceland";
    }

    public static class User
    {
        public const string Username = "testuser";
        public const string Password = "testuser";
        public const string Email = "test@email.com";
    }

    public static class MeetType
    {
        public const int Id = 1;
        public const string Title = "Powerlifting";
    }

    public static class Athlete
    {
        public const int Id = 1;
        public const string FirstName = "Testie";
        public const string LastName = "McTestFace";
        public const string Gender = "m";
        public const string Slug = "testie-mctestface";

        public static readonly DateOnly DateOfBirth = new(1985, 7, 2);
    }

    public static class Team
    {
        public const int Id = 1;
        public const string Title = "Test team";
        public const string TitleShort = "TTM";
        public const string TitleFull = "Test team";
        public const string Slug = "test-team";
    }

    public static class AgeCategory
    {
        public const int OpenId = 1;
        public const int JuniorId = 2;
    }

    public static class WeightCategory
    {
        public const int Id83Kg = 1;
        public const int Id93Kg = 2;
        public const int Id63Kg = 3;
        public const int Id74KgJunior = 4;
        public const int Id105Kg = 5;
    }

    public static class Meet
    {
        public const int Id = 1;
        public const string Title = "Test Meet 2025";
        public const string Slug = "test-meet-2025";
        public const int Year = 2025;
    }

    public static class Era
    {
        public const int HistoricalId = 1;
        public const int CurrentId = 2;
    }

    public static class Role
    {
        public const int AdminId = 1;
        public const string AdminName = "Admin";
    }
}

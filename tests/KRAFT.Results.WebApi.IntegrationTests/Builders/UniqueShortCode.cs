namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal static class UniqueShortCode
{
    private static int _counter;

    internal static string Next()
    {
        int n = Interlocked.Increment(ref _counter);
        char c0 = (char)('a' + (n % 26));
        char c1 = (char)('a' + ((n / 26) % 26));
        char c2 = (char)('a' + ((n / 676) % 26));
        return new string([c0, c1, c2]);
    }
}
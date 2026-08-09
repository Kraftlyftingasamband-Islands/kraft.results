using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests;

/// <summary>
/// Convention test that mechanically enforces DisplayFormat ownership of date format strings.
/// Any .razor or .cs file under the Web.Client or Web source trees that contains a date-format
/// pattern (e.g. "dd.MM.yyyy", "d. MMM yyyy", "yyyy-MM-dd") must route through DisplayFormat —
/// except for the two allowlisted ISO sites (AthleteForm.razor, AddParticipantForm.razor) where
/// machine-readable ISO 8601 date values are used for HTML attributes and API query parameters.
/// </summary>
public sealed partial class DateFormatConventionTests
{
    private static readonly string[] AllowlistedFileNames =
    [
        "AthleteForm.razor",
        "AddParticipantForm.razor",
    ];

    [Fact]
    public void DateFormatRegex_MatchesKnownDateFormatStrings()
    {
        // Arrange
        string[] positives =
        [
            "dd.MM.yyyy",
            "dd-MM-yyyy",
            "dd/MM/yyyy",
            "yyyy-MM-dd",
            "d. MMM yyyy",
            "MMM yyyy",
            @"ToString(""yyyy-MM-dd"")",
            @"meetDate={MeetStartDate:yyyy-MM-dd}",
        ];

        // Act + Assert
        foreach (string input in positives)
        {
            DateFormatPattern().IsMatch(input).ShouldBeTrue(
                $"Expected regex to match date format string: {input}");
        }
    }

    [Fact]
    public void DateFormatRegex_DoesNotMatchFalsePositives()
    {
        // Arrange
        string[] negatives =
        [
            @"viewBox=""0 0 24 24""",
            "M8 2a.5.5 0 0",
            "bi bi-house",
            @"stroke-width=""2""",
            @"fill-rule=""evenodd""",
            "AddAuthServices",
            "builder.Services.AddAuthServices()",
            @"stroke-linecap=""round"" stroke-linejoin=""round""",
            "5.646 5.646",
            "0 0 1 0 .708",
            @"aria-hidden=""true""",
        ];

        // Act + Assert
        foreach (string input in negatives)
        {
            DateFormatPattern().IsMatch(input).ShouldBeFalse(
                $"Expected regex NOT to match non-date string: {input}");
        }
    }

    [Fact]
    public void IsoDateRegex_MatchesIsoPattern()
    {
        // Arrange
        string[] positives =
        [
            "yyyy-MM-dd",
            @"ToString(""yyyy-MM-dd"")",
            "{MeetStartDate:yyyy-MM-dd}",
        ];

        // Act + Assert
        foreach (string input in positives)
        {
            IsoDatePattern().IsMatch(input).ShouldBeTrue(
                $"Expected ISO regex to match: {input}");
        }
    }

    [Fact]
    public void IsoDateRegex_DoesNotMatchNonIsoDateFormats()
    {
        // Arrange
        string[] negatives =
        [
            "dd.MM.yyyy",
            "d. MMM yyyy",
            "MMM yyyy",
        ];

        // Act + Assert
        foreach (string input in negatives)
        {
            IsoDatePattern().IsMatch(input).ShouldBeFalse(
                $"Expected ISO regex NOT to match non-ISO format: {input}");
        }
    }

    [Fact]
    public void SourceTree_ContainsEnoughFilesToScan()
    {
        // Arrange
        string repoRoot = ResolveRepoRoot();
        List<string> files = EnumerateSourceFiles(repoRoot);

        // Assert — guard against a mis-resolved root that would make the main test trivially pass
        files.Count.ShouldBeGreaterThan(
            0,
            "Source file enumeration returned zero files — repo root resolution may be broken");

        bool hasMeetCard = files.Exists(f => Path.GetFileName(f) == "MeetCard.razor");
        hasMeetCard.ShouldBeTrue(
            "MeetCard.razor was not found in the scanned file set — the scan roots may be wrong");
    }

    [Fact]
    public void SourceTree_ContainsNoRawDateFormatStrings_ExceptAllowlistedIsoSites()
    {
        // Arrange
        string repoRoot = ResolveRepoRoot();
        List<string> files = EnumerateSourceFiles(repoRoot);

        // Guard — ensure we scanned a meaningful set of files before asserting no violations
        files.Count.ShouldBeGreaterThan(
            0,
            "Source file enumeration returned zero files — repo root resolution may be broken");

        List<string> violations = [];

        // Act
        foreach (string filePath in files)
        {
            string fileName = Path.GetFileName(filePath);
            bool isAllowlisted = Array.Exists(
                AllowlistedFileNames,
                name => string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase));

            string[] lines = File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                MatchCollection matches = DateFormatPattern().Matches(line);

                foreach (Match match in matches)
                {
                    bool isIso = IsoDatePattern().IsMatch(match.Value);

                    // Permit only when the file is allowlisted AND the matched pattern is ISO
                    if (isAllowlisted && isIso)
                    {
                        continue;
                    }

                    string relativePath = Path.GetRelativePath(repoRoot, filePath);
                    violations.Add($"{relativePath}:{i + 1}: {match.Value}");
                }
            }
        }

        // Assert
        violations.ShouldBeEmpty(
            "Raw date format strings found outside DisplayFormat.cs. " +
            "Route all date formatting through DisplayFormat instead. Offending sites:\n" +
            string.Join("\n", violations));
    }

    // Matches a two-token date format string: <token><separator><token>
    // Tokens: dd, d, MMMM, MMM, MM, yyyy, yy  (longer forms must come first in alternation)
    // Separators: dot, hyphen, slash, space, or dot-space (for "d. MMM yyyy" style)
    // Negative lookbehind/lookahead on [a-zA-Z\d] prevents matching inside words like "AddAuth" or SVG class names.
    [GeneratedRegex(
        @"(?<![a-zA-Z\d])(?:dd|d|MMMM|MMM|MM|yyyy|yy)(?:\. |[.\-/ ])(?:dd|d|MMMM|MMM|MM|yyyy|yy)(?![a-zA-Z\d])",
        RegexOptions.None,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex DateFormatPattern();

    // An ISO pattern is year-first with hyphen separator: yyyy-MM or yyyy-MM-dd
    [GeneratedRegex(
        @"(?<![a-zA-Z\d])yyyy-(?:MM|dd)(?![a-zA-Z\d])",
        RegexOptions.None,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex IsoDatePattern();

    private static List<string> EnumerateSourceFiles(string repoRoot)
    {
        string[] scanRoots =
        [
            Path.Combine(repoRoot, "src", "KRAFT.Results.Web.Client"),
            Path.Combine(repoRoot, "src", "KRAFT.Results.Web"),
        ];

        List<string> files = [];

        foreach (string root in scanRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            IEnumerable<string> sourceFiles = Directory
                .EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Where(IsIncludedSourceFile);

            files.AddRange(sourceFiles);
        }

        return files;
    }

    private static bool IsIncludedSourceFile(string filePath)
    {
        string ext = Path.GetExtension(filePath);
        if (!string.Equals(ext, ".razor", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(ext, ".cs", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Exclude generated output directories
        char[] separators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];
        string[] segments = filePath.Split(separators);
        if (Array.Exists(
            segments,
            s => string.Equals(s, "obj", StringComparison.OrdinalIgnoreCase)
              || string.Equals(s, "bin", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // DisplayFormat.cs is the single sanctioned owner — exclude it
        if (string.Equals(
            Path.GetFileName(filePath),
            "DisplayFormat.cs",
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static string ResolveRepoRoot([CallerFilePath] string callerPath = "")
    {
        DirectoryInfo? directory = new FileInfo(callerPath).Directory;

        while (directory is not null)
        {
            if (Directory.EnumerateFiles(directory.FullName, "KRAFT.Results.slnx").Any())
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate 'KRAFT.Results.slnx' by walking up from '{callerPath}'. " +
            "Ensure the test file is within the repository.");
    }
}

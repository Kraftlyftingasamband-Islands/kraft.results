using System.Globalization;
using System.Runtime.CompilerServices;

namespace KRAFT.Results.Web.Client.Tests;

internal static class TestCulture
{
    [ModuleInitializer]
    internal static void SetIcelandicCulture()
    {
        CultureInfo icelandic = new("is-IS");
        CultureInfo.DefaultThreadCurrentCulture = icelandic;
        CultureInfo.DefaultThreadCurrentUICulture = icelandic;
        CultureInfo.CurrentCulture = icelandic;
        CultureInfo.CurrentUICulture = icelandic;
    }
}

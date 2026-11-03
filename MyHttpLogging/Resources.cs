using System.Globalization;
using System.Resources;

namespace Microsoft.AspNetCore.MyHttpLogging;

// Hack: The standalone project does not use ASP.NET Core's resource-code generator.
// Read the copied upstream resource from this assembly instead of framework-internal Resources.
internal static class Resources
{
    private static readonly ResourceManager ResourceManager = new(typeof(Resources));

    internal static string FormatUnableToFindServices(object serviceCollection, object registrationMethod)
        => string.Format(CultureInfo.CurrentCulture,
            ResourceManager.GetString("UnableToFindServices", CultureInfo.CurrentUICulture)!,
            serviceCollection, registrationMethod);
}

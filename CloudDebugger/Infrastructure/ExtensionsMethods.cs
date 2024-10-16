using System.Text.RegularExpressions;

namespace CloudDebugger.Infrastructure;

public static class ExtensionsMethods
{
    /// <summary>
    /// Will sanitize a given input to only include safe characters A-Z 0-9 [space] - . / \ :
    /// </summary>
    /// <param name="input">The input string to sanitize</param>
    /// <returns>The sanitized string</returns>
    public static string SanitizeInput(this string? input)
    {
        if (input == null)
            return string.Empty;

        // Define the regex pattern to allow only spaces, numerical, hyphen/minus and alphabetical characters, . / \ :
        string pattern = @"[^a-zA-Z0-9\s-./\\:]";
        // Replace all unwanted characters with an empty string
        string filteredMessage = Regex.Replace(input, pattern, "", RegexOptions.None, TimeSpan.FromMilliseconds(250));

        return filteredMessage;
    }
}

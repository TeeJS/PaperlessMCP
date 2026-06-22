namespace PaperlessMCP.Utils;

/// <summary>
/// Shared parsing utilities for MCP tool parameters.
/// </summary>
public static class ParsingHelpers
{
    /// <summary>
    /// Parses a comma-separated string of integers into an array.
    /// </summary>
    /// <param name="input">Comma-separated integer values (e.g., "1,2,3")</param>
    /// <returns>Array of parsed integers, or null if input is empty/whitespace</returns>
    public static int[]? ParseIntArray(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        return input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var n) ? n : (int?)null)
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .ToArray();
    }

    /// <summary>
    /// Parses a date string into a DateTime.
    /// </summary>
    /// <param name="input">Date string in any standard format</param>
    /// <returns>Parsed DateTime, or null if input is empty/invalid</returns>
    public static DateTime? ParseDate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        return DateTime.TryParse(input, out var date) ? date : null;
    }

    /// <summary>
    /// Parses a string into a positive integer, returning a fallback for
    /// missing, malformed, zero, or negative values.
    /// </summary>
    /// <param name="input">Raw string value (e.g., from an environment variable)</param>
    /// <param name="fallback">Value returned when input is not a positive integer</param>
    /// <returns>The parsed positive integer, or <paramref name="fallback"/></returns>
    public static int ParsePositiveInt(string? input, int fallback)
    {
        return int.TryParse(input, out var parsed) && parsed > 0 ? parsed : fallback;
    }
}

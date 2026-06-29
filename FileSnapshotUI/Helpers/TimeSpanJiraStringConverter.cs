using System;
using System.Text;
using System.Text.RegularExpressions;

namespace FileSnapshotUI.Helpers;

/// <summary>
/// Provides utility methods to convert between C# <see cref="TimeSpan"/> objects 
/// and Jira-compatible duration strings.
/// </summary>
public static class TimeSpanJiraStringConverter {
    /// <summary>
    /// Converts a <see cref="TimeSpan"/> into a Jira-formatted duration string.
    /// </summary>
    /// <param name="timeSpan">The <see cref="TimeSpan"/> to convert.</param>
    /// <returns>A string representation (e.g., "1w 2d 4h 30m").</returns>
    /// <remarks>
    /// The conversion uses the following mapping:
    /// <list type="bullet">
    /// <item><description>w: weeks (7 days)</description></item>
    /// <item><description>d: days</description></item>
    /// <item><description>h: hours</description></item>
    /// <item><description>m: minutes</description></item>
    /// </list>
    /// Zero-value components are excluded from the output string.
    /// </remarks>
    public static string TimeSpanToJira(TimeSpan timeSpan) {
        StringBuilder durationString = new();

        int weeks = timeSpan.Days / 7;
        int days = timeSpan.Days - (weeks * 7);
        int hours = timeSpan.Hours;
        int minutes = timeSpan.Minutes;

        if (weeks > 0) durationString.Append($"{weeks}w ");
        if (days > 0) durationString.Append($"{days}d ");
        if (hours > 0) durationString.Append($"{hours}h ");
        if (minutes > 0) durationString.Append($"{minutes}m");

        return durationString.ToString().Trim();
    }

    /// <summary>
    /// Parses a Jira-formatted duration string into a <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="jiraDuration">The duration string to parse (e.g., "1w 2h").</param>
    /// <returns>A <see cref="TimeSpan"/> representing the total parsed time.</returns>
    /// <exception cref="ArgumentException">Thrown if the input string does not match the expected Jira format.</exception>
    public static TimeSpan JiraToTimeSpan(string jiraDuration) {
        if (string.IsNullOrWhiteSpace(jiraDuration)) throw new ArgumentException("Duration cannot be empty", nameof(jiraDuration));

        // Validates if string is a Jira duration string
        var ValidationRegex = new Regex(@"^(\s*\d+\s*[wdhm]\s*)+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        // Extract tokens from Jira duration string
        var TokenRegex = new Regex(@"(?<value>\d+)\s*(?<unit>[wdhm])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        if (!ValidationRegex.IsMatch(jiraDuration)) throw new ArgumentException("Invalid Jira Duration format", nameof(jiraDuration));

        long totalMinutes = 0;
        var matches = TokenRegex.Matches(jiraDuration);

        foreach (Match match in matches) {
            int value = int.Parse(match.Groups["value"].Value);
            string unit = match.Groups["unit"].Value.ToLower();

            switch (unit) {
                case "w":
                    totalMinutes += value * 7 * 24 * 60;
                    break;
                case "d":
                    totalMinutes += value * 24 * 60;
                    break;
                case "h":
                    totalMinutes += value * 60;
                    break;
                case "m":
                    totalMinutes += value;
                    break;
            }
        }
        return TimeSpan.FromMinutes(totalMinutes);
    }
}
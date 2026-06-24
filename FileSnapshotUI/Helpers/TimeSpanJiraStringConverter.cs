using System;
using System.Text;
using System.Text.RegularExpressions;

namespace FileSnapshotUI.Helpers;

public static class TimeSpanJiraStringConverter {
    public static string TimeSpanToJira(TimeSpan timeSpan) {
        StringBuilder durationString = new();

        int weeks = timeSpan.Days/7;
        int days = timeSpan.Days - (weeks*7);
        int hours = timeSpan.Hours;
        int minutes = timeSpan.Minutes;

        if (weeks > 0) durationString.Append($"{weeks}w ");
        if (days > 0) durationString.Append($"{days}d ");
        if (hours > 0) durationString.Append($"{hours}h ");
        if (minutes > 0) durationString.Append($"{minutes}m");
        
        return durationString.ToString().Trim();
    }

    public static TimeSpan JiraToTimeSpan(string jiraDuration) {
        if (string.IsNullOrWhiteSpace(jiraDuration)) throw new ArgumentException("Duration cannot be empty", nameof(jiraDuration));

        var ValidationRegex = new Regex(@"^(\s*\d+\s*[wdhm]\s*)+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var TokenRegex = new Regex(@"(?<value>\d+)\s*(?<unit>[wdhm])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        if (!ValidationRegex.IsMatch(jiraDuration)) throw new ArgumentException("Invalid Jira Duration format", nameof(jiraDuration));

        long totalMinutes = 0;
        var matches = TokenRegex.Matches(jiraDuration);

        foreach(Match match in matches) {
            int value = int.Parse(match.Groups["value"].Value);
            string unit = match.Groups["unit"].Value.ToLower();

            switch(unit) {
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
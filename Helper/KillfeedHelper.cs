using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectM.Network;
using static Killfeed.Utils.DataStore;

namespace Killfeed.Utils;
public static class KillfeedHelper
{
    public static List<string> FormatLeaderboard(List<PlayerStatistics> topPlayers)
    {
        var sbList = new List<StringBuilder>();
        var sb = new StringBuilder();

        sb.AppendLine(AnsiColor.ColorText("Top Players Leaderboard", "yellow"));
        sb.AppendLine(AnsiColor.ColorText(" Position | Kills / Deaths | Name | Highest Level", "green"));
        sb.AppendLine(AnsiColor.ColorText(" -------------------------------------------------------", "yellow"));

        var message = (PlayerStatistics k) =>
            $"{AnsiColor.ColorText(k.Kills.ToString(), "green")} / {AnsiColor.ColorText(k.Deaths.ToString(), "red")} " +
            $"{AnsiColor.ColorText(k.LastName, "white"),-20} " +
            $"{AnsiColor.ColorText($"[{k.MaxLevel}]", "yellow")}";

        for (var i = 0; i < topPlayers.Count; i++)
        {
            var k = topPlayers[i];
            string entry = $"{i + 1}. {message(k)}\n";

            if ((sb.Length + entry.Length) > Plugin.MAX_REPLY_LENGTH)
            {
                sbList.Add(sb);
                sb = new StringBuilder();
                sb.AppendLine();
            }

            sb.Append(entry);
        }

        if (sb.Length > 0)
        {
            sbList.Add(sb);
        }

        return sbList.Select(s => s.ToString()).ToList();
    }

    public static string FormatKillfeed(PlayerStatistics stats, int rank, int totalPlayers)
    {
        var sb = new StringBuilder();
        sb.AppendLine(AnsiColor.ColorText("Killfeed Statistics", "yellow"));

        sb.AppendLine($"{AnsiColor.ColorText(" Player : ", "white")}{AnsiColor.ColorText(stats.LastName, "white")}");
        sb.AppendLine($"{AnsiColor.ColorText(" Rank   : ", "white")}{AnsiColor.ColorText($"{rank + 1}", "green")} / {AnsiColor.ColorText($"{totalPlayers}", "white")}");

        sb.AppendLine(AnsiColor.ColorText(" Kills / Deaths", "yellow"));
        sb.AppendLine($"{AnsiColor.ColorText("    Kills : ", "white")}{AnsiColor.ColorText($"{stats.Kills}", "green")}");
        sb.AppendLine($"{AnsiColor.ColorText("    Deaths : ", "white")}{AnsiColor.ColorText($"{stats.Deaths}", "red")}");

        sb.AppendLine(AnsiColor.ColorText(" Streaks", "yellow"));
        sb.AppendLine($"{AnsiColor.ColorText("    Current  : ", "white")}{AnsiColor.ColorText($"{stats.CurrentStreak}", "reset")}");
        sb.AppendLine($"{AnsiColor.ColorText("    Highest : ", "white")}{AnsiColor.ColorText($"{stats.HighestStreak}", "red")}");

        return sb.ToString();
    }
}

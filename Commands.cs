using System.Linq;
using System.Text;
using Cpp2IL.Core.Extensions;
using ProjectM;
using ProjectM.Network;
using VampireCommandFramework;

namespace Killfeed;

public class Commands
{
	[Command("leaderboard", shortHand: "kf top")]
	public void TopCommand(ChatCommandContext ctx)
	{
		// TODO: Enhance with cache and such
		int num = 10;
		int offset = 5;
		var topKillers = DataStore.PlayerDatas.Values.OrderByDescending(k => k.Kills).Take(num).ToArray();

		offset = offset > topKillers.Length ? topKillers.Length : offset;
		num = num > topKillers.Length ? topKillers.Length : num;



		var pad = (string name) => new string('\t'.Repeat(6 - name.Length / 5).ToArray());


		var sb = new StringBuilder();
		var sb2 = new StringBuilder();

		sb2.AppendLine("");
		sb.AppendLine($"{Markup.Prefix} <size=18><u>Top Kills</u></size>");

		//var message = (DataStore.PlayerStatistics k) => $"{Markup.Highlight(k.LastName)}{pad(k.LastName)}<color={Markup.SecondaryColor}><b>{k.Kills}</b> / {k.Deaths}</color>";
		var message = (DataStore.PlayerStatistics k) => $"\t<color={Markup.SecondaryColor}><b>{k.Kills,-3}</b> / {k.Deaths,3}</color>\t{Markup.Highlight(k.LastName)}";

		for (var i = 0; i < offset; i++)
		{
			var k = topKillers[i];
			sb.AppendLine($"{i + 1}. {message(k)}");
			//sb.AppendLine($"{i + 1}. {Markup.Highlight(k.LastName.PadRight(1, ' '))}{pad(k.LastName)}{Markup.Secondary(k.LastName.Length)}");
		}



		for (var i = offset; i < num; i++)
		{
			var k = topKillers[i];
			sb2.AppendLine($"{i + 1}. {message(k)}");
			//sb2.AppendLine($"{i + 1}. {Markup.Highlight(k.LastName.PadRight(1, ' '))}{pad(k.LastName)}{Markup.Secondary(k.LastName.Length)}");
		}
		ctx.Reply(sb.ToString());
		ctx.Reply(sb2.ToString());
	}

	[Command("killfeed", shortHand: "kf", description: "Shows Killfeed info")]
	public void KillfeedCommand(ChatCommandContext ctx)
	{

		var steamId = ctx.User.PlatformId;

		// append current rank based on kills
		if (!DataStore.PlayerDatas.TryGetValue(steamId, out _))
		{
			throw ctx.Error($"You have no stats yet!");
		}

		var (stats, rank) = DataStore.PlayerDatas.Values.OrderByDescending(k => k.Kills)
																.Select((stats, rank) => (stats, rank))
																.First(u => u.stats.SteamId == ctx.User.PlatformId);

		var sb = new StringBuilder();
		sb.AppendLine($"{Markup.Prefix} <size=21><u>Killfeed Stats for {Markup.Highlight(stats.LastName)}</u>");

		var rankStr = $"{Markup.Highlight($"{(rank + 1)}")} / {Markup.Secondary(DataStore.PlayerDatas.Count)}";
		sb.AppendLine($"Rank: {rankStr}</size>");

		sb.AppendLine($"Kills: {Markup.Highlight(stats.Kills)}");
		sb.AppendLine($"Deaths: {Markup.Highlight(stats.Deaths)}");
		sb.AppendLine($"Current Streak: {Markup.Highlight(stats.CurrentStreak)}");
		sb.AppendLine($"Highest Streak: {Markup.Highlight(stats.HighestStreak)}");


		ctx.Reply(sb.ToString());
	}
}

using System.Linq;
using System.Text;
using Cpp2IL.Core.Extensions;
using Killfeed.Utils;
using ProjectM;
using ProjectM.Network;
using VampireCommandFramework;

namespace Killfeed.Commands;

public class Commands
{
	[Command("leaderboard", shortHand: "lb")]
	public void TopCommand(ChatCommandContext ctx)
	{
		int num = 10;
		var topKillers = DataStore.PlayerDatas.Values.OrderByDescending(k => k.Kills).Take(num).ToList();
		var leaderboardMessage = KillfeedHelper.FormatLeaderboard(topKillers);

		foreach (var leaderReply in leaderboardMessage)
		{
			ctx.Reply(leaderReply);
		}
	}

	[Command("killfeed", shortHand: "kf", description: "Shows Killfeed info")]
	public void KillfeedCommand(ChatCommandContext ctx)
	{

		var steamId = ctx.User.PlatformId;

		if (!DataStore.PlayerDatas.TryGetValue(steamId, out _))
		{
			throw ctx.Error($"You have no stats yet!");
		}

		var (stats, rank) = DataStore.PlayerDatas.Values.OrderByDescending(k => k.Kills)
																.Select((stats, rank) => (stats, rank))
																.First(u => u.stats.SteamId == ctx.User.PlatformId);

		var killfeedMessage = KillfeedHelper.FormatKillfeed(stats, rank, DataStore.PlayerDatas.Count);
		ctx.Reply(killfeedMessage);
	}
}

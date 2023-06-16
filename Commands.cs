using System.Linq;
using System.Text;
using VampireCommandFramework;

namespace LeaderBored;

[CommandGroup("leaderboard", "lb")]
public class Commands
{

	//[Command("test-save")]
	//public static void GenerateCmd(ICommandContext ctx)
	//{
	//	DataStore.WriteToDisk();
	//}

	//[Command("test-generate")]
	//public static void GenerateCmd(ICommandContext ctx, int count)
	//{
	//	DataStore.GenerateNTestData(count);
	//}

	[Command("top")]
	public void TopCommand(ChatCommandContext ctx)
	{
		// TODO: Enhance with cache and such
		var topKillers = DataStore.PlayerDatas.Values.OrderByDescending(k => k.Kills).Take(10).ToArray();

		var sb = new StringBuilder();
		sb.AppendLine($"{Markup.Prefix} <u>Top Kills</u>");
		for (var i = 0; i < topKillers.Count(); i++)
		{
			var k = topKillers[i];
			sb.AppendLine($"{i+1}. {Markup.Highlight(k.LastName)} {k.Kills} {k.Deaths} {k.CurrentStreak}");
		}

		ctx.Reply(sb.ToString());
	}
}
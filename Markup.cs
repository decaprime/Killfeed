using VampireCommandFramework;

namespace LeaderBored;

internal static class Markup
{
	public static string Highlight(string s) => s.Underline().Color("#def");

	public static string Prefix = $"[lb]".Color("#ed1").Bold();

}

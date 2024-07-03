using BepInEx.Configuration;

namespace Killfeed;

internal class Settings
{
	internal static bool AnnounceKills { get; private set; }
	internal static int AnnounceKillstreakLostMinimum { get; private set; }
	internal static bool AnnounceKillstreak { get; private set; }
	internal static bool IncludeLevel { get; private set; }
	internal static bool UseMaxLevel { get; private set; }
	internal static int LevelDifferenceThreshold { get; private set; }
	internal static int LevelColorThreshold1 { get; private set; }
	internal static int LevelColorThreshold2 { get; private set; }
	internal static int LevelColorThreshold3 { get; private set; }

	internal static void Initialize(ConfigFile config)
	{
		AnnounceKills = config.Bind("General", "AnnounceKills", true, "Announce kills in chat").Value;
		AnnounceKillstreakLostMinimum = config.Bind("General", "AnnounceKillstreakLostMinimum", 3, "Minimum killstreak count that must be lost to be announced.").Value;
		AnnounceKillstreak = config.Bind("General", "AnnounceKillstreak", true, "Announce killstreaks in chat").Value;

		IncludeLevel = config.Bind("General", "IncludeLevel", true, "Include player gear levels in announcements.").Value;
		UseMaxLevel = config.Bind("General", "UseMaxLevel", false, "Use max gear level instead of current gear level.").Value;

		LevelDifferenceThreshold = config.Bind("General", "LevelDifferenceThreshold", 20, "Threshold for level difference to consider a valide kill").Value;

		LevelColorThreshold1 = config.Bind("General", "LevelColorThreshold1", 10, "First level difference threshold (yellow)").Value;
		LevelColorThreshold2 = config.Bind("General", "LevelColorThreshold2", 20, "Second level difference threshold (red = grief-kill)").Value;
	}
}

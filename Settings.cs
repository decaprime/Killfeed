

using BepInEx.Configuration;
using System;

namespace LeaderBored;

internal class Settings
{
	internal static bool AnnounceKills { get; private set; }

	internal static void Initialize(ConfigFile config)
	{
		AnnounceKills = config.Bind("General", "AnnounceKills", true, "Announce kills in chat").Value;
	}
}

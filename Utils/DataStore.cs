using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Backtrace.Unity.Common;
using Bloodstone.API;
using Il2CppSystem.Linq;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Jobs;
using Random = System.Random;

namespace Killfeed.Utils;
public class DataStore
{
	public record struct PlayerStatistics(ulong SteamId, string LastName, int Kills, int Deaths, int CurrentStreak,
	int HighestStreak, string LastClanName, int CurrentLevel, int MaxLevel, Entity UserEntity, Entity CharEntity)
	{
		private static string SafeCSVName(string s) => s.Replace(",", "");

		public string ToCsv() => $"{SteamId},{SafeCSVName(LastName)},{Kills},{Deaths},{CurrentStreak},{HighestStreak},{SafeCSVName(LastClanName)},{CurrentLevel},{MaxLevel}";

		public static PlayerStatistics Parse(string csv)
		{
			var split = csv.Split(',');
			return new PlayerStatistics()
			{
				SteamId = ulong.Parse(split[0]),
				LastName = split[1],
				Kills = int.Parse(split[2]),
				Deaths = int.Parse(split[3]),
				CurrentStreak = int.Parse(split[4]),
				HighestStreak = int.Parse(split[5]),
				LastClanName = split.Length > 6 ? split[6] : "",
				CurrentLevel = split.Length > 7 ? int.Parse(split[7]) : -1,
				MaxLevel = split.Length > 8 ? int.Parse(split[8]) : -1,
				UserEntity = Entity.Null, // Default value, update this in your logic where necessary
				CharEntity = Entity.Null // Default value, update this in your logic where necessary
			};
		}

		public string FormattedName
		{
			get
			{
				var name = Markup.Highlight(LastName);
				if (Settings.IncludeLevel)
				{
					return Settings.UseMaxLevel ? $"{name} ({Markup.Secondary(MaxLevel)}*)" : $"{name} ({Markup.Secondary(CurrentLevel)})";
				}
				else
				{
					return $"{name}";
				}
			}
		}
	}

	public record struct EventData(ulong VictimId, ulong KillerId, float3 Location, long Timestamp, int VictimLevel, int KillerLevel)
	{
		public string ToCsv() => $"{VictimId},{KillerId},{Location.x},{Location.y},{Location.z},{Timestamp},{VictimLevel},{KillerLevel}";

		public static EventData Parse(string csv)
		{
			var split = csv.Split(',');
			return new EventData()
			{
				VictimId = ulong.Parse(split[0]),
				KillerId = ulong.Parse(split[1]),
				Location = new float3(float.Parse(split[2]), float.Parse(split[3]), float.Parse(split[4])),
				Timestamp = long.Parse(split[5]),
				VictimLevel = split.Length > 6 ? int.Parse(split[6]) : 0,
				KillerLevel = split.Length > 7 ? int.Parse(split[7]) : 0,
			};
		}
	}

	public static List<EventData> Events = new();
	public static Dictionary<ulong, PlayerStatistics> PlayerDatas = new();

	private static readonly Random _rand = new();
	private static EventData GenerateTestEvent() => new()
	{
		VictimId = (ulong)_rand.NextInt64(),
		KillerId = (ulong)_rand.NextInt64(),
		Location = new float3((float)_rand.NextDouble(), (float)_rand.NextDouble(), (float)_rand.NextDouble()),
		Timestamp = DateTime.UtcNow.AddMinutes(_rand.Next(-10000, 10000)).Ticks
	};

	public static void GenerateNTestData(int count)
	{
		for (var i = 0; i < count; i++)
		{
			Events.Add(GenerateTestEvent());
		}
	}

	private const string EVENTS_FILE_NAME = "events.v1.csv";
	private const string EVENTS_FILE_PATH = $"BepInEx/config/Killfeed/{EVENTS_FILE_NAME}";

	private const string STATS_FILE_NAME = "stats.v1.csv";
	private const string STATS_FILE_PATH = $"BepInEx/config/Killfeed/{STATS_FILE_NAME}";

	public static void WriteToDisk()
	{
		var dir = Path.GetDirectoryName(EVENTS_FILE_PATH);
		if (!Directory.Exists(dir))
		{
			Directory.CreateDirectory(dir);
		}

		// TODO: Ideally this appends events and is smarter
		using StreamWriter eventsFile = new StreamWriter(EVENTS_FILE_PATH, append: false);
		foreach (var eventData in Events)
		{
			eventsFile.WriteLine(eventData.ToCsv());
		}

		using StreamWriter statsFile = new StreamWriter(STATS_FILE_PATH, append: false);
		foreach (var playerData in PlayerDatas.Values)
		{
			statsFile.WriteLine(playerData.ToCsv());
		}
	}

	public static void LoadFromDisk()
	{
		// can't think of how they would not be newed but let's be sure we don't duplicate data
		Events.Clear();
		PlayerDatas.Clear();

		// let's assume maybe it can be empty or not exist and we don't care
		LoadEventData();
		LoadPlayerData();
	}

	private static void LoadEventData()
	{
		if (!File.Exists(EVENTS_FILE_PATH))
		{
			return;
		}
		using StreamReader eventsFile = new StreamReader(EVENTS_FILE_PATH);
		while (!eventsFile.EndOfStream)
		{
			var line = eventsFile.ReadLine();
			if (string.IsNullOrWhiteSpace(line)) continue;
			try
			{
				Events.Add(EventData.Parse(line));
			}
			catch (Exception)
			{
				Plugin.Logger.LogError($"Failed to parse event line: \"{line}\"");
			}
		}
	}

	private static void LoadPlayerData()
	{
		if (!File.Exists(STATS_FILE_PATH)) return;
		using StreamReader statsFile = new StreamReader(STATS_FILE_PATH);
		while (!statsFile.EndOfStream)
		{
			var line = statsFile.ReadLine();
			if (string.IsNullOrWhiteSpace(line)) continue;
			try
			{
				var playerData = PlayerStatistics.Parse(line);
				if (PlayerDatas.TryGetValue(playerData.SteamId, out PlayerStatistics data))
				{
					Plugin.Logger.LogWarning($"Duplicate player data found, overwriting {data} with {playerData}");
				}
				PlayerDatas[playerData.SteamId] = playerData;
			}
			catch (Exception)
			{
				Plugin.Logger.LogError($"Failed to parse player line: \"{line}\"");
			}
		}
	}

	public static void RegisterKillEvent(PlayerCharacter victim, Entity victimEntity, PlayerCharacter killer, Entity killerEntity, float3 location, int victimLevel, int killerLevel)
	{
		var victimUser = victim.UserEntity.Read<User>();
		var killerUser = killer.UserEntity.Read<User>();

		Plugin.Logger.LogWarning($"{victimLevel} {killerLevel}");

		var newEvent = new EventData(victimUser.PlatformId, killerUser.PlatformId, location, DateTime.UtcNow.Ticks, victimLevel, killerLevel);

		Events.Add(newEvent);

		PlayerStatistics UpsertName(ulong steamId, string name, string clanName, int level, Entity userEntity, Entity charEntity)
		{
			if (PlayerDatas.TryGetValue(steamId, out var player))
			{
				player.LastName = name;
				player.LastClanName = clanName;
				player.CurrentLevel = level;
				player.MaxLevel = Math.Max(player.MaxLevel, level);
				player.UserEntity = userEntity;
				player.CharEntity = charEntity;
				PlayerDatas[steamId] = player;
			}
			else
			{
				PlayerDatas[steamId] = new PlayerStatistics(steamId, name, 0, 0, 0, 0, clanName, level, level, userEntity, charEntity);
			}

			return PlayerDatas[steamId];
		}

		var victimData = UpsertName(victimUser.PlatformId, victimUser.CharacterName.ToString(), victim.SmartClanName.ToString(), victimLevel, victim.UserEntity, victimEntity);
		var killerData = UpsertName(killerUser.PlatformId, killerUser.CharacterName.ToString(), killer.SmartClanName.ToString(), killerLevel, killer.UserEntity, killerEntity);

		int victimEffectiveLevel = victimData.MaxLevel > 0 ? victimData.MaxLevel : victimLevel;
		int killerEffectiveLevel = killerData.MaxLevel > 0 ? killerData.MaxLevel : killerLevel;

		int levelDifference = killerEffectiveLevel - victimEffectiveLevel;
		if (levelDifference < Settings.LevelDifferenceThreshold)
		{
			RecordKill(killerUser.PlatformId);
			var lostStreak = RecordDeath(victimUser.PlatformId);
			AnnounceKill(victimData, killerData, lostStreak);
			UpdateTopKillersBuff();
		}
		else
		{
			// Apply debuff for significant level difference
			ApplyDebuffForUnfairKill(killer.UserEntity, killerEntity);
			AnnounceKill(victimData, killerData, 0);
		}

		WriteToDisk();
	}

	private static void AnnounceKill(PlayerStatistics victimUser, PlayerStatistics killerUser, int lostStreakAmount)
	{
		if (!Settings.AnnounceKills) return;

		string victimClan = victimUser.LastClanName != "No clan found" ? victimUser.LastClanName : "";
		string killerClan = killerUser.LastClanName != "No clan found" ? killerUser.LastClanName : "";

		string victimLevelColor = "green";
		string killerLevelColor = "green";

		int levelDifference = killerUser.MaxLevel - victimUser.MaxLevel;
		if (levelDifference >= Settings.LevelColorThreshold1 && levelDifference <= Settings.LevelColorThreshold2)
		{
			victimLevelColor = "yellow";
			killerLevelColor = "yellow";
		}
		else if (levelDifference > Settings.LevelColorThreshold2)
		{
			victimLevelColor = "red";
			killerLevelColor = "red";
		}

		var victimName = AnsiColor.ColorText(victimClan, "reset") + AnsiColor.ColorText(victimUser.LastName, "white") + $"[" + AnsiColor.ColorText($"{victimUser.MaxLevel}", victimLevelColor) + "]";
		var killerName = AnsiColor.ColorText(killerClan, "reset") + AnsiColor.ColorText(killerUser.LastName, "white") + $"[" + AnsiColor.ColorText($"{killerUser.MaxLevel}", killerLevelColor) + "]";

		string killedText = AnsiColor.ColorText("killed", killerLevelColor);

		var message = lostStreakAmount > Settings.AnnounceKillstreakLostMinimum
			? $"{killerName} ended {victimName}'s {Markup.Secondary(lostStreakAmount)} kill streak!"
			: $"{killerName} {killedText} {victimName}!";

		var killMsg = killerUser.CurrentStreak switch
		{
			5 => $"<size=18>{killerName} is on a killing spree!",
			10 => $"<size=19>{killerName} is on a rampage!",
			15 => $"<size=20>{killerName} is dominating!",
			20 => $"<size=21>{killerName} is unstoppable!",
			25 => $"<size=22>{killerName} is godlike!",
			30 => $"<size=24>{killerName} is WICKED SICK!",
			_ => null
		};

		ServerChatUtils.SendSystemMessageToAllClients(VWorld.Server.EntityManager, Markup.Prefix + message);

		if (!string.IsNullOrEmpty(killMsg) && Settings.AnnounceKillstreak)
		{
			ServerChatUtils.SendSystemMessageToAllClients(VWorld.Server.EntityManager, Markup.Prefix + killMsg);
		}
	}

	private static void UpdateTopKillersBuff()
	{
		var topKillers = PlayerDatas.Values.OrderByDescending(k => k.Kills).Take(10).ToList();
		var top3Killers = topKillers.Take(3).ToList();

		var buffPrefabs = new List<PrefabGUID>
		{
			new PrefabGUID(1533067119), // Buff for 1st place
            new PrefabGUID(-1864993435), // Buff for 2nd place
            new PrefabGUID(1670636401)   // Buff for 3rd place
        };

		foreach (var player in PlayerDatas.Values)
		{
			var userEntity = player.UserEntity;
			var charEntity = player.CharEntity;

			for (int i = 0; i < buffPrefabs.Count; i++)
			{
				var buffPrefab = buffPrefabs[i];
				if (i < top3Killers.Count && top3Killers[i] == player)
				{
					if (!BuffUtility.HasBuff(VWorld.Server.EntityManager, charEntity, buffPrefab))
					{
						Buffs.AddBuff(userEntity, charEntity, buffPrefab, 30);
					}
				}
				else
				{
					if (BuffUtility.HasBuff(VWorld.Server.EntityManager, charEntity, buffPrefab))
					{
						Buffs.RemoveBuff(charEntity, buffPrefab);
					}
				}
			}
		}
	}

	private static void ApplyDebuffForUnfairKill(Entity killerUser, Entity killerEntity)
	{
		// Define your debuff prefab GUID
		var debuffPrefab = new PrefabGUID(1163490655); // Replace with your debuff GUID
													   // Apply the debuff
		Buffs.AddBuff(killerUser, killerEntity, debuffPrefab, 30); // Adjust duration as needed
	}

	private static int RecordDeath(ulong platformId)
	{
		var lostStreak = 0;
		if (PlayerDatas.TryGetValue(platformId, out var player))
		{
			player.Deaths++;

			lostStreak = player.CurrentStreak;
			player.CurrentStreak = 0;

			PlayerDatas[platformId] = player;
		}
		else
		{
			PlayerDatas[platformId] = new PlayerStatistics(platformId, "", 0, 1, 0, 0, "", 0, 0, Entity.Null, Entity.Null);
		}

		return lostStreak;
	}

	private static void RecordKill(ulong steamId)
	{
		if (PlayerDatas.TryGetValue(steamId, out var player))
		{
			player.Kills++;
			player.CurrentStreak++;
			player.HighestStreak = math.max(player.HighestStreak, player.CurrentStreak);
			PlayerDatas[steamId] = player;
		}
		else
		{
			PlayerDatas[steamId] = new PlayerStatistics(steamId, "", 1, 0, 1, 1, "", 0, 0, Entity.Null, Entity.Null);
		}
	}
}

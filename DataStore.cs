using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Backtrace.Unity.Common;
using Bloodstone.API;
using Il2CppSystem.Linq;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Mathematics;
using Random = System.Random;

namespace LeaderBored;
public class DataStore
{
	public record struct PlayerStatistics(ulong SteamId, string LastName, uint Kills, int Deaths, int CurrentStreak,
		int HighestStreak)
	{
		// lol yikes
		public string ToCsv() => $"{SteamId},{(LastName?.Contains(',') ?? true ? "invalid" : LastName)},{Kills},{Deaths},{CurrentStreak},{HighestStreak}";

		public static PlayerStatistics Parse(string csv)
		{
			// intentionally naieve and going to blow up so I'll catch it and not get an object for that player and log it
			var split = csv.Split(',');
			return new PlayerStatistics()
			{
				SteamId = ulong.Parse(split[0]),
				LastName = split[1],
				Kills = uint.Parse(split[2]),
				Deaths = int.Parse(split[3]),
				CurrentStreak = int.Parse(split[4]),
				HighestStreak = int.Parse(split[5])
			};
		}
	}

	public record struct EventData(ulong VictimId, ulong KillerId, float3 Location, long Timestamp)
	{
		public string ToCsv() => $"{VictimId},{KillerId},{Location.x},{Location.y},{Location.z},{Timestamp}";

		public static EventData Parse(string csv)
		{
			var split = csv.Split(',');
			return new EventData()
			{
				VictimId = ulong.Parse(split[0]),
				KillerId = ulong.Parse(split[1]),
				Location = new float3(float.Parse(split[2]), float.Parse(split[3]), float.Parse(split[4])),
				Timestamp = long.Parse(split[5])
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
	private const string EVENTS_FILE_PATH = $"BepInEx/config/Leaderbored/{EVENTS_FILE_NAME}";

	private const string STATS_FILE_NAME = "stats.v1.csv";
	private const string STATS_FILE_PATH = $"BepInEx/config/Leaderbored/{STATS_FILE_NAME}";

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
				if(PlayerDatas.ContainsKey(playerData.SteamId))
				{
					Plugin.Logger.LogWarning($"Duplicate player data found, overwriting {PlayerDatas[playerData.SteamId]} with {playerData}");
				}
				PlayerDatas[playerData.SteamId] = playerData;
			}
			catch (Exception)
			{
				Plugin.Logger.LogError($"Failed to parse player line: \"{line}\"");
			}
		}
	}

    public static void RegisterKillEvent(PlayerCharacter victim, PlayerCharacter killer, float3 location)
	{
		var victimUser = victim.UserEntity.Read<User>();
		var killerUser = killer.UserEntity.Read<User>();

		var newEvent = new EventData(victimUser.PlatformId, killerUser.PlatformId, location, DateTime.UtcNow.Ticks);

		Events.Add(newEvent);


		var UpsertName = (ulong steamId, string name) =>
		{
			if (PlayerDatas.TryGetValue(steamId, out var player))
			{
				player.LastName = name;
				PlayerDatas[steamId] = player;
			}
			else
			{
				PlayerDatas[steamId] = new PlayerStatistics() { LastName = name, SteamId = steamId };
			}
		};

		UpsertName(victimUser.PlatformId, victimUser.CharacterName.ToString());
		UpsertName(killerUser.PlatformId, killerUser.CharacterName.ToString());

		RecordKill(killerUser.PlatformId);
		var lostStreak = RecordDeath(victimUser.PlatformId);

		AnnounceKill(victimUser, killerUser, lostStreak);

		// TODO: Very bad, but going to save to disk each kill for nice hiccup of lag
		// while this is naieve and whole file, in append or WAL this might be better
		WriteToDisk();
	}

	private static void AnnounceKill(User victimUser, User killerUser, bool lostStreak)
	{
		if (!Settings.AnnounceKills) return;
		
		var victimName = Markup.Highlight(PlayerDatas[victimUser.PlatformId].LastName);
		var killerName = Markup.Highlight(PlayerDatas[killerUser.PlatformId].LastName);

		var message = lostStreak
			? $"{killerName} ended {victimName}'s kill streak!"
			: $"{killerName} killed {victimName}!";

		ServerChatUtils.SendSystemMessageToAllClients(VWorld.Server.EntityManager, Markup.Prefix + message);
	}

	private static bool RecordDeath(ulong platformId)
	{
		var lostStreak = false;
		if (PlayerDatas.TryGetValue(platformId, out var player))
		{
			player.Deaths++;
			if (player.CurrentStreak > 0)
			{
				player.CurrentStreak = 0;
				lostStreak = true;
			}
			PlayerDatas[platformId] = player;
		}
		else
		{
			PlayerDatas[platformId] = new PlayerStatistics() { Deaths = 1, SteamId = platformId };
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
			PlayerDatas[steamId] = new PlayerStatistics() { Kills = 1, CurrentStreak = 1, HighestStreak = 1, SteamId = steamId };
		}
	}
}
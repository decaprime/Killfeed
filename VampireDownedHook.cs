using System;
using Bloodstone.API;
using HarmonyLib;
using ProjectM;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Killfeed;

[HarmonyPatch(typeof(VampireDownedServerEventSystem), nameof(VampireDownedServerEventSystem.OnUpdate))]
public static class VampireDownedHook
{
	public static void Prefix(VampireDownedServerEventSystem __instance)
	{
		var downedEvents = __instance.__query_1174204813_0.ToEntityArray(Allocator.Temp);
		foreach (var entity in downedEvents)
		{
			ProcessVampireDowned(entity);
		}
	}

	private static void ProcessVampireDowned(Entity entity)
	{

		if (!VampireDownedServerEventSystem.TryFindRootOwner(entity, 1, VWorld.Server.EntityManager, out var victimEntity))
		{
			Plugin.Logger.LogMessage("Couldn't get victim entity");
			return;
		}

		var downBuff = entity.Read<VampireDownedBuff>();


		if (!VampireDownedServerEventSystem.TryFindRootOwner(downBuff.Source, 1, VWorld.Server.EntityManager, out var killerEntity))
		{
			Plugin.Logger.LogMessage("Couldn't get victim entity");
			return;
		}

		var victim = victimEntity.Read<PlayerCharacter>();

		Plugin.Logger.LogMessage($"{victim.Name} is victim");
		var unitKiller = killerEntity.Has<UnitLevel>();

		if (unitKiller)
		{
			Plugin.Logger.LogInfo($"{victim.Name} was killed by a unit. [Not currently tracked]");
			return;
		}

		var playerKiller = killerEntity.Has<PlayerCharacter>();

		if (!playerKiller)
		{
			Plugin.Logger.LogWarning($"Killer could not be identified for {victim.Name}, if you know how to reproduce this please contact deca on discord or report on github");
			return;
		}

		var killer = killerEntity.Read<PlayerCharacter>();

		if (killer.UserEntity == victim.UserEntity)
		{
			Plugin.Logger.LogInfo($"{victim.Name} killed themselves. [Not currently tracked]");
			return;
		}

		var location = victimEntity.Read<LocalToWorld>();

		int victimLevel = victimEntity.Has<Equipment>(out var victimEquipment) ? (int)Math.Round(victimEquipment.GetFullLevel()) : -1;
		int killerLevel = killerEntity.Has<Equipment>(out var killerEquipment) ? (int)Math.Round(killerEquipment.GetFullLevel()) : -1;

		DataStore.RegisterKillEvent(victim, killer, location.Position, victimLevel, killerLevel);
	}
}

using ProjectM.Network;
using ProjectM;
using Unity.Entities;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using Bloodstone.API;

namespace Killfeed;
internal class Buffs
{
    public delegate void BuffCreated(Entity buffEntity);
    public static bool AddBuff(Entity User, Entity Character, PrefabGUID buffPrefab, float duration = 0, bool immortal = true)
    {
        var des = VWorld.Server.GetExistingSystemManaged<DebugEventsSystem>();
        var buffEvent = new ApplyBuffDebugEvent()
        {
            BuffPrefabGUID = buffPrefab
        };

        var fromCharacter = new FromCharacter()
        {
            User = User,
            Character = Character
        };
        if (!BuffUtility.TryGetBuff(VWorld.Server.EntityManager, Character, buffPrefab, out Entity buffEntity))
        {
            des.ApplyBuff(fromCharacter, buffEvent);
            if (BuffUtility.TryGetBuff(VWorld.Server.EntityManager, Character, buffPrefab, out buffEntity))
            {
                if (buffEntity.Has<CreateGameplayEventsOnSpawn>())
                {
                    buffEntity.Remove<CreateGameplayEventsOnSpawn>();
                }
                if (buffEntity.Has<GameplayEventListeners>())
                {
                    buffEntity.Remove<GameplayEventListeners>();
                }

                if (immortal)
                {
                    buffEntity.Add<Buff_Persists_Through_Death>();
                    if (buffEntity.Has<RemoveBuffOnGameplayEvent>())
                    {
                        buffEntity.Remove<RemoveBuffOnGameplayEvent>();
                    }

                    if (buffEntity.Has<RemoveBuffOnGameplayEventEntry>())
                    {
                        buffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
                    }
                }
                if (duration > -1 && duration != 0)
                {
                    if (!buffEntity.Has<LifeTime>())
                    {
                        buffEntity.Add<LifeTime>();
                        buffEntity.Write(new LifeTime
                        {
                            EndAction = LifeTimeEndAction.Destroy
                        });
                    }

                    var lifetime = buffEntity.Read<LifeTime>();
                    lifetime.Duration = duration;
                    buffEntity.Write(lifetime);
                }
                else if (duration == -1)
                {
                    if (buffEntity.Has<LifeTime>())
                    {
                        var lifetime = buffEntity.Read<LifeTime>();
                        lifetime.Duration = -1;
                        lifetime.EndAction = LifeTimeEndAction.None;
                        buffEntity.Write(lifetime);
                    }
                    if (buffEntity.Has<RemoveBuffOnGameplayEvent>())
                    {
                        buffEntity.Remove<RemoveBuffOnGameplayEvent>();
                    }
                    if (buffEntity.Has<RemoveBuffOnGameplayEventEntry>())
                    {
                        buffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    public static void RemoveBuff(Entity Character, PrefabGUID buffPrefab)
    {
        if (BuffUtility.TryGetBuff(VWorld.Server.EntityManager, Character, buffPrefab, out var buffEntity))
        {
            DestroyUtility.Destroy(VWorld.Server.EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
        }
    }
}

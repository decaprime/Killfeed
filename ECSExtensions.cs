
using Bloodstone.API;
using Il2CppInterop.Runtime;
using Unity.Entities;

internal static class ECSExtensions
{
	public static void With<T>(this Entity entity, VExtensions.ActionRef<T> action) where T : struct
	{
		T item = entity.RW<T>();
		action(ref item);
		VWorld.Game.EntityManager.SetComponentData(entity, item);
	}

	public static bool Has<T>(this Entity entity)
	{
		int typeIndex = TypeManager.GetTypeIndex(Il2CppType.Of<T>());
		
		return VWorld.Game.EntityManager.HasComponentRaw(entity, typeIndex);
	}

	public unsafe static T RW<T>(this Entity entity)
	{
		int typeIndex = TypeManager.GetTypeIndex(Il2CppType.Of<T>());
		T* componentDataRawRW = (T*)VWorld.Game.EntityManager.GetComponentDataRawRW(entity, typeIndex);
		return *componentDataRawRW;
	}

	public unsafe static T Read<T>(this Entity entity)
	{
		int typeIndex = TypeManager.GetTypeIndex(Il2CppType.Of<T>());
		T* componentDataRawRO = (T*)VWorld.Game.EntityManager.GetComponentDataRawRO(entity, typeIndex);
		return *componentDataRawRO;
	}

	public unsafe static DynamicBuffer<T> AddBufferAOT<T>(this Entity entity) where T : struct
	{
		int typeIndex = TypeManager.GetTypeIndex(Il2CppType.Of<T>());

		entity.AddComponentAOT<T>();

		return *(DynamicBuffer<T>*)VWorld.Game.EntityManager.GetBufferRawRW(entity, typeIndex);
	}

	public unsafe static void AddComponentAOT<T>(this Entity entity) where T : struct
	{
		VWorld.Game.EntityManager.AddComponent(entity, Il2CppType.Of<T>());
	}
	public unsafe static void AddComponentDataAOT<T>(this Entity entity, T component) where T : struct
	{
		VWorld.Game.EntityManager.AddComponent(entity, Il2CppType.Of<T>());
		
		VWorld.Game.EntityManager.SetComponentData(entity, component);
	}
}

using System;
using Unity.Entities;

static class VWorld
{
	private static World? _serverWorld;


	/// Extracted from bloodstone to make reintegrating bloodstone later easier if desired.

	public static World Server
	{
		get
		{
			if (_serverWorld != null && _serverWorld.IsCreated)
				return _serverWorld;

			_serverWorld = GetWorld("Server")
				?? throw new System.Exception("There is no Server world (yet). Did you install a server mod on the client?");
			return _serverWorld;
		}
	}
	private static World? GetWorld(string name)
	{
		foreach (var world in World.s_AllWorlds)
		{
			if (world.Name == name)
			{
				_serverWorld = world;
				return world;
			}
		}

		return null;
	}
}


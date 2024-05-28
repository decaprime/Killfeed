using System.Diagnostics;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Steamworks;
using VampireCommandFramework;

namespace Killfeed;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("gg.deca.VampireCommandFramework")]
[BepInDependency("gg.deca.Bloodstone")]
[Bloodstone.API.Reloadable]
public partial class Plugin : BasePlugin
{
	Harmony _harmony;
	public static ManualLogSource Logger;
	public override void Load()
	{
		Logger = Log;
		// Plugin startup logic
		Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} version {MyPluginInfo.PLUGIN_VERSION} is loaded!");

		// Harmony patching
		_harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
		_harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());

		// Register all commands in the assembly with VCF
		CommandRegistry.RegisterAll();
		Settings.Initialize(Config);

		DataStore.LoadFromDisk();
	}

	public override bool Unload()
	{
		CommandRegistry.UnregisterAssembly();
		_harmony?.UnpatchSelf();
		return true;
	}
}

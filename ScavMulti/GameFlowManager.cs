using System;
using System.Collections;
using HarmonyLib;

namespace ScavMulti;

[HarmonyPatch]
public static class GameFlowManager
{	
	[HarmonyPrefix]
	[HarmonyPatch(typeof(global::PreRunScript), nameof(global::PreRunScript.StartRun))]
	static void PreRunScript_StartRun_Prefix()
	{
		OnRunStart?.Invoke(runStartType: RunStartType.NewRun);
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(global::PreRunScript), nameof(global::PreRunScript.LoadRun))]
	static void PreRunScript_LoadRun_Prefix()
	{
		OnRunStart?.Invoke(runStartType: RunStartType.Continue);
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(global::PlayerCamera), nameof(global::PlayerCamera.ToMainMenu))]
	static void PlayerCamera_ToMainMenu_Prefix()
	{
		OnRunLeave?.Invoke();
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(global::WorldGeneration), nameof(global::WorldGeneration.InstantiateWorld))]
	static void WorldGeneration_InstantiateWorld_Prefix()
	{
		IsWorldGenerating = true;
		OnWorldGenStart?.Invoke();
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::WorldGeneration), nameof(global::WorldGeneration.FinishWorldGeneration))]
	static void WorldGeneration_FinishWorldGeneration_Postfix()
	{
		IsWorldGenerating = false;
		OnWorldGenEnd?.Invoke();
	}

	public static IEnumerator StartRun(RunStartType runStartType)
	{
		if (!MainMenuManager.MenuInstance)
			throw new InvalidOperationException("StartRun called when not in main menu");
		OnRunStart?.Invoke(runStartType);
		return MainMenuManager.MenuInstance.WaitLoad();
	}

	public delegate void OnRunStartDelegate(RunStartType runStartType);

	public static event OnRunStartDelegate OnRunStart;
	public static event Action OnRunLeave;
	public static event Action OnWorldGenStart;
	public static event Action OnWorldGenEnd;

	public static bool IsWorldGenerating { get; private set; } = false;
}

public enum RunStartType
{
	NewRun,
	Continue,
	Joining
}

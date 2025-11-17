using UnityEngine;
using HarmonyLib;
using ScavMulti.Network.Messages;

namespace ScavMulti;

[HarmonyPatch]
public static class WorldLogic
{
	static bool _ignoreNextEvent = false;
	static bool _IgnoreNextEvent
	{
		get
		{
			bool value = _ignoreNextEvent;
			_ignoreNextEvent = false;
			return value;
		}
	}

	public static void IgnoreNextEvent()
	{
		_ignoreNextEvent = true;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(global::WorldGeneration), nameof(global::WorldGeneration.DamageBlock), [typeof(Vector2Int), typeof(float), typeof(bool), typeof(bool)])]
	static bool WorldGeneration_DamageBlock_Postfix(float dmg)
	{
		// this is a small patch whose goal is to prevent the DamageBlock
		// routine from executing if `dmg` is zero
		// because a BlockDamage is still created if dmg == 0 which we dont want
		// since we call Body::Attack on purpose with a damage of 0
		// on a OtherExperiment to show an attack animation but not apply its damage
		return dmg != 0;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::WorldGeneration), nameof(global::WorldGeneration.DamageBlock), [typeof(Vector2Int), typeof(float), typeof(bool), typeof(bool)])]
	static void WorldGeneration_DamageBlock_Postfix(Vector2Int pos, float dmg, bool bonusMetal)
	{
		if (NetMode.OnlineAndPlaying && !_IgnoreNextEvent)
		{
			MainExperiment.Instance.Events.Add(new BlockDamageEvent(pos, dmg, bonusMetal));
		}
	}

	private static Random.State _worldGenState;
	private static Random.State _worldGenSeed;
	public static Random.State WorldGenSeed
	{
		get => _worldGenSeed;
		internal set
		{
			_worldGenSeed = value;
			_worldGenState = value;
		}
	}

	static float WorldGen_RandomValueGetter()
		=> RandomClone.Value(ref _worldGenState);
	static int WorldGen_RandomRangeInt(int min, int max)
		=> RandomClone.Range(min, max, ref _worldGenState);
	static float WorldGen_RandomRangeFloat(float min, float max)
		=> RandomClone.Range(min, max, ref _worldGenState);
	static Vector2 WorldGen_RandomInsideUnitCircle()
		=> RandomClone.InsideUnitCircle(ref _worldGenState);

	public static void PatchWorldGenerationRandom(Harmony harmony)
	{
		var baseMethod = AccessTools.Method(typeof(global::WorldGeneration), nameof(global::WorldGeneration.GenerateWorld));

		RecursiveRandomReplacer.RecursivelyPatchMethod(harmony, baseMethod, new(
			AccessTools.Method(typeof(WorldLogic), nameof(WorldGen_RandomValueGetter)),
			AccessTools.Method(typeof(WorldLogic), nameof(WorldGen_RandomRangeFloat)),
			AccessTools.Method(typeof(WorldLogic), nameof(WorldGen_RandomRangeInt)),
			AccessTools.Method(typeof(WorldLogic), nameof(WorldGen_RandomInsideUnitCircle))
		));
	}
}

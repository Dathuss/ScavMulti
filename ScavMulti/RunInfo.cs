

using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ScavMulti;

[HarmonyPatch]
public static class RunInfo
{
	private static Dictionary<Vector2Int, ushort> _modifiedBlocks;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::WorldGeneration), nameof(global::WorldGeneration.SetBlock))]
	static void WorldGeneration_SetBlock_Postfix(Vector2Int pos, ushort block)
	{
		if (!GameFlowManager.IsWorldGenerating)
			_modifiedBlocks[pos] = block;
	}

	private static void OnWorldGenStart()
	{
		_modifiedBlocks = new();
	}

	public static void Init()
	{
		GameFlowManager.OnWorldGenStart += OnWorldGenStart;
	} 

	public static IReadOnlyDictionary<Vector2Int, ushort> ModifiedBlocks => _modifiedBlocks;
}

using System;
using System.Collections.Generic;
using HarmonyLib;
using ScavMulti.Network.Messages;
using UnityEngine;

namespace ScavMulti;

[HarmonyPatch]
/// <summary>
/// represents the Expie that is YOU, the player on this computer
/// </summary>
public class MainExperiment : ExperimentInfo
{
	public static MainExperiment Instance { get; private set; }
	internal static void SetMainExperiment(GameObject obj)
	{
		if (Instance)
			throw new InvalidOperationException("SetMainExperiment called when it was already set");
		Instance = obj.AddComponent<MainExperiment>();
	}

	public List<UpdateEventBase> Events = new();

	void LateUpdate()
	{
		if (MessageDispatcher.IsAvailable)
		{
			MessageDispatcher.DispatchMessage(new ExpieUpdate(
				Body.transform.position,
				RigidBody.velocity,
				Body.moveDir,
				Body.crouching,
				Body.crouchAmount,
				(Vector2)Body.targetLookPos,
				Events
			));
		}
		Events.Clear();
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(global::Body), nameof(global::Body.Attack))]
	static void Body_Attack_Prefix(global::Body __instance)
	{
		if (MessageDispatcher.IsAvailable && __instance == MainExperiment.Instance.Body)
		{
			if (__instance.conscious && __instance.attackCooldown <= 0f)
				MainExperiment.Instance.Events.Add(new AttackEvent());
		}
	}
}

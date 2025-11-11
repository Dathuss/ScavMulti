using System;
using System.Collections.Generic;
using ScavMulti.Network.Messages;
using UnityEngine;

namespace ScavMulti;

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
		if (NetMode.Online)
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

	protected override void OnAttack(bool isAllowed, global::AttackInfo attackInfo)
	{
		if (NetMode.Online && isAllowed)
		{
			MainExperiment.Instance.Events.Add(new AttackEvent());
		}
	}
}

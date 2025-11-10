using System;
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

	void Update()
	{
		if (MessageDispatcher.IsAvailable)
		{
			MessageDispatcher.DispatchMessage(new ExpieUpdate(Body.transform.position, RigidBody.velocity));
		}
	}
}

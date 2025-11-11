using System;
using UnityEngine;
using HarmonyLib;
using ScavMulti.Network.Messages;

namespace ScavMulti;

[HarmonyPatch]
/// <summary>
/// represents any Expie that is NOT you, the player on this computer
/// </summary>
public class OtherExperiment : ExperimentInfo
{
	protected override void Start()
	{
		base.Start();
		RigidBody.isKinematic = true;
		RigidBody.velocity = Vector3.zero;
	}

	public void HandleUpdate(ExpieUpdate expieUpdate)
	{
		Body.transform.position = expieUpdate.Position;
		RigidBody.velocity = expieUpdate.Velocity;
		Body.moveDir = expieUpdate.MoveDir;
		Body.crouching = expieUpdate.Crouching;
		Body.crouchAmount = expieUpdate.CrouchAmount;
		Body.targetLookPos = expieUpdate.TargetLookPos;

		foreach (var evnt in expieUpdate.Events)
		{
			switch (evnt)
			{
				case AttackEvent:
					Body.UseItemInHand();
					break;
				default:
					Logger.LogError($"Unknown or unimplemented event received on client {Id}: {evnt.GetType()}");
					break;
			}
		}
	}

	protected override void OnAttack(bool isAllowed, global::AttackInfo attackInfo)
	{
		if (NetMode.Online)
		{
			// only show the animation and "disable" the damage done
			attackInfo.damage = 0;
			attackInfo.structuralDamage = 0;
			attackInfo.metalMoreDamage = false;
		}
	}

	public static OtherExperiment CreateInstance(int id, Vector3 position)
	{
		if (!_baseBody)
		{
			throw new NullReferenceException("_baseBody is null ! This is not normal !");
		}
		var clone = GameObject.Instantiate(_baseBody, position, Quaternion.identity);
		clone.name = $"Experiment {id}";
		clone.SetActive(true);
		var expie = clone.AddComponent<OtherExperiment>();
		expie.Id = id;
		return expie;
	}
}

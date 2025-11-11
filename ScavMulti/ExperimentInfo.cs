using System;
using UnityEngine;
using HarmonyLib;

namespace ScavMulti;

[HarmonyPatch]
public abstract class ExperimentInfo : MonoBehaviour
{
	public int Id { get; protected set; } = int.MinValue;
	public GameObject BodyObject;
	public GameObject HeadObject;
	public global::Body Body;
	public Rigidbody2D RigidBody { get; private set; }
	public BoxCollider2D MainCollider { get; private set; }

	protected virtual void Start()
	{
		Body = GetComponentInChildren<global::Body>();
		if (!Body)
			Logger.LogError("Body Script was not found !!");
		else
		{
			BodyObject = Body.gameObject;
			RigidBody = BodyObject.GetComponent<Rigidbody2D>();
			if (!RigidBody)
				Logger.LogError("RigidBody2D not found on Body Object");
			MainCollider = BodyObject.GetComponent<BoxCollider2D>();
			if (!MainCollider)
				Logger.LogError("BoxCollider2D not found on Body Object");
			else
			{
				var mask = LayerMask.NameToLayer("Body");
				if (mask < 0)
					Logger.LogError("'Body' collision mask does not exist");
				else
					MainCollider.excludeLayers |= 1 << mask;
			}
			var headTransform = Body.transform.Find("Head");
			if (!headTransform)
				Logger.LogError("Head Object not found as Body Child");
			else
				HeadObject = headTransform.gameObject;
		}
	}

	// this is a "clean" expie gameobject from which all new expies will come from
	// (omg just like in the game !!!!1!!)
	protected static GameObject _baseBody { get; private set; }

	public static void SetupHooks()
	{
		GameFlowManager.OnWorldGenStart += () =>
		{
			var expieRoot = GameObject.Find("Experiment");
			if (!expieRoot)
			{
				Logger.LogError("Expie base body was not found !! Things will go bonkers");
			}
			else
			{
				_baseBody = GameObject.Instantiate(expieRoot);
				_baseBody.name = "BaseExpie";
				_baseBody.SetActive(false);
				MainExperiment.SetMainExperiment(expieRoot);
			}
		};
	}

	static ExperimentInfo GetExpieFrom(Body instance)
	{
		if (MainExperiment.Instance.Body == instance)
			return MainExperiment.Instance;
		return Experiments.FromBody(instance);
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(global::Body), nameof(global::Body.Attack))]
	static void Body_Attack_Prefix(global::Body __instance, global::AttackInfo atk)
	{
		var expie = GetExpieFrom(__instance);
		if (expie != null)
		{
			bool isAllowed = __instance.conscious && __instance.attackCooldown <= 0f;
			expie.OnAttack(isAllowed, atk);
		}
	}

	protected virtual void OnAttack(bool isAllowed, global::AttackInfo attackInfo) { }
}

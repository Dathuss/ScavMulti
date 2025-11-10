using System;
using UnityEngine;

namespace ScavMulti;

public abstract class ExperimentInfo : MonoBehaviour
{
	public int Id { get; protected set; } = int.MinValue;
	public GameObject BodyObject;
	public global::Body Body;
	public Rigidbody2D RigidBody { get; private set; }

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
}

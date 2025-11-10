using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace ScavMulti;

[BepInPlugin(PluginInfo.PluginGUID, PluginInfo.PluginName, PluginInfo.PluginVersion)]
public class Plugin : BaseUnityPlugin
{
	public static Plugin Instance { get; private set; }
	private Harmony _harmony = null;

	void Awake()
    {
		Instance = this;
		try
		{
			ScavMulti.Logger.Init(Logger);
			_harmony = new Harmony(PluginInfo.PluginGUID);
			_harmony.PatchAll(Assembly.GetExecutingAssembly());
            MessagePack.MessagePackSerializer.DefaultOptions = Constants.MessagePackSerializerOptions;
			Utils.ProperExceptionLogger.Init();
			AssetResolver.Init(typeof(Sprite));

			RunInfo.Init();
			ClientManager.CreateInstance();
			ServerManager.CreateInstance();
			MessageHandler.CreateInstance();
			ExperimentInfo.SetupHooks();
		}
		catch (Exception e)
		{
			Logger.LogError($"Exception thrown on initialization of {PluginInfo.PluginGUID}:\n{e}");
			throw;
		}
		Logger.LogInfo($"Plugin {PluginInfo.PluginGUID} loaded succesfully");
	}

	// static void OtherBodyUpdatePrefix()
	// {
		// var body = _otherExperiment.Body;
		// body.moveDir.x = (Input.GetKey(KeyCode.L) ? 1f : 0f) - (Input.GetKey(KeyCode.J) ? 1f : 0f);
		// body.moveDir.y = (Input.GetKey(KeyCode.I) ? 1f : 0f) - (Input.GetKey(KeyCode.K) ? 1f : 0f);
		// if (Input.GetKeyDown(KeyCode.M))
		// {
		// 	body.Jump();
		// 	body.endedJump = false;
		// }
		// else if (Input.GetKeyUp(KeyCode.M))
		// 	body.endedJump = true;
		
		// body.crouching = (!body.reversedControls && Input.GetKey(KeyCode.K)) || (body.reversedControls && Input.GetKey(KeyCode.I));
	// }

	void OnDestroy()
	{
		_harmony?.UnpatchSelf();
	}
}

using UnityEngine;
using ScavMulti.Network.Messages;

namespace ScavMulti;

public class MessageHandler : MonoBehaviour
{
	public void HandleMessage(MessageBase message)
	{
		switch (message)
		{
			case ExpieUpdate expieUpdate:
				Experiments.FromId(message.SourceId).HandleUpdate(expieUpdate);
				break;
			default:
				Logger.LogError($"Unknown or unimplemented message received: {message.GetType()}");
				break;
		}
	}

	public static GameObject CreateInstance()
	{
		var obj = new GameObject("ScavMulti_MessageHandler");
		GameObject.DontDestroyOnLoad(obj);
		Instance = obj.AddComponent<MessageHandler>();
		return obj;
	}

	public static MessageHandler Instance { get; private set; }
}

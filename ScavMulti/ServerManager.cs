using System.Net;
using System.Collections.Generic;
using UnityEngine;
using ScavMulti.Network;
using ScavMulti.Network.Messages;

namespace ScavMulti;

public class ServerManager : MonoBehaviour
{
	private bool _willServerRun = false;
	private bool _isRunning = false;
	private Server _server { get; set; }

	void Awake()
	{
		GameFlowManager.OnRunStart += OnRunStart;
		GameFlowManager.OnWorldGenEnd += OnWorldGenEnd;
		GameFlowManager.OnRunLeave += () =>
		{
			if (_isRunning)
			{
				_server.Dispose();
				_server = null;
				_isRunning = false;
				_willServerRun = false;
				MessageDispatcher.ResetEndpoint();
				NetMode.SetMode(NetMode.ModeClass.Offline);
			}
		};
	}

	void OnRunStart(RunStartType runStartType)
	{
		if (runStartType == RunStartType.NewRun || runStartType == RunStartType.Continue)
		{
			NetMode.SetMode(NetMode.ModeClass.IAmTheServer);
			_willServerRun = true;
		}
	}

	void OnWorldGenEnd()
	{
		if (_willServerRun)
		{
			Logger.LogInfo("World gen finished, instantiating socket");
			var ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000); // TODO: change this
			_server = new(ep);
			_server.Run();
			MessageDispatcher.SetEndpoint(_server);
			GameFlowManager.IsPlaying = true;
			_willServerRun = false;
			_isRunning = true;
		}
	}

	static readonly List<ItemCreateEvent> _itemInitialSendBuffer = [];

	bool HandleServerMessage(MessageBase message, Client client)
	{
		switch (message)
		{
			case IAmReady:
				if (!client.IsFullyRunning)
				{
					var mainBodyPos = transform.position;
					MessageDispatcher.ForwardMessage(new ClientConnected(client.Id, mainBodyPos), client.Id);
					Experiments.AddExperiment(client.Id, mainBodyPos);
					client.SetIsFullyRunning();
				}
				return true;
			case WorldStateRequest:
				ItemManager.MakeItemCreateEvents(_itemInitialSendBuffer);
				client.Enqueue(new WorldState(
					MainExperiment.Instance.transform.position,
					RunInfo.ModifiedBlocks,
					BuildingEntityManager.DamagedEntities,
					_itemInitialSendBuffer
				));
				_itemInitialSendBuffer.Clear();
				return true;
		}
		return false;
	}

	void LateUpdate()
	{
		if (_isRunning)
		{
			foreach (var deadClient in _server.RemoveDeadClients())
			{
				MessageDispatcher.DispatchMessage(new ClientDisconnected(deadClient.Id));
				Experiments.RemoveExperiment(deadClient.Id);
				Logger.LogWarning($"Client is leaving. Exception: {deadClient.ClientCancelledException}");
			}

			foreach (var client in _server)
			{
				while (client.IsRunning && !client.IsEmpty)
				{
					var message = client.Dequeue();
					message.SourceId = client.Id;
					if (!HandleServerMessage(message, client))
						MessageHandler.Instance.HandleMessage(message);
				}
			}

			while (_server.NextPendingClient(out var pendingClient))
			{
				Logger.LogInfo("Client pending !!");

				_server.AcceptClient(pendingClient);

				var mainBodyPos = global::PlayerCamera.main.body.transform.position;
				pendingClient.Enqueue(new PeerHandshake(pendingClient.Id));
				pendingClient.Enqueue(new WorldInfo(
					WorldGeneration.world.chunkWidth,
					WorldGeneration.world.chunkHeight,
					(uint)WorldGeneration.CHUNKSIZE,
					WorldLogic.WorldGenSeed,
					WorldGeneration.world.biomeDepth
				));
			}
		}
	}

	public static GameObject CreateInstance()
	{
		var obj = new GameObject("ScavMulti_ServerManager");
		GameObject.DontDestroyOnLoad(obj);
		Instance = obj.AddComponent<ServerManager>();
		return obj;
	}

	public static ServerManager Instance { get; private set; }
}

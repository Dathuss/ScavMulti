using System.Net;
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
			NetMode.SetMode(NetMode.ModeClass.IAmTheServer);
			_willServerRun = false;
			_isRunning = true;
		}
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
					mainBodyPos,
					WorldLogic.WorldGenSeed,
					WorldGeneration.world.biomeDepth,
					RunInfo.ModifiedBlocks,
					BuildingEntityManager.DamagedEntities
				));
				MessageDispatcher.ForwardMessage(new ClientConnected(pendingClient.Id, mainBodyPos), pendingClient.Id);
				Experiments.AddExperiment(pendingClient.Id, mainBodyPos);
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

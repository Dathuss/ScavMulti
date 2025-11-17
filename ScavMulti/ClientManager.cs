using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using ScavMulti.Network;
using ScavMulti.Network.Messages;

namespace ScavMulti;

public class ClientManager : MonoBehaviour
{
	Client _endpoint;
	WorldInfo _worldInfo;
	bool _isTryingToConnect;
	bool _isJoiningWorld = false;
	bool _isRunning = false;

	void Awake()
	{
		MainMenuManager.OnConnectClicked += (ipAddress) =>
		{
			if (!_isTryingToConnect)
			{
				StartCoroutine(Utils.TryCoroutine(TryConnectToServer(ipAddress),
					onError: (e) =>
					{
						MainMenuManager.SetConnectErrorText(e.Message);
					})
				);
			}
		};
		GameFlowManager.OnWorldGenStart += OnWorldGenStart;
		GameFlowManager.OnWorldGenEnd += OnWorldGenEnd;
		GameFlowManager.OnRunLeave += () =>
		{
			if (_isRunning)
			{
				_isRunning = false;
				_isJoiningWorld = false;
				_isTryingToConnect = false;
				_endpoint.Dispose();
				_endpoint = null;
				_worldInfo = null;
				MessageDispatcher.ResetEndpoint();
				NetMode.SetMode(NetMode.ModeClass.Offline);
			}
		};
	}

	IEnumerator TryConnectToServer(string ipAddress)
	{
		_isTryingToConnect = true;
		try
		{
			var split = ipAddress.Split(':');
			if (split.Length != 2)
				throw new FormatException("Port not specified");
			uint parsed = uint.Parse(split[1]);
			var addr = IPAddress.Parse(split[0]);
			var ep = new IPEndPoint(addr, (int)parsed);
			var client = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
			client.Connect(ep);
			_endpoint = new Client(client);
			_endpoint.Start();
			Logger.LogInfo("Connection accepted, receiving handshake");
			yield return _endpoint.WaitUntilHasData();
			int myId = _endpoint.Dequeue<PeerHandshake>().YourId;
			_endpoint.Id = myId;
			Logger.LogInfo("Received handshake");
			yield return _endpoint.WaitUntilHasData();
			_worldInfo = _endpoint.Dequeue<WorldInfo>();
			_isJoiningWorld = true;
			yield return GameFlowManager.StartRun(RunStartType.Joining);
		}
		finally
		{
			_isTryingToConnect = false;
		}
	}

	void OnWorldGenStart()
	{
		if (_isJoiningWorld)
		{
			WorldGeneration.world.chunkWidth = _worldInfo.NumChunksX;
			WorldGeneration.world.chunkHeight = _worldInfo.NumChunksY;
			WorldGeneration.world.biomeDepth = _worldInfo.BiomeDepth;
			WorldLogic.WorldGenSeed = _worldInfo.WorldGenSeed;
			NetMode.SetMode(NetMode.ModeClass.IAmTheClient);
			MessageDispatcher.SetEndpoint(_endpoint);
		}
	}

	void OnWorldGenEnd()
	{
		IEnumerator WorldGenEndCoroutine()
		{
			_endpoint.Enqueue(new WorldStateRequest());
			yield return _endpoint.WaitUntilHasData();
			var state = _endpoint.Dequeue<WorldState>();
			foreach (var kv in state.ModifiedBlocks)
			{
				WorldGeneration.world.SetBlock(kv.Key, kv.Value);
			}
			var idToEntityMap = BuildingEntityManager.IdToEntityMap;
			foreach (var kv in state.DamagedEntities)
			{
				if (idToEntityMap.TryGetValue(kv.Key, out BuildingEntityManager e) && e)
					e.UpdateHealth(kv.Value);
				else
					Logger.LogWarning($"Entity with id {kv.Key} not found when fixing entities");
			}

			ItemManager.DestroyAllItemsLocally();

			foreach (var itemCreateEvent in state.Items)
			{
				ItemManager.CreateItem(itemCreateEvent);
			}

			_endpoint.Enqueue(new IAmReady());
			_isJoiningWorld = false;
			_isRunning = true;

			Experiments.AddExperiment(-1, state.CurrentExperimentPos);
			GameFlowManager.IsPlaying = true;
		}
		
		if (_isJoiningWorld)
		{
			Logger.LogInfo("World gen finished, fixing world");
			StartCoroutine(WorldGenEndCoroutine());
		}
	}

	void LateUpdate()
	{
		if (_isRunning)
		{
			while (_endpoint.IsRunning && !_endpoint.IsEmpty)
			{
				var message = _endpoint.Dequeue();
				if (message is ClientConnected clientConnected)
					Experiments.AddExperiment(clientConnected.Id, clientConnected.Position);
				else if (message is ClientDisconnected clientDisconnected)
					Experiments.RemoveExperiment(clientDisconnected.Id);
				else
					MessageHandler.Instance.HandleMessage(message);
			}
		}
	}

	public static GameObject CreateInstance()
	{
		var obj = new GameObject("ScavMulti_ClientManager");
		GameObject.DontDestroyOnLoad(obj);
		Instance = obj.AddComponent<ClientManager>();
		return obj;
	}

	public static ClientManager Instance { get; private set; }
}

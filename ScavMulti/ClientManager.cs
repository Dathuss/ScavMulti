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
	bool _isJoiningWorld = false;
	bool _isRunning = false;

	void Awake()
	{
		MainMenuManager.OnConnectClicked += (ipAddress) =>
		{
			StartCoroutine(Utils.TryCoroutine(TryConnectToServer(ipAddress),
				onError: (e) =>
				{
					MainMenuManager.SetConnectErrorText(e.Message);
				})
			);
		};
		GameFlowManager.OnWorldGenStart += OnWorldGenStart;
		GameFlowManager.OnWorldGenEnd += OnWorldGenEnd;
	}

	IEnumerator TryConnectToServer(string ipAddress)
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
		_endpoint.Dequeue<PeerHandshake>();
		Logger.LogInfo("Received handshake");
		yield return _endpoint.WaitUntilHasData();
		_worldInfo = _endpoint.Dequeue<WorldInfo>();
		_isJoiningWorld = true;
		yield return GameFlowManager.StartRun(RunStartType.Joining);
	}

	void OnWorldGenStart()
	{
		if (_isJoiningWorld)
		{
			WorldGeneration.world.chunkWidth = _worldInfo.NumChunksX;
			WorldGeneration.world.chunkHeight = _worldInfo.NumChunksY;
			WorldGeneration.world.biomeDepth = _worldInfo.BiomeDepth;
			UnityEngine.Random.state = _worldInfo.WorldGenSeed;
		}
	}

	void OnWorldGenEnd()
	{
		IEnumerator FixWorldCoroutine()
		{
			// we have to wait one frame before fixing the world because
			// some entities may not have been initialized yet
			yield return new WaitForEndOfFrame();
			foreach (var kv in _worldInfo.ModifiedBlocks)
			{
				WorldGeneration.world.SetBlock(kv.Key, kv.Value);
			}
			var reverseEntityIdentifierMap = RunInfo.ReverseEntityIdentifierMap;
			foreach (var id in _worldInfo.DestroyedEntities)
			{
				if (reverseEntityIdentifierMap.TryGetValue(id, out BuildingEntity e) && e)
					Object.Destroy(e.gameObject);
			}
		}
		
		if (_isJoiningWorld)
		{
			Logger.LogInfo("World gen finished, fixing world");
			StartCoroutine(FixWorldCoroutine());
			MessageDispatcher.SetEndpoint(_endpoint);
			_isJoiningWorld = false;
			_isRunning = true;
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

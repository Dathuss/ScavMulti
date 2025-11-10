using System;
using ScavMulti.Network;
using ScavMulti.Network.Messages;

namespace ScavMulti;

public static class MessageDispatcher
{
	private static Server _serverInstance;
	private static Client _clientInstance;
	public static bool IsAvailable { get; private set; }

	public static void SetEndpoint(Server serverInstance)
	{
		_serverInstance = serverInstance;
		_clientInstance = null;
		IsAvailable = true;
	}

	public static void SetEndpoint(Client clientInstance)
	{
		_clientInstance = clientInstance;
		_serverInstance = null;
		IsAvailable = true;
	}

	public static void ResetEndpoint()
	{
		_serverInstance = null;
		_clientInstance = null;
		IsAvailable = false;
	}

	public static void DispatchMessage(MessageBase message)
	{
		if (_serverInstance != null)
		{
			message.SourceId = -1; // server id
			_serverInstance.SendToAllClients(message);
		}
		else if (_clientInstance != null)
		{
			// The server should only rely on the endpoint information to
			// know which client has sent which message,
			// therefore we don't even bother setting an id
			_clientInstance.Enqueue(message);
		}
		else
			throw new InvalidOperationException("DispatchMessage called with no available endpoint");
	}

	public static void ForwardMessage(MessageBase message, int clientId)
	{
		if (_serverInstance != null)
		{
			message.SourceId = clientId;
			_serverInstance.SendToAllClientsExcept(message, clientId);
		}
		else
			throw new InvalidOperationException("ForwardMessage called when I am not the server");
	}
}

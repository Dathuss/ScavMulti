using System;
using ScavMulti.Network;
using ScavMulti.Network.Messages;

namespace ScavMulti;

public static class MessageDispatcher
{
	private static Server _serverInstance;
	private static Client _clientInstance;

	public static void SetEndpoint(Server serverInstance)
	{
		_serverInstance = serverInstance;
		_clientInstance = null;
	}

	public static void SetEndpoint(Client clientInstance)
	{
		_clientInstance = clientInstance;
		_serverInstance = null;
	}

	public static void ResetEndpoint()
	{
		_serverInstance = null;
		_clientInstance = null;
	}

	public static void DispatchMessage(MessageBase message, bool requiresFullyRunningClient = true)
	{
		if (_serverInstance != null)
		{
			message.SourceId = -1; // server id
			_serverInstance.SendToAllClients(message, requiresFullyRunningClient);
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

	public static void ForwardMessage(MessageBase message, int clientId, bool requiresFullyRunningClient = true)
	{
		if (_serverInstance != null)
		{
			message.SourceId = clientId;
			_serverInstance.SendToAllClientsExcept(message, clientId, requiresFullyRunningClient);
		}
		else
			throw new InvalidOperationException("ForwardMessage called when I am not the server");
	}
}

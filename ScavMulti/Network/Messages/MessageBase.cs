using MessagePack;

namespace ScavMulti.Network.Messages;

[Union(0, typeof(Error))]
[Union(1, typeof(PeerHandshake))]
[Union(2, typeof(WorldInfo))]
[Union(3, typeof(WorldStateRequest))]
[Union(4, typeof(WorldState))]
[Union(5, typeof(IAmReady))]
[Union(6, typeof(ClientConnected))]
[Union(7, typeof(ClientDisconnected))]
[Union(8, typeof(ExpieUpdate))]
public abstract record class MessageBase
{
	[Key(1000)]
	public int SourceId { get; set; }
}

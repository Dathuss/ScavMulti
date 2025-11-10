using MessagePack;

namespace ScavMulti.Network.Messages;

[Union(0, typeof(Error))]
[Union(1, typeof(PeerHandshake))]
[Union(2, typeof(WorldInfo))]
[Union(3, typeof(ClientConnected))]
[Union(4, typeof(ClientDisconnected))]
[Union(5, typeof(ExpieUpdate))]
public abstract record class MessageBase
{
	[Key(1000)]
	public int SourceId { get; set; }
}

using MessagePack;

namespace ScavMulti.Network.Messages;

[Union(0, typeof(AttackEvent))]
public abstract record class UpdateEventBase;

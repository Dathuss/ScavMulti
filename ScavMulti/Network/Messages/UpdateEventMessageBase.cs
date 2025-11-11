using MessagePack;

namespace ScavMulti.Network.Messages;

[Union(0, typeof(AttackEvent))]
[Union(1, typeof(BlockDamageEvent))]
public abstract record class UpdateEventBase;

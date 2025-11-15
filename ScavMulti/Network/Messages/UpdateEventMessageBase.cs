using MessagePack;

namespace ScavMulti.Network.Messages;

[Union(0, typeof(AttackEvent))]
[Union(1, typeof(BlockDamageEvent))]
[Union(2, typeof(EntityHealthSyncEvent))]
public abstract record class UpdateEventBase;

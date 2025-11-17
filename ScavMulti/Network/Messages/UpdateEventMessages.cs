using System.Collections.Generic;
using MessagePack;
using UnityEngine;

namespace ScavMulti.Network.Messages;

[MessagePackObject]
public record class AttackEvent : UpdateEventBase;

[MessagePackObject]
public record class BlockDamageEvent(
	[property: Key(0)] Vector2Int Pos,
	[property: Key(1)] float Damage,
	[property: Key(2)] bool BonusMetal
) : UpdateEventBase;

[MessagePackObject]
public record class EntityHealthSyncEvent(
	[property: Key(0)] int EntityId,
	[property: Key(1)] float NewHealth
) : UpdateEventBase;

[MessagePackObject]
public record class ItemCreateEvent(
	[property: Key(0)] string Type,
	[property: Key(1)] float FreshDropTime,
	[property: Key(2)] ItemUpdateEvent InitialProps
) : UpdateEventBase;

[MessagePackObject]
public record class ItemUpdateEvent(
	[property: Key(0)] int Id,
	[property: Key(1)] Vector2 Position,
	[property: Key(2)] float Rotation,
	[property: Key(3)] Vector2 Velocity,
	[property: Key(4)] float AngularVelocity,
	[property: Key(5)] float Condition
) : UpdateEventBase;

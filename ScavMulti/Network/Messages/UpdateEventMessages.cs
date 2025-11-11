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


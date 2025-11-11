using System.Collections.Generic;
using MessagePack;
using UnityEngine;

namespace ScavMulti.Network.Messages;

[MessagePackObject]
public record class AttackEvent : UpdateEventBase;


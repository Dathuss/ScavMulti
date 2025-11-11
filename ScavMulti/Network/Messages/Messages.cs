using System.Collections.Generic;
using MessagePack;
using UnityEngine;

namespace ScavMulti.Network.Messages;

[MessagePackObject]
public record class Error(
	[property: Key(0)] bool IsFatal,
	[property: Key(1)] string Message
) : MessageBase;

[MessagePackObject]
public record class PeerHandshake(
	// the id the server gave to the client receiving this message
	[property: Key(0)] int YourId
) : MessageBase;

/// <summary>
/// sent by the server after the handshake
/// </summary>
[MessagePackObject]
public record class WorldInfo(
	[property: Key(0)] uint NumChunksX,
	[property: Key(1)] uint NumChunksY,
	[property: Key(2)] uint ChunkSize,
	[property: Key(3)] Vector3 CurrentExperimentPos,
	[property: Key(4)] UnityEngine.Random.State WorldGenSeed,
	[property: Key(5)] int BiomeDepth,
	[property: Key(6)] IReadOnlyDictionary<Vector2Int, ushort> ModifiedBlocks,
	[property: Key(7)] IEnumerable<int> DestroyedEntities

) : MessageBase;

/// <summary>
/// sent by the server to notify a new client has joined
/// </summary>
[MessagePackObject]
public record class ClientConnected(
	[property: Key(0)] int Id,
	[property: Key(1)] Vector3 Position
) : MessageBase;

[MessagePackObject]
public record class ClientDisconnected(
	[property: Key(0)] int Id
) : MessageBase;

[MessagePackObject]
public record class ExpieUpdate(
	[property: Key(0)] Vector2 Position,
	[property: Key(1)] Vector2 Velocity,
	[property: Key(2)] Vector2 MoveDir,
	[property: Key(3)] bool Crouching,
	[property: Key(4)] float CrouchAmount,
	[property: Key(5)] Vector2 TargetLookPos,
	[property: Key(6)] IEnumerable<UpdateEventBase> Events
) : MessageBase;

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
	[property: Key(3)] float CurrentExperimentPosX,
	[property: Key(4)] float CurrentExperimentPosY,
	[property: Key(5)] UnityEngine.Random.State WorldGenSeed,
	[property: Key(6)] int BiomeDepth,
	[property: Key(7)] IReadOnlyDictionary<Vector2Int, ushort> ModifiedBlocks,
	[property: Key(8)] IEnumerable<int> DestroyedEntities

) : MessageBase;

using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using ScavMulti.Network.Messages;

namespace ScavMulti;

[HarmonyPatch]
public class BuildingEntityManager : MonoBehaviour
{
#	region Static stuff
	private static Dictionary<BuildingEntityManager, int> _entityToIdMap;
	private static Dictionary<int, BuildingEntityManager> _idToEntityMap;
	private static Dictionary<int, float> _damagedEntities;

	public static IReadOnlyDictionary<BuildingEntityManager, int> EntityToIdMap => _entityToIdMap;
	public static IReadOnlyDictionary<int, BuildingEntityManager> IdToEntityMap => _idToEntityMap;
	public static IReadOnlyDictionary<int, float> DamagedEntities => _damagedEntities;

	public static void SetupHooks()
	{
		GameFlowManager.OnWorldGenStart += () =>
		{
			_entityToIdMap = new();
			_idToEntityMap = new();
			_damagedEntities = new();
		};
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::BuildingEntity), nameof(global::BuildingEntity.Start))]
	static void BuildingEntity_Start_Postfix(BuildingEntity __instance)
	{
		__instance.gameObject.AddComponent<BuildingEntityManager>();
	}
#	endregion

	private bool _ignoreNextEvent = false;
	private float _previousHealth;
	public global::BuildingEntity BuildingEntity { get; private set; }
	public int Id { get; private set; }

	void Awake()
	{
		BuildingEntity = GetComponent<BuildingEntity>();
		_previousHealth = BuildingEntity.health;
		int seed = 0;
		void CombineHash(int hash)
		{
			seed ^= (int)(hash + 0x9e3779b9) + (seed << 6) + (seed >> 2);
		}
		CombineHash(transform.position.x.GetHashCode());
		CombineHash(transform.position.y.GetHashCode());
		CombineHash(transform.position.z.GetHashCode());
		CombineHash(transform.rotation.z.GetHashCode());
		Id = seed;
		_entityToIdMap.Add(this, Id);
		_idToEntityMap.Add(Id, this);
		if (NetMode.IAmTheClient && BuildingEntity.itemsDropOnDestroy.Length > 0)
		{
			// only the server generates drop items
			BuildingEntity.itemsDropOnDestroy = [];
		}
	}

	public void UpdateHealth(float newHealth, bool dontSync = true)
	{
		BuildingEntity.health = newHealth;
		_ignoreNextEvent = dontSync;
	}

	void Update()
	{
		if (NetMode.IAmTheClient && BuildingEntity.itemsDropOnDestroy.Length > 0)
		{
			// only the server generates drop items
			BuildingEntity.itemsDropOnDestroy = [];
		}
		if (BuildingEntity.health != _previousHealth && NetMode.OnlineAndPlaying)
		{
			if (!_ignoreNextEvent)
				MainExperiment.Instance.Events.Add(new EntityHealthSyncEvent(Id, BuildingEntity.health));
			_damagedEntities[Id] = BuildingEntity.health;
			_ignoreNextEvent = false;
			_previousHealth = BuildingEntity.health;
		}
	}

	void OnDestroy()
	{
		if (!GameFlowManager.IsWorldGenerating)
		{
			if (!_ignoreNextEvent)
				MainExperiment.Instance.Events.Add(new EntityHealthSyncEvent(Id, 0));
			_damagedEntities[Id] = 0;
			_entityToIdMap.Remove(this);
			_idToEntityMap.Remove(Id);
		}
	}
}

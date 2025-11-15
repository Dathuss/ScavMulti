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
	private static List<int> _destroyedEntityIds;

	public static IReadOnlyDictionary<BuildingEntityManager, int> EntityToIdMap => _entityToIdMap;
	public static IReadOnlyDictionary<int, BuildingEntityManager> IdToEntityMap => _idToEntityMap;
	public static IReadOnlyList<int> DestroyedEntityIds => _destroyedEntityIds;

	public static void SetupHooks()
	{
		GameFlowManager.OnWorldGenStart += () =>
		{
			_entityToIdMap = new();
			_idToEntityMap = new();
			_destroyedEntityIds = new();
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
	}

	public void UpdateHealth(float newHealth)
	{
		BuildingEntity.health = newHealth;
		_previousHealth = newHealth;
	}

	void Update()
	{
		if (BuildingEntity.health != _previousHealth && NetMode.Online)
		{
			MainExperiment.Instance.Events.Add(new EntityHealthSyncEvent(Id, BuildingEntity.health));
		}
		_previousHealth = BuildingEntity.health;
	}

	void OnDestroy()
	{
		if (!GameFlowManager.IsWorldGenerating)
		{
			_destroyedEntityIds.Add(Id);
			_entityToIdMap.Remove(this);
			_idToEntityMap.Remove(Id);
		}
	}
}

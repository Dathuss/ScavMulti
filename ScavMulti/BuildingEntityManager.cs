using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using ScavMulti.Network.Messages;

namespace ScavMulti;

[HarmonyPatch]
public class BuildingEntityManager : MonoBehaviour
{
#	region Static stuff
	private static int _currentMaxEntityId;
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
			_currentMaxEntityId = 0;
		};
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::BuildingEntity), nameof(global::BuildingEntity.Start))]
	static void BuildingEntity_Start_Postfix(BuildingEntity __instance)
	{
		__instance.gameObject.AddComponent<BuildingEntityManager>();
	}
#	endregion

	private float _previousHealth;
	public global::BuildingEntity BuildingEntity { get; private set; }
	public int Id { get; private set; }

	void Awake()
	{
		BuildingEntity = GetComponent<BuildingEntity>();
		_previousHealth = BuildingEntity.health;
		Id = _currentMaxEntityId++;
		if (!_entityToIdMap.ContainsKey(this))
		{
			_entityToIdMap.Add(this, Id);
			_idToEntityMap.Add(Id, this);
		}
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

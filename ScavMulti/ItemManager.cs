using UnityEngine;
using HarmonyLib;
using ScavMulti.Network.Messages;
using System.Collections.Generic;
using System;

namespace ScavMulti;

[HarmonyPatch]
public class ItemManager : MonoBehaviour
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::Item), nameof(global::Item.Awake))]
	static void Item_Awake_Postfix(Item __instance)
	{
		if (NetMode.IAmTheClient && !_pleaseDontKillYourself)
		{
			Object.Destroy(__instance.gameObject);
		}
		else
			__instance.gameObject.AddComponent<ItemManager>();
	}

	static int _incrementalItemId;
	static bool _pleaseDontKillYourself = false;
	static int _yourIdWillBe;
	static Dictionary<int, ItemManager> _itemMap;
	public static IReadOnlyDictionary<int, ItemManager> ItemMap => _itemMap;

	const float EVENT_UPDATE_RATE = 1f; // per second
	const int MAX_EVENT_PER_FRAME = 6;
	static int _numberOfEventsSentThisFrame = 0;
	internal static void ResetNumberOfEventsSentThisFrame() => _numberOfEventsSentThisFrame = 0;

	public static void SetupHooks()
	{
		GameFlowManager.OnWorldGenStart += () =>
		{
			_incrementalItemId = 0;
			_itemMap = [];
		};
	}

	public static void MakeItemCreateEvents(List<ItemCreateEvent> result)
	{
		foreach (var item in _itemMap.Values)
		{
			result.Add(item.MakeCreateEvent());
		}
	}

	public static ItemManager CreateItem(string itemType, int id, float freshDropTime)
	{
		_pleaseDontKillYourself = true;
		_yourIdWillBe = id;
		try
		{
			var instance = (GameObject)GameObject.Instantiate(Resources.Load(itemType));
			if (freshDropTime > 0)
				instance.AddComponent<global::FreshItemDrop>().timeLeft = freshDropTime;
			var manager = instance.GetComponent<ItemManager>();
			return manager;
		}
		catch (Exception e)
		{
			Logger.LogError($"CreateItem({itemType}, {id}, {freshDropTime}): {e}");
			return null;
		}
		finally
		{
			_pleaseDontKillYourself = false;
		}
	}

	public static ItemManager CreateItem(ItemCreateEvent itemCreateEvent)
	{
		var item = CreateItem(itemCreateEvent.Type, itemCreateEvent.InitialProps.Id, itemCreateEvent.FreshDropTime);
		item.ApplyUpdate(itemCreateEvent.InitialProps);
		return item;
	}

	public static void DestroyAllItemsLocally()
	{
		foreach (var item in GameObject.FindObjectsOfType<global::Item>())
		{
			var manager = item.GetComponent<ItemManager>();
			if (manager)
				manager._dieLocally = true;
			GameObject.Destroy(item.gameObject);
		}
		_itemMap.Clear();
		_incrementalItemId = 0;
	}

	public int Id;
	public global::Item Item { get; private set; }
	float _timeSinceLastUpdateSent;
	bool _dieLocally = false;

	void Awake()
	{
		Item = GetComponent<global::Item>();
		if (!NetMode.IAmTheClient)
			Id = _incrementalItemId++;
		else
			Id = _yourIdWillBe;
		_itemMap.Add(Id, this);
	}

	void Start()
	{
		if (NetMode.IAmTheServerAndPlaying)
		{
			MainExperiment.Instance.Events.Add(MakeCreateEvent());
		}
	}

	void Update()
	{
		if (NetMode.IAmTheServerAndPlaying)
		{
			_timeSinceLastUpdateSent += Time.deltaTime;
			if (_timeSinceLastUpdateSent >= EVENT_UPDATE_RATE && _numberOfEventsSentThisFrame < MAX_EVENT_PER_FRAME)
			{
				if (Experiments.SmallestExpieDistance(transform.position) < 200)
				{
					MainExperiment.Instance.Events.Add(MakeUpdateEvent());
					_numberOfEventsSentThisFrame++;
				}
				_timeSinceLastUpdateSent = 0;
			}
		}
	}

	ItemUpdateEvent MakeUpdateEvent()
	{
		return new ItemUpdateEvent(Id,
			transform.position,
			transform.rotation.z,
			Item.rb.velocity,
			Item.rb.angularVelocity,
			Item.condition
		);
	}

	ItemCreateEvent MakeCreateEvent()
	{
		var freshDrop = GetComponent<global::FreshItemDrop>();
		return new ItemCreateEvent(
			Item.id,
			freshDrop ? freshDrop.timeLeft : -1,
			MakeUpdateEvent()
		);
	}

	public void ApplyUpdate(ItemUpdateEvent updateEvent)
	{
		transform.position = updateEvent.Position;
		var rotation = transform.rotation;
		rotation.z = updateEvent.Rotation;
		transform.rotation = rotation;
		Item.rb.velocity = updateEvent.Velocity;
		Item.rb.angularVelocity = updateEvent.AngularVelocity;
		Item.condition = updateEvent.Condition;
	}

	void OnDestroy()
	{
		// TODO: sync
		if (!_dieLocally)
			_itemMap.Remove(Id);
	}
}

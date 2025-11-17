using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScavMulti;

/// <summary>
/// manages all OtherExperiment objects, which correspond to every player that is not
/// this user's player
/// </summary>
public class Experiments
{
	private static HashSet<OtherExperiment> _allExperiments;
	private static Dictionary<int, OtherExperiment> _idToExpieMap;
	public static IReadOnlyDictionary<int, OtherExperiment> IdToExpieMap => _idToExpieMap;
	public static IReadOnlyCollection<OtherExperiment> AllExperiments => _allExperiments;

	static Experiments()
	{
		_allExperiments = new();
		_idToExpieMap = new();
		GameFlowManager.OnRunLeave += () =>
		{
			_allExperiments.Clear();
			_idToExpieMap.Clear();
		};
	}

	public static OtherExperiment AddExperiment(int id, Vector3 position)
	{
		if (_idToExpieMap.ContainsKey(id))
			throw new InvalidOperationException($"AddExperiment called with duplicate ID {id}");
		var expie = OtherExperiment.CreateInstance(id, position);
		_allExperiments.Add(expie);
		_idToExpieMap.Add(id, expie);
		return expie;
	}

	public static void RemoveExperiment(int id)
	{
		if (_idToExpieMap.TryGetValue(id, out var expie))
		{
			_allExperiments.Remove(expie);
			expie.gameObject.SetActive(false);
			Object.Destroy(expie.gameObject);
		}
		else
			throw new InvalidOperationException($"RemoveExperiment called with unknown id {id}");
	}

	public static OtherExperiment FromId(int id)
	{
		return _idToExpieMap[id];
	}

	public static OtherExperiment FromBody(global::Body instance)
	{
		return _allExperiments.FirstOrDefault(x => x.Body == instance);
	}

	public static float SmallestExpieDistance(Vector2 pos)
	{
		float minDist = float.MaxValue;
		foreach (var expie in _allExperiments)
		{
			minDist = Mathf.Min(minDist, Vector2.Distance(expie.Body.transform.position, pos));
		}
		minDist = Mathf.Min(minDist, Vector2.Distance(MainExperiment.Instance.Body.transform.position, pos));
		return minDist;
	}
}

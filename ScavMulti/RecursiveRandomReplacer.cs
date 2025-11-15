using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace ScavMulti;

public record class RandomReplaceImplementations(
	MethodInfo ValueGetterMethod,
	MethodInfo RangeMethod,
	MethodInfo RangeIntMethod,
	MethodInfo InsideUnitCircleMethod
);

/// <summary>
/// This class was created after i realized the random functions in unity could be unpredictable
/// due to their globality. This created problems in the world gen.
/// The purpose of this is to recursively patch a method (coroutines are supported)
/// and its called methods, and reroute every call to a random function to a custom call
/// However, the peculiarity of it is that only the top method is actually patched,
/// the rest are cloned so they won't be rerouted outside of the main method.
/// This is essential to keep predictability.
/// For now it's hardcoded to only patch certain random methods, but if i need something
/// similar later, ill make it more generic.
/// </summary>
static class RecursiveRandomReplacer
{
	private static Dictionary<MethodBase, MethodInfo> _methodReplaceMap;
	private static Dictionary<MethodBase, MethodInfo> _originalToCloneMethods;

	private static IEnumerable<CodeInstruction> RandomTranspiler(IEnumerable<CodeInstruction> instructions, System.Reflection.Emit.ILGenerator generator)
	{
		var matcher = new CodeMatcher(instructions, generator);

		while (true)
		{
			matcher.SearchForward(x => x.opcode == System.Reflection.Emit.OpCodes.Call || x.opcode == System.Reflection.Emit.OpCodes.Callvirt);
			if (matcher.IsInvalid)
				break;
			var method = matcher.Operand as MethodBase;
			if (method != null &&
				(_methodReplaceMap.TryGetValue(method, out var replaceMethod) || _originalToCloneMethods.TryGetValue(method, out replaceMethod)))
			{
				// we reroute the called method to either our proxy or another cloned method
				matcher.Set(matcher.Opcode, replaceMethod);
			}
			matcher.Advance(1);
		}

		return matcher.InstructionEnumeration();
	}

	/// <summary>
	/// this function recursively analyzes all method calls in a method, and if they call
	/// any of the functions in Random, clones them and notifies they are to be patched
	/// by putting them in <see cref="_originalToCloneMethods" />
	/// </summary>
	/// <returns>
	/// true if the method, or any of its methods called, has a Random call and has to be patched
	/// </returns>
	private static bool RecursiveAddMethods(int depth, MethodBase originalMethod, HashSet<MethodBase> alreadyProcessedMethods)
	{
		bool atLeastOneMethodFound = false;
		if (IsCoroutine(originalMethod))
		{
			var moveNextMethod = AccessTools.EnumeratorMoveNext(originalMethod);

			if (!_methodReplaceMap.ContainsKey(moveNextMethod) && alreadyProcessedMethods.Add(moveNextMethod))
			{
				if (RecursiveAddMethods(depth + 1, moveNextMethod, alreadyProcessedMethods))
				{
					// the "main" coroutine is to not be copied, since it's what we want to patch in the first place
					if (depth != 0)
					{
						// enumerators are hell to copy due to them being types under the hood
						// in our case for now we won't need a deep enumerator copy so we just patch the original
						// var moveNextCopy = moveNextMethod.CreateILCopy();
						_originalToCloneMethods.Add(moveNextMethod, moveNextMethod);
						atLeastOneMethodFound = true;
					}
				}
			}
		}
		var body = new DynamicMethodDefinition(originalMethod).Definition.Body.Instructions;

		atLeastOneMethodFound |= body
			.Where(x => x.OpCode == OpCodes.Call)
			.Select(x => x.Operand as MethodReference)
			.Any(x => _methodReplaceMap.ContainsKey(x.ResolveReflection()));

		foreach (var calledMethod in body
			.Where(x => x.OpCode == OpCodes.Call || x.OpCode == OpCodes.Callvirt)
			.Select(x => (x.Operand as MethodReference).ResolveReflection())
			.Where(x => !_methodReplaceMap.ContainsKey(x) && x.HasMethodBody() && alreadyProcessedMethods.Add(x)))
		{
			if (RecursiveAddMethods(depth + 1, calledMethod, alreadyProcessedMethods))
			{
				var copy = calledMethod.CreateILCopy();
				_originalToCloneMethods.Add(calledMethod, copy);
				atLeastOneMethodFound = true;
			}
		}
		return atLeastOneMethodFound;
	}
	
	static bool IsCoroutine(MethodBase mb)
	{
		return mb.GetCustomAttribute<System.Runtime.CompilerServices.IteratorStateMachineAttribute>() != null;
	}

	public static void RecursivelyPatchMethod(Harmony harmony, MethodInfo baseMethod, RandomReplaceImplementations impls)
	{
		// this is super important, otherwise IL clones will throw an exception on patch
		Environment.SetEnvironmentVariable("MONOMOD_DMD_TYPE", "cecil");

		var unityRandomType = typeof(UnityEngine.Random);
		_methodReplaceMap = new()
		{
			{AccessTools.PropertyGetter(unityRandomType, "value"), impls.ValueGetterMethod},
			{AccessTools.Method(unityRandomType, "Range", [typeof(float), typeof(float)]), impls.RangeMethod},
			{AccessTools.Method(unityRandomType, "RandomRange", [typeof(float), typeof(float)]), impls.RangeMethod},
			{AccessTools.Method(unityRandomType, "Range", [typeof(int), typeof(int)]), impls.RangeIntMethod},
			{AccessTools.Method(unityRandomType, "RandomRange", [typeof(int), typeof(int)]), impls.RangeIntMethod},
			{AccessTools.PropertyGetter(unityRandomType, "insideUnitCircle"), impls.InsideUnitCircleMethod}
		};

		foreach (var kv in _methodReplaceMap)
		{
			if (kv.Value != null)
			{
				if (!kv.Value.IsStatic)
					throw new InvalidOperationException($"`{kv.Key.Name}` replacement method in not marked static");
				if (!Enumerable.SequenceEqual(kv.Key.GetParameters().Select(x => x.ParameterType), kv.Value.GetParameters().Select(x => x.ParameterType)))
					throw new InvalidOperationException($"`{kv.Key.Name}` replacement method's parameter(s) do not match original parameters");
				if ((kv.Key as MethodInfo).ReturnType != kv.Value.ReturnType)
					throw new InvalidOperationException($"`{kv.Key.Name}` replacement method's return type does not match original return type");
			}
		}

		_originalToCloneMethods = [];
		HashSet<MethodBase> alreadyProcessedMethods = [];

		RecursiveAddMethods(0, baseMethod, alreadyProcessedMethods);
		var transpileMethod = new HarmonyMethod(AccessTools.Method(typeof(RecursiveRandomReplacer), nameof(RandomTranspiler)));

		// here, the base method (and its MoveNext, if necessary)
		// are patched.
		harmony.Patch(baseMethod, transpiler: transpileMethod);
		if (IsCoroutine(baseMethod))
		{
			var moveNextMethod = AccessTools.EnumeratorMoveNext(baseMethod);
			harmony.Patch(moveNextMethod, transpiler: transpileMethod);
		}

		// the rest of the patched methods are clones
		foreach (var method in _originalToCloneMethods.Values)
		{
			harmony.Patch(method, transpiler: transpileMethod);
		}

		_methodReplaceMap.Clear();
		_originalToCloneMethods.Clear();
	}
}

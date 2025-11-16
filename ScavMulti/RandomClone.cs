using UnityEngine;

namespace ScavMulti;

/// <summary>
/// This classes exposes an exact reproduction of the UnityEngine.Random internals,
/// but it uses instances instead of a global state.
/// I reverse engineered it thanks to the debug information provided by IL2CPP.
/// Though i didn't reverse insideUnitSphere, onUnitSphere, rotation, and rotationUniform
/// because i don't need them for now.
/// </summary>
public static class RandomClone
{
	static uint GetRand(ref Random.State state)
	{
		// https://www.jstatsoft.org/article/download/v008i14/916
		// the algorithm is based on a Xorshift 128 algorithm
		uint rand = (uint)((state.s0 << 11) ^ (uint)state.s0);
		state.s0 = state.s1;
		state.s1 = state.s2;
		uint s3 = (uint)state.s3;
		state.s2 = (int)s3;
		rand = (((s3 >> 11) ^ rand) >> 8) ^ s3 ^ rand;
		state.s3 = (int)rand;
		return rand;
	}

	static float GetRandFloat(ref Random.State state)
	{
		const float FLT_EPSILON = 1.192093e-07f;

		uint rand = (uint)((state.s0 << 11) ^ (uint)state.s0);
		state.s0 = state.s1;
		state.s1 = state.s2;
		uint s3 = (uint)state.s3;
		state.s2 = (int)s3;
		rand = (((s3 >> 11) ^ rand) >> 8) ^ s3 ^ rand;
		state.s3 = (int)rand;
		return (rand & 0x7FFFFF) * FLT_EPSILON;
	}

	// Random_Get_Custom_PropValue
	public static float Value(ref Random.State state)
	{
		float result = GetRandFloat(ref state);
		return result;
	}

	// Random_CUSTOM_Range
	public static float Range(float min, float max, ref Random.State state)
	{
		float rand = GetRandFloat(ref state);
		// by default, Mono uses extended floating points (80-bit), while
		// the unity runtime uses standard 32-bit numbers
		// this may yield different results the bigger numbers get
		// however if we use vectorized operations then it "forces"
		// our code to calculate on 32-bit
		System.Numerics.Vector<float> oneV = new(1.0f);
		System.Numerics.Vector<float> randV = new(rand);
		System.Numerics.Vector<float> minV = new(min);
		System.Numerics.Vector<float> maxV = new(max);
		float res = (float)((oneV - randV) * maxV + randV * minV)[0];
		return res;
	}

	// Random_CUSTOM_RandomRangeInt
	public static int Range(int min, int max, ref Random.State state)
	{
		int result = min;
		if (min < max)
		{
			result = (int)(min + GetRand(ref state) % (max - min));
		}
		else if (max < min)
		{
			result = (int)(min - GetRand(ref state) % (min - max));
		}
		return result;
	}

	// Random_CUSTOM_GetRandomUnitCircle
	public static Vector2 InsideUnitCircle(ref Random.State state)
	{
		float theta = (1 - GetRandFloat(ref state)) * 2 * Mathf.PI;
		float cos = Mathf.Cos(theta);
		float sin = Mathf.Sin(theta);
		float length = Mathf.Sqrt(1 - GetRandFloat(ref state));
		var res = new Vector2(sin * length, cos * length);
		return res;
	}
}

using System;
using System.Collections;
using UnityEngine;

namespace GptDeepResearch
{
	public static class CoroutineRunner
	{
		// Safely executes a coroutine, catching exceptions and sending error messages via onError
		public static IEnumerator SafeExecute(IEnumerator routine, float stepDelay, Action<string> onError, Action onComplete) // may need to remove , onComplete
		{
			while (true)
			{
				object current = null;
				try
				{
					if (!routine.MoveNext())
					{
						break;
					}
					current = routine.Current;
				}
				catch (Exception ex)
				{
					onError?.Invoke(ex.Message);
					yield break;
				}

				if (current is IEnumerator nested)
				{
					// If the yielded value is another IEnumerator, wrap it
					yield return SafeExecute(nested, stepDelay, onError, onComplete); // may need to remove , onComplete
				}
				else if (current == null)
				{
					// No yield from interpreter; apply step delay
					if (stepDelay > 1f / 100)
						yield return new WaitForSecondsRealtime(stepDelay);
					else
						yield return null;
				}
				else
				{
					// Yield the actual instruction (e.g., WaitForSecondsRealtime from sleep)
					yield return current;
				}
			}
		}

		public static IEnumerator Wait(float seconds)
		{
			yield return new WaitForSecondsRealtime(seconds);
		}
	}
}

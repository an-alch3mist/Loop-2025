using System;
using System.Collections;
using UnityEngine;
// LABELED DIFF FOR CoroutineRunner.cs
// Add better error handling and console integration



namespace GptDeepResearch
{
	// LABELED DIFF FOR CoroutineRunner.cs
	// Minor update to handle the onComplete callback properly

	public static class CoroutineRunner
	{

		// MODIFY SafeExecute method for improved error handling:
		public static IEnumerator SafeExecute(IEnumerator routine, float stepDelay, Action<string> onError, Action onComplete = null)
		{
			while (true)
			{
				object current = null;
				try
				{
					if (!routine.MoveNext())
					{
						// MODIFY: Call onComplete when routine finishes successfully
						onComplete?.Invoke();
						break;
					}
					current = routine.Current;
				}
				catch (Exception ex)
				{
					// MODIFY: Improved error handling with console integration
					string errorMessage = $"Runtime error: {ex.Message}";

					// Try to send to console manager first
					try
					{
						ConsoleManager.AddMessage(errorMessage, ConsoleMessageType.Error);
					}
					catch
					{	// without .logerror
						// Fallback to Unity console if console manager fails
						Debug.Log("errorFallBack: " + errorMessage);
					}

					onError?.Invoke(ex.Message);
					yield break;
				}

				if (current is IEnumerator nested)
				{
					// If the yielded value is another IEnumerator, wrap it
					yield return SafeExecute(nested, stepDelay, onError, null); // Don't pass onComplete to nested
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

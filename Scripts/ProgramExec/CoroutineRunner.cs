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
		// LABELED DIFF FOR CoroutineRunner.cs
		// Improve error handling to catch ALL exceptions and route to ConsoleManager

		// REPLACE the entire SafeExecute method (around line 15):
		public static IEnumerator SafeExecute(IEnumerator routine, float stepDelay, Action<string> onError, Action onComplete = null)
		{
			while (true)
			{
				object current = null;
				bool routineFinished = false;
				bool hasError = false;
				string errorMessage = "";

				// Try to get next value from routine
				try
				{
					routineFinished = !routine.MoveNext();
					if (routineFinished)
					{
						// Routine finished successfully
						onComplete?.Invoke();
						break;
					}
					current = routine.Current;
				}
				catch (System.Exception ex)
				{
					// Mark error and prepare message outside try-catch
					hasError = true;
					errorMessage = ex.Message;
				}

				// Handle error outside try-catch to avoid yield return limitation
				if (hasError)
				{
					// Send to console manager with error styling
					try
					{
						ConsoleManager.LogError(errorMessage);
					}
					catch
					{
						// Ultimate fallback - should never happen
						UnityEngine.Debug.Log($"Console fallback: Runtime error: {errorMessage}");
					}

					// Also call the original error callback for compatibility
					onError?.Invoke(errorMessage);
					yield break;
				}

				// Handle nested coroutines and delays (outside try-catch)
				if (current is IEnumerator nested)
				{
					// If the yielded value is another IEnumerator, wrap it recursively
					yield return SafeExecute(nested, stepDelay, onError, null);
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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GptDeepResearch
{
	/// <summary>
	/// Global manager to control all ScriptRunner instances in the scene
	/// Handles state transitions between Running/Reset states
	/// </summary>
	public static class GlobalScriptManager
	{
		// Event broadcasted when all runners should stop
		public static event System.Action OnStopAllRunners;

		// Event broadcasted when console should be cleared
		public static event System.Action OnClearConsole;

		// Track all registered ScriptRunner instances
		private static HashSet<ScriptRunner> registeredRunners = new HashSet<ScriptRunner>();

		// Currently running ScriptRunner (null if none)
		private static ScriptRunner currentRunningScript = null;

		/// <summary>
		/// Register a ScriptRunner instance for global management
		/// </summary>
		public static void RegisterRunner(ScriptRunner runner)
		{
			registeredRunners.Add(runner);
		}

		/// <summary>
		/// Unregister a ScriptRunner instance
		/// </summary>
		public static void UnregisterRunner(ScriptRunner runner)
		{
			registeredRunners.Remove(runner);
			if (currentRunningScript == runner)
			{
				currentRunningScript = null;
			}
		}

		/// <summary>
		/// Called when a ScriptRunner's Run button is pressed
		/// Stops all other runners and starts the specified one
		/// </summary>
		// MODIFY the StartRunner method (around line 45):
		public static void StartRunner(ScriptRunner runner)
		{
			// Stop all runners first
			StopAllRunners();

			// REMOVE: Don't clear console anymore - let messages accumulate
			// ClearConsole();

			// Set new running script
			currentRunningScript = runner;

			// Transition all runners to appropriate states
			foreach (var r in registeredRunners)
			{
				if (r == runner)
				{
					r.SetState(ScriptRunner.ScriptState.Running);
				}
				else
				{
					r.SetState(ScriptRunner.ScriptState.Reset);
				}
			}
		}

		/// <summary>
		/// Called when any ScriptRunner's Reset button is pressed
		/// Stops all runners and resets all to Reset state
		/// </summary>
		public static void ResetAllRunners()
		{
			// Stop all execution
			StopAllRunners();

			// Reset current running script
			currentRunningScript = null;

			// Set all runners to Reset state
			foreach (var runner in registeredRunners)
			{
				runner.SetState(ScriptRunner.ScriptState.Reset);
			}
		}

		/// <summary>
		/// Called when a script encounters an error
		/// Transitions the errored script back to Reset state
		/// </summary>
		public static void OnScriptError(ScriptRunner erroredRunner)
		{
			if (currentRunningScript == erroredRunner)
			{
				currentRunningScript = null;
				erroredRunner.SetState(ScriptRunner.ScriptState.Reset);
			}
		}

		/// <summary>
		/// Called when a script completes successfully
		/// </summary>
		public static void OnScriptComplete(ScriptRunner completedRunner)
		{
			if (currentRunningScript == completedRunner)
			{
				currentRunningScript = null;
				completedRunner.SetState(ScriptRunner.ScriptState.Reset);
			}
		}

		/// <summary>
		/// Get the currently running ScriptRunner (null if none)
		/// </summary>
		public static ScriptRunner GetCurrentRunningScript()
		{
			return currentRunningScript;
		}

		/// <summary>
		/// Stop all running scripts
		/// </summary>
		private static void StopAllRunners()
		{
			OnStopAllRunners?.Invoke();
		}

		/// <summary>
		/// Clear the console
		/// </summary>
		// MODIFY the ClearConsole method to be a no-op (around line 110):
		private static void ClearConsole()
		{
			// DON'T clear console anymore - preserve message history
			// OnClearConsole?.Invoke();

			// Keep the method for backwards compatibility but make it do nothing
		}

		/// <summary>
		/// Check if any script is currently running
		/// </summary>
		public static bool IsAnyScriptRunning()
		{
			return currentRunningScript != null;
		}

		/// <summary>
		/// Cleanup - call this when scene changes
		/// </summary>
		public static void Cleanup()
		{
			registeredRunners.Clear();
			currentRunningScript = null;
		}
	}
}
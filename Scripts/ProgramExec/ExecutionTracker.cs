using System;
using UnityEngine;

namespace GptDeepResearch
{
	/// <summary>
	/// Tracks Python script execution for line highlighting
	/// Usage: Call ExecutionTracker.NotifyLineExecution(lineNumber) from PythonInterpreter
	/// </summary>
	public static class ExecutionTracker
	{
		// Event fired when a line is executed
		public static event Action<int> OnLineExecuted;

		// Event fired when execution starts
		public static event Action OnExecutionStarted;

		// Event fired when execution stops/completes
		public static event Action OnExecutionStopped;

		/// <summary>
		/// Call this from PythonInterpreter when executing each statement
		/// </summary>
		public static void NotifyLineExecution(int lineNumber)
		{
			OnLineExecuted?.Invoke(lineNumber);
		}

		/// <summary>
		/// Call this when script execution begins
		/// </summary>
		public static void NotifyExecutionStarted()
		{
			OnExecutionStarted?.Invoke();
		}

		/// <summary>
		/// Call this when script execution stops or completes
		/// </summary>
		public static void NotifyExecutionStopped()
		{
			OnExecutionStopped?.Invoke();
		}
	}
}
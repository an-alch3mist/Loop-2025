// LABELED DIFF FOR ScriptRunner.cs
// Add these changes to your existing ScriptRunner.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;


// LABELED DIFF FOR ScriptRunner.cs
// Add line execution tracking and console integration

// ADD after the using statements (around line 6):
using TMPro;

namespace GptDeepResearch
{
	public class ScriptRunner : MonoBehaviour
	{
		// ADD: New enum for script states
		public enum ScriptState
		{
			Reset,    // Default state - Run button enabled, input editable
			Running   // Executing state - Run button disabled, input read-only
		}

		[Header("UI References")]
		public TMP_InputField scriptInput;
		public TMP_InputField consoleOutput;
		// REMOVE: public LineHighlighter lineHighlighter; // We're removing LineHighlighter

		// ADD: New UI references for line number highlighting
		[Header("Line Number References")]
		public TextMeshProUGUI lineNumbersText;  // Reference to line_number tmp component

		// ADD: New UI reference for Run, Reset button
		[Header("Control Buttons")]
		public Button runButton;
		public Button resetButton;

		[Header("Settings")]
		public static float stepDelay = 1f / 10;

		[Header("error prefix")]
		[SerializeField] string errorPrefix = "main_script.py ";
		[SerializeField] TMP_InputField title_inputfield;

		public string ErrorLog { get; private set; } = "";
		private bool isExecuting = false;

		// ADD: Current state and line tracking
		private ScriptState currentState = ScriptState.Reset;
		private int currentExecutingLine = -1;
		private List<string> codeLines = new List<string>();

		// MODIFY the Start method (around line 60):
		void Start()
		{
			// ad
			this.errorPrefix = title_inputfield.text;

			// MODIFY: Register with GlobalScriptManager
			GlobalScriptManager.RegisterRunner(this);

			// MODIFY: Subscribe to global events
			GlobalScriptManager.OnStopAllRunners += StopExecution;
			GlobalScriptManager.OnClearConsole += ClearConsole;

			if (runButton != null)
				runButton.onClick.AddListener(() => {
					OnRunButtonPressed();
				});

			// ADD: Subscribe to Reset button
			if (resetButton != null)
				resetButton.onClick.AddListener(() => {
					OnResetButtonPressed();
				});

			// ADD: Subscribe to line execution events for highlighting
			ExecutionTracker.OnLineExecuted += OnLineExecuted;
			ExecutionTracker.OnExecutionStarted += OnExecutionStarted;
			ExecutionTracker.OnExecutionStopped += OnExecutionStopped;

			// ADD: Initialize state
			SetState(ScriptState.Reset);
		}


		// MODIFY OnDestroy method (around line 80):
		void OnDestroy()
		{
			// ADD: Unregister from GlobalScriptManager
			GlobalScriptManager.UnregisterRunner(this);
			GlobalScriptManager.OnStopAllRunners -= StopExecution;
			GlobalScriptManager.OnClearConsole -= ClearConsole;

			// ADD: Unsubscribe from execution events
			ExecutionTracker.OnLineExecuted -= OnLineExecuted;
			ExecutionTracker.OnExecutionStarted -= OnExecutionStarted;
			ExecutionTracker.OnExecutionStopped -= OnExecutionStopped;
		}

		// ADD new event handlers (around line 160):
		private void OnExecutionStarted()
		{
			// Only highlight if this is the currently running script
			if (GlobalScriptManager.GetCurrentRunningScript() == this)
			{
				UpdateLineNumberHighlighting();
			}
		}
		private void OnExecutionStopped()
		{
			// Only update if this was the running script
			if (GlobalScriptManager.GetCurrentRunningScript() == this || currentExecutingLine != -1)
			{
				currentExecutingLine = -1;
				UpdateLineNumberHighlighting();
			}
		}


		// MODIFY: Rename and change behavior
		private void OnRunButtonPressed()
		{
			// Let GlobalScriptManager handle the coordination
			GlobalScriptManager.StartRunner(this);

			// Start our execution
			RunScript();
		}

		// MODIFY the OnResetButtonPressed method to also reset scene:
		private void OnResetButtonPressed()
		{
			// Reset scene first, then coordinate with GlobalScriptManager
			StartCoroutine(ResetWithScene());
		}
		// MODIFY the ResetWithScene method (around line 130):
		private IEnumerator ResetWithScene()
		{
			// Stop any running execution first
			if (isExecuting)
			{
				StopExecution();
				yield return null; // Wait a frame
			}

			// Reset scene state
			yield return ResetSceneState();

			// ADD: Log reset message instead of clearing console
			ConsoleManager.LogInfo("Script reset has been made");

			// Then coordinate with GlobalScriptManager
			GlobalScriptManager.ResetAllRunners();
		}

		// MODIFY the RunScript method (around line 120):
		internal void RunScript()
		{
			if (isExecuting)
			{
				Debug.Log("Script is already running!");
				return;
			}

			ErrorLog = "";

			// ADD: Update code lines for line highlighting
			UpdateCodeLines();

			try
			{
				var lexer = new PythonLexer(scriptInput.text);
				var parser = new PythonParser(lexer.Tokens);
				List<Stmt> ast = parser.Parse();

				var interpreter = new PythonInterpreter();
				isExecuting = true;

				// ADD: Notify execution started
				ExecutionTracker.NotifyExecutionStarted();

				// MODIFY: Start with scene reset, then execute script
				StartCoroutine(ExecuteWithSceneReset(interpreter, ast));
			}
			catch (System.Exception ex)
			{
				ReportError(ex.Message);
				isExecuting = false;
				// ADD: Notify execution stopped on error
				ExecutionTracker.NotifyExecutionStopped();
				GlobalScriptManager.OnScriptError(this);
			}
		}
		
		// ADD: New method to handle scene reset + script execution
		private IEnumerator ExecuteWithSceneReset(PythonInterpreter interpreter, List<Stmt> ast)
		{
			// First, reset the scene
			yield return ResetSceneState();

			// Then execute the script
			yield return CoroutineRunner.SafeExecute(
				interpreter.Execute(ast),
				stepDelay,
				ReportError,
				OnExecutionComplete
			);
		}

		// MODIFY: Update line execution handling
		private void OnLineExecuted(int lineNumber)
		{
			// Only highlight if this is the currently running script
			if (GlobalScriptManager.GetCurrentRunningScript() == this)
			{
				currentExecutingLine = lineNumber;
				UpdateLineNumberHighlighting();
			}
		}

		// ADDITIONAL PATCH for ScriptRunner.cs
		// Add this method to integrate scene reset functionality

		// ADD this method to ScriptRunner.cs (around line 150, before UpdateCodeLines method)

		/// <summary>
		/// Reset scene state before running script
		/// </summary>
		private IEnumerator ResetSceneState()
		{
			// Call scene reset if available
			yield return GameBuiltinMethods.ResetScene();
		}


		// MODIFY OnExecutionComplete method (around line 170):
		private void OnExecutionComplete()
		{
			isExecuting = false;
			currentExecutingLine = -1;
			UpdateLineNumberHighlighting(); // Reset highlighting

			Debug.Log("Script execution completed.");

			// ADD: Notify execution stopped
			ExecutionTracker.NotifyExecutionStopped();

			// ADD: Notify GlobalScriptManager of completion
			GlobalScriptManager.OnScriptComplete(this);
		}

		// MODIFY the ReportError method to ensure errors go to console (around line 180):
		private void ReportError(string msg)
		{
			string errorMessage = $"@{errorPrefix}: {msg}";
			ErrorLog += errorMessage + "\n";

			// Send error to console manager
			ConsoleManager.LogError(errorMessage);

			isExecuting = false;
			currentExecutingLine = -1;
			UpdateLineNumberHighlighting();

			// Notify execution stopped on error
			ExecutionTracker.NotifyExecutionStopped();

			// Notify GlobalScriptManager of error
			GlobalScriptManager.OnScriptError(this);
		}

		// MODIFY StopExecution method (around line 200):
		public void StopExecution()
		{
			if (isExecuting)
			{
				StopAllCoroutines();
				isExecuting = false;
				currentExecutingLine = -1;
				UpdateLineNumberHighlighting(); // Reset highlighting

				// ADD: Notify execution stopped
				ExecutionTracker.NotifyExecutionStopped();

				ReportError("Execution stopped by user");
			}
		}

		// MODIFY the ClearConsole method to NOT clear the console (around line 220):
		private void ClearConsole()
		{
			// DON'T clear the console anymore - let it accumulate messages
			// Keep this method for compatibility but make it do nothing
			// if (consoleOutput != null)
			//     consoleOutput.text = "";
			// ErrorLog = "";

			// Instead, just reset the ErrorLog for this script runner
			ErrorLog = "";
		}

		// ADD: State management method
		public void SetState(ScriptState newState)
		{
			currentState = newState;

			switch (newState)
			{
				case ScriptState.Reset:
					// Enable Run button, make input editable
					if (runButton != null)
						runButton.interactable = true;
					if (scriptInput != null)
						scriptInput.readOnly = false;

					// Reset line highlighting to gray
					currentExecutingLine = -1;
					UpdateLineNumberHighlighting();
					break;

				case ScriptState.Running:
					// Disable Run button, make input read-only
					if (runButton != null)
						runButton.interactable = false;
					if (scriptInput != null)
						scriptInput.readOnly = true;
					break;
			}
		}

		// ADD: Update code lines for line highlighting
		private void UpdateCodeLines()
		{
			codeLines.Clear();
			if (!string.IsNullOrEmpty(scriptInput.text))
			{
				codeLines.AddRange(scriptInput.text.Split('\n'));
			}
		}

		// ADD: Line number highlighting method (replaces LineHighlighter)
		private void UpdateLineNumberHighlighting()
		{
			if (lineNumbersText == null) return;

			// Parse existing line numbers text
			string[] lines = lineNumbersText.text.Split('\n');
			System.Text.StringBuilder newText = new System.Text.StringBuilder();

			for (int i = 0; i < lines.Length; i++)
			{
				if (i > 0) newText.AppendLine();

				// Extract line number from formatted text (remove any existing color tags)
				string lineText = lines[i];
				lineText = System.Text.RegularExpressions.Regex.Replace(lineText, @"<color[^>]*>|</color>", "");

				// Apply highlighting
				if (currentState == ScriptState.Running && currentExecutingLine == i + 1)
				{
					newText.Append($"<color=#fefefe>{lineText}</color>");
				}
				else
				{
					newText.Append($"<color=#3a3a3a>{lineText}</color>");
				}
			}

			lineNumbersText.text = newText.ToString();
		}

		// ADD: Public getter for current state
		public ScriptState GetCurrentState()
		{
			return currentState;
		}
	}
}
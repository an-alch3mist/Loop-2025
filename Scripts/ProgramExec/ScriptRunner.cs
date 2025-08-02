using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace GptDeepResearch
{
	public class ScriptRunner : MonoBehaviour
	{
		[Header("UI References")]
		public TMP_InputField scriptInput;
		public TMP_InputField consoleOutput;
		public Button runButton;
		public LineHighlighter lineHighlighter;

		[Header("Settings")]
		public static float stepDelay = 1f / 10;
		public string errorPrefix = "main_script.py ";

		public string ErrorLog { get; private set; } = "";
		private bool isExecuting = false;

		void Start()
		{
			if (runButton != null)
				runButton.onClick.AddListener(() => {
					RunScript();
				});

			// Subscribe to line execution events
			ExecutionTracker.OnLineExecuted += OnLineExecuted;
		}

		void OnDestroy()
		{
			ExecutionTracker.OnLineExecuted -= OnLineExecuted;
		}

		public void RunScript()
		{
			if (isExecuting)
			{
				Debug.Log("Script is already running!");
				return;
			}

			ErrorLog = "";
			if (consoleOutput != null)
				consoleOutput.text = "";

			// Update line highlighter with current code
			if (lineHighlighter != null)
				lineHighlighter.UpdateCodeLines(scriptInput.text);

			try
			{
				var lexer = new PythonLexer(scriptInput.text);
				var parser = new PythonParser(lexer.Tokens);
				List<Stmt> ast = parser.Parse();

				var interpreter = new PythonInterpreter();
				isExecuting = true;
				StartCoroutine(CoroutineRunner.SafeExecute(
					interpreter.Execute(ast),
					stepDelay,
					ReportError,
					OnExecutionComplete
				));
			}
			catch (System.Exception ex)
			{
				ReportError(ex.Message);
				isExecuting = false;
			}
		}

		private void OnLineExecuted(int lineNumber)
		{
			if (lineHighlighter != null)
			{
				lineHighlighter.HighlightLine(lineNumber);
			}
		}

		private void OnExecutionComplete()
		{
			isExecuting = false;
			if (lineHighlighter != null)
			{
				lineHighlighter.HideHighlight();
			}
			Debug.Log("Script execution completed.");
		}

		private void ReportError(string msg)
		{
			ErrorLog += $"@{errorPrefix}: {msg}\n";
			if (consoleOutput != null)
				consoleOutput.text = ErrorLog;

			isExecuting = false;
			if (lineHighlighter != null)
				lineHighlighter.HideHighlight();
		}

		// Public method to stop execution (for stop button)
		public void StopExecution()
		{
			if (isExecuting)
			{
				StopAllCoroutines();
				isExecuting = false;
				if (lineHighlighter != null)
					lineHighlighter.HideHighlight();

				ReportError("Execution stopped by user");
			}
		}
	}
}
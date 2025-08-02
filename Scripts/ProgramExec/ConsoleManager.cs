using System;
using System.Text;
using UnityEngine;
using TMPro;

namespace GptDeepResearch
{
	/// <summary>
	/// Manages console output for both Python print() and Unity Debug.Log
	/// Usage: Attach to a GameObject with TMP_Text component for console display
	/// </summary>
	public class ConsoleManager : MonoBehaviour
	{
		[Header("Console Display")]
		[SerializeField] private TMP_InputField consoleDisplay;

		[Header("Settings")]
		[SerializeField] private int maxLines = 1000;
		[SerializeField] private bool showUnityLogs = true;
		[SerializeField] private bool showTimestamps = false;
		[SerializeField] bool autoScroll = true;

		private StringBuilder consoleText = new StringBuilder();
		private int currentLineCount = 0;

		// Singleton instance
		public static ConsoleManager Instance { get; private set; }

		void Awake()
		{
			// Singleton pattern
			if (Instance == null)
			{
				Instance = this;

				if (consoleDisplay == null)
					consoleDisplay = GetComponent<TMP_InputField>();

				// Subscribe to Unity's log messages if enabled
				if (showUnityLogs)
				{
					Application.logMessageReceived += OnUnityLogReceived;
				}
			}
			else
			{
				Destroy(gameObject);
			}
		}

		void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
				Application.logMessageReceived -= OnUnityLogReceived;
			}
		}

		static void ScrollToBottom(TMP_InputField inputField)
		{
			// Force the UI to rebuild layouts so the content height is updated
			// Canvas.ForceUpdateCanvases();

			// 0 = bottom; 1 = top (verticalNormalizedPosition is inverted)
			// consoleDisplay.verticalNormalizedPosition = 0f;
		}

		/// <summary>
		/// Add a message to the console (called by Python print())
		/// </summary>
		public static void AddMessage(string message, ConsoleMessageType type = ConsoleMessageType.Print)
		{
			if (Instance != null)
			{
				Instance.AddMessageInternal(message, type);
			}
		}

		/// <summary>
		/// Clear the console
		/// </summary>
		public static void Clear()
		{
			if (Instance != null)
			{
				Instance.ClearInternal();
			}
		}

		private void AddMessageInternal(string message, ConsoleMessageType type)
		{
			try
			{
				// Add timestamp if enabled
				string timestamp = showTimestamps ? $"[{DateTime.Now:HH:mm:ss}] " : "";

				// Add type prefix based on message type
				string prefix = GetTypePrefix(type);

				// Format the message
				string formattedMessage = $"{timestamp}{prefix}{message}";

				// Add to console text
				if (currentLineCount > 0)
					consoleText.AppendLine();

				consoleText.Append(formattedMessage);
				currentLineCount++;

				// Trim old lines if we exceed max
				if (currentLineCount > maxLines)
				{
					TrimOldLines();
				}

				// Update display
				UpdateDisplay();
			}
			catch (Exception ex)
			{
				// Fallback to Unity console if our console fails
				Debug.LogError($"ConsoleManager error: {ex.Message}");
			}
		}

		private void ClearInternal()
		{
			consoleText.Clear();
			currentLineCount = 0;
			UpdateDisplay();
		}

		private void OnUnityLogReceived(string logString, string stackTrace, LogType type)
		{
			// Only show Debug.Log messages (not warnings/errors to avoid spam)
			if (type == LogType.Log)
			{
				AddMessageInternal(logString, ConsoleMessageType.Unity);
			}
			// 
			if(this.autoScroll  == true)
				ScrollToBottom(this.consoleDisplay);
		}

		private string GetTypePrefix(ConsoleMessageType type)
		{
			switch (type)
			{
				case ConsoleMessageType.Print:
					return "";
				case ConsoleMessageType.Error:
					return "<color=red>[ERROR]</color> ";
				case ConsoleMessageType.Warning:
					return "<color=yellow>[WARNING]</color> ";
				case ConsoleMessageType.Unity:
					return "<color=cyan>[UNITY]</color> ";
				default:
					return "";
			}
		}

		private void TrimOldLines()
		{
			string[] lines = consoleText.ToString().Split('\n');
			int linesToKeep = maxLines - 100; // Keep some buffer

			if (lines.Length > linesToKeep)
			{
				consoleText.Clear();

				for (int i = lines.Length - linesToKeep; i < lines.Length; i++)
				{
					if (consoleText.Length > 0)
						consoleText.AppendLine();
					consoleText.Append(lines[i]);
				}

				currentLineCount = linesToKeep;
			}
		}

		private void UpdateDisplay()
		{
			if (consoleDisplay != null)
			{
				consoleDisplay.text = consoleText.ToString();

				// Auto-scroll to bottom
				Canvas.ForceUpdateCanvases();
			}
		}

		/*
		/// <summary>
		/// Set the console display component
		/// </summary>
		public void SetConsoleDisplay(TMP_Text display)
		{
			consoleDisplay = display;
			UpdateDisplay();
		}
		*/
	}

	public enum ConsoleMessageType
	{
		Print,
		Error,
		Warning,
		Unity
	}
}
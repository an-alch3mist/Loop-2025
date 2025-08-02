using System;
using System.Text;
using UnityEngine;

using UnityEngine.UI;
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
			}
		}

		// MODIFY ScrollToBottom method to be more reliable:
		static void ScrollToBottom(TMP_InputField inputField)
		{
			if (inputField != null)
			{
				var scrollRect = inputField.GetComponent<ScrollRect>();
				if (scrollRect != null)
				{
					// Force content size update
					Canvas.ForceUpdateCanvases();
					// 0 = bottom; 1 = top (verticalNormalizedPosition is inverted)
					scrollRect.verticalNormalizedPosition = 0f;
				}
			}
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
		// ADD these new static methods after Clear() method (around line 75):
		/// <summary>
		/// Log an info message (yellow color)
		/// </summary>
		public static void LogInfo(string message)
		{
			if (Instance != null)
			{
				Instance.AddMessageInternal($"<color=yellow>{message}</color>", ConsoleMessageType.Print);
			}
		}
		/// <summary>
		/// Log an error message (red color)
		/// </summary>
		public static void LogError(string message)
		{
			if (Instance != null)
			{
				Instance.AddMessageInternal($"<color=red>[ERROR]</color> {message}", ConsoleMessageType.Error);
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

				/*
				if (currentLineCount > 0)
					consoleText.AppendLine();
				consoleText.Append(formattedMessage);
				*/
				AppendLineToBeginning(consoleText, formattedMessage);

				currentLineCount++;

				/*
				// Trim old lines if we exceed max
				if (currentLineCount > maxLines)
				{
					TrimOldLines();
				}
				*/

				// Update display
				UpdateDisplay();
			}
			catch (Exception ex)
			{
				// Fallback to Unity console if our console fails
				Debug.LogError($"ConsoleManager error: {ex.Message}");
			}
		}
		static void AppendLineToBeginning(StringBuilder sb, string formattedMessage)
		{
			// Only '\n'—no '\r'
			string newline = sb.Length > 0 ? "\n" : "\n";
			string line = $"{newline}{formattedMessage}";
			sb.Insert(0, line);
		}

		private void ClearInternal()
		{
			consoleText.Clear();
			currentLineCount = 0;
			UpdateDisplay();
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

		// MODIFY UpdateDisplay method (around line 160) - ADD auto-scroll after text update:
		private void UpdateDisplay()
		{
			if (consoleDisplay != null)
			{
				consoleDisplay.text = consoleText.ToString();

				// Force canvas update and auto-scroll to bottom
				Canvas.ForceUpdateCanvases();

				if (autoScroll)
				{
					ScrollToBottom(consoleDisplay);
				}
			}
		}
	}

	public enum ConsoleMessageType
	{
		Print,
		Error,
		Warning,
		Unity
	}
}
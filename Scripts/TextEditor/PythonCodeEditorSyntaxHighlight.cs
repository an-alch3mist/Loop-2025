using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;

using SPACE_UTIL;

namespace CodeEditor
{
	/// <summary>
	/// Professional Python code editor with syntax highlighting, auto-indentation, and smart editing features
	/// </summary>
	public class PythonCodeEditorSyntaxHighlight : MonoBehaviour
	{
		[Header("Components")]
		[SerializeField] private TMP_InputField inputField;
		[SerializeField] private TextMeshProUGUI displayText; // Separate display component

		[Header("Syntax Colors")]
		[SerializeField] private Color keywordColor = new Color(0.3f, 0.5f, 1f); // Blue
		[SerializeField] private Color stringColor = new Color(0.2f, 0.8f, 0.2f); // Green
		[SerializeField] private Color numberColor = new Color(1f, 0.6f, 0.2f); // Orange
		[SerializeField] private Color commentColor = new Color(0.6f, 0.6f, 0.6f); // Gray
		[SerializeField] private Color defaultColor = Color.white;

		[Header("Editor Settings")]
		[SerializeField] private int tabSize = 4;
		[SerializeField] private bool enableSyntaxHighlighting = true;
		[SerializeField] private bool enableAutoIndent = true;

		// Raw text storage (without rich text tags)
		private string rawText = "";
		private bool isUpdatingText = false;

		// Syntax highlighting
		private readonly SyntaxHighlighter syntaxHighlighter = new SyntaxHighlighter();

		// Python keywords
		public static readonly HashSet<string> PythonKeywords = new HashSet<string>
		{
			"if", "else", "elif", "while", "for", "def", "class", "return", "pass",
			"break", "continue", "import", "from", "as", "try", "except", "finally",
			"with", "lambda", "and", "or", "not", "in", "is", "True", "False", "None",
			"global", "nonlocal", "yield", "async", "await"
		};

		void Start()
		{
			InitializeEditor();
			Debug.Log(inputField.text.flat());
		}

		void Update()
		{
			// HandleSpecialInput();
		}

		private void InitializeEditor()
		{
			if (inputField == null)
				inputField = GetComponent<TMP_InputField>();

			if (inputField == null)
			{
				Debug.LogError("PythonCodeEditor requires a TMP_InputField component!");
				return;
			}

			// If no separate display text is assigned, create one
			if (displayText == null)
			{
				SetupOverlayDisplay();
			}

			// Configure input field
			inputField.lineType = TMP_InputField.LineType.MultiLineNewline;

			// Make input field text same color as display for proper caret positioning
			if (inputField.textComponent != null)
			{
				inputField.textComponent.color = defaultColor;
			}

			// Set up event handlers
			inputField.onValueChanged.AddListener(OnTextChanged);
			inputField.onSelect.AddListener(OnFieldSelected);

			// Initialize syntax highlighter with colors
			syntaxHighlighter.Initialize(keywordColor, stringColor, numberColor, commentColor, defaultColor);

			// Process initial text if any
			rawText = inputField.text;
			UpdateDisplayText();
		}

		private void ConfigureTabWidth()
		{
			// Set tab width on input field text component
			if (inputField.textComponent != null)
			{
				// inputField.textComponent.tabSize = tabSize;
			}

			// Set tab width on display text component
			if (displayText != null)
			{
				// displayText.tabSize = tabSize;
			}
		}

		private void SetupOverlayDisplay()
		{
			// Create overlay text component
			GameObject overlayGO = new GameObject("SyntaxHighlightOverlay");
			overlayGO.transform.SetParent(inputField.transform.NameStartsWith("text").NameStartsWith("text"), false);

			displayText = overlayGO.AddComponent<TextMeshProUGUI>();

			// Copy settings from input field
			var inputTextComponent = inputField.textComponent;
			// reduce alpha of inputTextCmponent
			inputTextComponent.color = new Color(1, 1, 1, 0.2f);

			displayText.font = inputTextComponent.font;
			displayText.fontSize = inputTextComponent.fontSize;
			displayText.color = defaultColor;
			displayText.fontStyle = inputTextComponent.fontStyle;
			displayText.alignment = inputTextComponent.alignment;
			displayText.enableWordWrapping = inputTextComponent.enableWordWrapping;

			// Position overlay exactly over input field text
			RectTransform overlayRect = displayText.GetComponent<RectTransform>();
			RectTransform inputRect = inputTextComponent.GetComponent<RectTransform>();

			overlayRect.anchorMin = inputRect.anchorMin;
			overlayRect.anchorMax = inputRect.anchorMax;
			overlayRect.offsetMin = inputRect.offsetMin;
			overlayRect.offsetMax = inputRect.offsetMax;
			overlayRect.sizeDelta = inputRect.sizeDelta;
			overlayRect.anchoredPosition = inputRect.anchoredPosition;

			// Ensure overlay doesn't block input but appears above input text
			displayText.raycastTarget = false;
			overlayGO.transform.SetAsLastSibling(); // Render on top

			// Hide the input field text by making it transparent
			var inputColor = inputTextComponent.color;
			inputColor.a = 0.01f; // Very transparent but not completely (for caret positioning)
			inputTextComponent.color = inputColor;
		}

		private void OnTextChanged(string newText)
		{
			if (isUpdatingText) return;

			// Store raw text and update display
			rawText = newText;
			UpdateDisplayText();
		}

		private void OnFieldSelected(string text)
		{
			// Ensure proper focus handling
		}

		private void UpdateDisplayText()
		{
			if (!enableSyntaxHighlighting || displayText == null)
			{
				if (displayText != null)
					displayText.text = rawText;
				return;
			}

			try
			{
				// Apply syntax highlighting to display text only
				string highlightedText = syntaxHighlighter.ProcessText(rawText);
				displayText.text = highlightedText;
			}
			catch (Exception ex)
			{
				Debug.LogError($"Error processing text: {ex.Message}");
				displayText.text = rawText; // Fallback to raw text
			}
		}

		private void HandleSpecialInput()
		{
			if (!inputField.isFocused) return;

			// Handle Tab key - completely override Unity's behavior
			if (Input.GetKeyDown(KeyCode.Tab))
			{
				// Prevent Unity from processing this tab
				Event.current?.Use();
				HandleTabInput();
			}

			// Handle Enter key for auto-indentation
			if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
			{
				if (enableAutoIndent)
				{
					StartCoroutine(HandleEnterInputDelayed());
				}
			}

			// Handle Ctrl+Backspace
			if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Backspace))
			{
				HandleCtrlBackspace();
			}
		}

		private void HandleTabInput()
		{
			isUpdatingText = true;

			try
			{
				int caretPos = inputField.caretPosition;

				// Insert actual tab character
				rawText = rawText.Insert(caretPos, "\t");
				inputField.text = rawText;
				inputField.caretPosition = caretPos + 1;

				UpdateDisplayText();
			}
			finally
			{
				isUpdatingText = false;
			}
		}

		private System.Collections.IEnumerator HandleEnterInputDelayed()
		{
			yield return null; // Wait one frame for Unity to process the enter

			HandleEnterInput();
		}

		private void HandleEnterInput()
		{
			isUpdatingText = true;

			try
			{
				int caretPos = inputField.caretPosition;

				// Find the start of the previous line (the line before the new line we just created)
				int prevLineStart = rawText.LastIndexOf('\n', caretPos - 2);
				if (prevLineStart == -1) prevLineStart = 0;
				else prevLineStart++; // Move past the \n

				// Find the end of the previous line
				int prevLineEnd = rawText.IndexOf('\n', prevLineStart);
				if (prevLineEnd == -1) prevLineEnd = caretPos - 1;
				else prevLineEnd = Mathf.Min(prevLineEnd, caretPos - 1);

				if (prevLineStart >= prevLineEnd) return; // No previous line content

				// Get the previous line content
				string prevLine = rawText.Substring(prevLineStart, prevLineEnd - prevLineStart);

				// Extract indentation from previous line (preserve exact whitespace characters)
				string indentation = "";
				for (int i = 0; i < prevLine.Length; i++)
				{
					if (prevLine[i] == ' ' || prevLine[i] == '\t')
						indentation += prevLine[i];
					else
						break;
				}

				// Check if the previous line ends with a colon (increase indentation)
				if (prevLine.TrimEnd().EndsWith(":"))
				{
					indentation += "\t"; // Add one tab character for increased indentation
				}

				// Insert indentation at current position
				rawText = rawText.Insert(caretPos, indentation);
				inputField.text = rawText;
				inputField.caretPosition = caretPos + indentation.Length;

				UpdateDisplayText();
			}
			finally
			{
				isUpdatingText = false;
			}
		}

		private void HandleCtrlBackspace()
		{
			isUpdatingText = true;

			try
			{
				int caretPos = inputField.caretPosition;

				if (caretPos == 0) return;

				// Find the start of the word to delete
				int deleteStart = caretPos - 1;

				// Skip whitespace
				while (deleteStart > 0 && char.IsWhiteSpace(rawText[deleteStart]))
					deleteStart--;

				// Skip the word
				while (deleteStart > 0 && !char.IsWhiteSpace(rawText[deleteStart - 1]))
					deleteStart--;

				// Delete the word and whitespace
				int deleteLength = caretPos - deleteStart;
				if (deleteLength > 0)
				{
					rawText = rawText.Remove(deleteStart, deleteLength);
					inputField.text = rawText;
					inputField.caretPosition = deleteStart;

					UpdateDisplayText();
				}
			}
			finally
			{
				isUpdatingText = false;
			}
		}

		/// <summary>
		/// Get the raw text without rich text formatting
		/// </summary>
		public string GetPlainText()
		{
			return rawText;
		}

		/// <summary>
		/// Set text content programmatically
		/// </summary>
		public void SetText(string text)
		{
			isUpdatingText = true;

			try
			{
				rawText = text;
				inputField.text = rawText;
				UpdateDisplayText();
			}
			finally
			{
				isUpdatingText = false;
			}
		}

		/// <summary>
		/// Toggle syntax highlighting on/off
		/// </summary>
		public void SetSyntaxHighlighting(bool enabled)
		{
			enableSyntaxHighlighting = enabled;
			UpdateDisplayText();
		}
	}

	/// <summary>
	/// Handles syntax highlighting with performance optimizations
	/// </summary>
	public class SyntaxHighlighter
	{
		private Color keywordColor;
		private Color stringColor;
		private Color numberColor;
		private Color commentColor;
		private Color defaultColor;

		// Regex patterns for syntax elements
		private Regex keywordRegex;
		private Regex stringRegex;
		private Regex numberRegex;
		private Regex commentRegex;

		// Rich text color codes
		private string keywordColorCode;
		private string stringColorCode;
		private string numberColorCode;
		private string commentColorCode;

		public void Initialize(Color keyword, Color stringCol, Color number, Color comment, Color defaultCol)
		{
			keywordColor = keyword;
			stringColor = stringCol;
			numberColor = number;
			commentColor = comment;
			defaultColor = defaultCol;

			// Convert colors to hex codes
			keywordColorCode = ColorToHex(keywordColor);
			stringColorCode = ColorToHex(stringColor);
			numberColorCode = ColorToHex(numberColor);
			commentColorCode = ColorToHex(commentColor);

			// Compile regex patterns
			string keywordPattern = @"\b(" + string.Join("|", PythonCodeEditorSyntaxHighlight.PythonKeywords) + @")\b";
			keywordRegex = new Regex(keywordPattern, RegexOptions.Compiled);

			stringRegex = new Regex(@"(""[^""]*""|'[^']*')", RegexOptions.Compiled);
			numberRegex = new Regex(@"\b\d+(?:\.\d+)?\b", RegexOptions.Compiled);
			commentRegex = new Regex(@"#.*$", RegexOptions.Compiled | RegexOptions.Multiline);
		}

		public string ProcessText(string text)
		{
			if (string.IsNullOrEmpty(text)) return text;

			var result = new StringBuilder(text.Length * 2); // Pre-allocate for performance

			// Process line by line for better performance with large files
			string[] lines = text.Split('\n');

			for (int i = 0; i < lines.Length; i++)
			{
				if (i > 0) result.Append('\n');
				result.Append(ProcessLine(lines[i]));
			}

			return result.ToString();
		}

		private string ProcessLine(string line)
		{
			if (string.IsNullOrEmpty(line)) return line;

			var segments = new List<TextSegment>();

			// Find comments first (they override everything else)
			var commentMatches = commentRegex.Matches(line);

			// If there's a comment, split the line
			if (commentMatches.Count > 0)
			{
				var commentMatch = commentMatches[0];
				string beforeComment = line.Substring(0, commentMatch.Index);
				string comment = commentMatch.Value;

				// Process the part before comment
				if (!string.IsNullOrEmpty(beforeComment))
				{
					segments.AddRange(ProcessSegments(beforeComment));
				}

				// Add comment segment
				segments.Add(new TextSegment(commentMatch.Index, comment.Length, commentColorCode));

				return ApplySegments(line, segments);
			}

			// No comments, process normally
			segments.AddRange(ProcessSegments(line));
			return ApplySegments(line, segments);
		}

		private List<TextSegment> ProcessSegments(string text)
		{
			var segments = new List<TextSegment>();

			// Find strings first (they have priority)
			var stringMatches = stringRegex.Matches(text);
			var stringRanges = new List<(int start, int end)>();

			foreach (Match match in stringMatches)
			{
				segments.Add(new TextSegment(match.Index, match.Length, stringColorCode));
				stringRanges.Add((match.Index, match.Index + match.Length));
			}

			// Find keywords (avoiding strings)
			var keywordMatches = keywordRegex.Matches(text);
			foreach (Match match in keywordMatches)
			{
				if (!IsInStringRange(match.Index, stringRanges))
				{
					segments.Add(new TextSegment(match.Index, match.Length, keywordColorCode));
				}
			}

			// Find numbers (avoiding strings)
			var numberMatches = numberRegex.Matches(text);
			foreach (Match match in numberMatches)
			{
				if (!IsInStringRange(match.Index, stringRanges))
				{
					segments.Add(new TextSegment(match.Index, match.Length, numberColorCode));
				}
			}

			return segments;
		}

		private bool IsInStringRange(int position, List<(int start, int end)> stringRanges)
		{
			foreach (var range in stringRanges)
			{
				if (position >= range.start && position < range.end)
					return true;
			}
			return false;
		}

		private string ApplySegments(string originalText, List<TextSegment> segments)
		{
			if (segments.Count == 0) return originalText;

			// Sort segments by position
			segments.Sort((a, b) => a.Start.CompareTo(b.Start));

			var result = new StringBuilder(originalText.Length * 2);
			int currentPos = 0;

			foreach (var segment in segments)
			{
				// Add text before this segment
				if (segment.Start > currentPos)
				{
					result.Append(originalText.Substring(currentPos, segment.Start - currentPos));
				}

				// Add colored segment
				result.Append($"<color={segment.ColorCode}>");
				result.Append(originalText.Substring(segment.Start, segment.Length));
				result.Append("</color>");

				currentPos = segment.Start + segment.Length;
			}

			// Add remaining text
			if (currentPos < originalText.Length)
			{
				result.Append(originalText.Substring(currentPos));
			}

			return result.ToString();
		}

		private string ColorToHex(Color color)
		{
			return $"#{ColorUtility.ToHtmlStringRGB(color)}";
		}

		private struct TextSegment
		{
			public int Start;
			public int Length;
			public string ColorCode;

			public TextSegment(int start, int length, string colorCode)
			{
				Start = start;
				Length = length;
				ColorCode = colorCode;
			}
		}
	}
}
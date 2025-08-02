using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

using SPACE_UTIL;

namespace GptDeepResearch
{
	/// <summary>
	/// Professional Python code editor with syntax highlighting, auto-indentation, and smart editing features
	/// </summary>

	// USAGE NOTE: 
	// Call RefreshGameCommands() whenever:
	// 1. Scene changes and new GameController is registered
	// 2. GameController adds/removes commands dynamically
	// Example: GameControllerBase.Start() should call this method
	public class PythonCodeEditorSyntaxHighlight : MonoBehaviour
	{
		// LABELED DIFF FOR PythonCodeEditorSyntaxHighlight.cs
		// Add game command highlighting functionality

		[Header("Components")]
		[SerializeField] private TMP_InputField inputField;
		[SerializeField] private TextMeshProUGUI displayText; // Separate display component

		// LABELED DIFF FOR PythonCodeEditorSyntaxHighlight.cs
		// Update syntax highlighting with new color scheme and built-in function detection

		// REPLACE the [Header("Syntax Colors")] section (around line 20) with:
		[Header("Syntax Colors")]
		[SerializeField] private Color keywordColor = new Color(198f / 255f, 120f / 255f, 221f / 255f); // rgb(198,120,221)
		[SerializeField] private Color stringColor = new Color(152f / 255f, 195f / 255f, 121f / 255f); // rgb(152,195,121)
		[SerializeField] private Color numberColor = new Color(209f / 255f, 154f / 255f, 102f / 255f); // rgb(209,154,102)
		[SerializeField] private Color commentColor = new Color(92f / 255f, 99f / 255f, 112f / 255f); // rgb(92,99,112)
		[SerializeField] private Color operatorColor = new Color(97f / 255f, 175f / 255f, 239f / 255f); // rgb(97,175,239)
																										// ADD: New colors for built-ins and game commands
		[SerializeField] private Color builtinFunctionColor = new Color(224f / 255f, 108f / 255f, 117f / 255f); // rgb(224,108,117)
		[SerializeField] private Color gameCommandColor = new Color(224f / 255f, 108f / 255f, 117f / 255f); // rgb(224,108,117)
		[SerializeField] private Color defaultColor = new Color(0.8f, 0.8f, 0.8f, 1f);


		[Header("Editor Settings")]
		[SerializeField] private int tabSize = 4;
		[SerializeField] private bool enableSyntaxHighlighting = true;
		[SerializeField] private bool enableAutoIndent = true;

		// Raw text storage (without rich text tags)
		[Header("handled inside")]
		[SerializeField] string rawText = "";
		private bool isUpdatingText = false;

		// Syntax highlighting
		private readonly SyntaxHighlighter syntaxHighlighter = new SyntaxHighlighter();

		// MODIFY the Python keywords set to remove print (around line 50):
		public static readonly HashSet<string> PythonKeywords = new HashSet<string>
		{
			"if", "else", "elif", "while", "for", "def", "class",
			"return", "pass",
			"break", "continue",
			"import",
			"and", "or", "not",
			"in", "is",
			"True", "False",
			"None",
			"global",
			// REMOVE "print" from here - it's now in PythonBuiltins
		};

		// ADD new Python built-in functions set (around line 40):
		public static readonly HashSet<string> PythonBuiltins = new HashSet<string>
		{
			"len", "sleep", "range", "print"
		};

		void Start()
		{
			InitializeEditor();
			// ConfigureInputFieldWrapping();
			Debug.Log(this.gameObject);
			Debug.Log(inputField.text.flat());

			/* not a desirable behaviour when word wrap for an innput field is disabled
			var textComponent = this.gameObject.GC<TMP_InputField>().textComponent;
			textComponent.enableWordWrapping = false;
			textComponent.overflowMode = TextOverflowModes.Overflow;
			*/
		}

		void Update()
		{
			// HandleSpecialInput();
		}

		// subscription to when inputfield alters
		// MODIFY InitializeEditor method (around line 100):
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

			// MODIFY: Initialize syntax highlighter with new colors
			syntaxHighlighter.Initialize(keywordColor, stringColor, numberColor, commentColor,
									   operatorColor, builtinFunctionColor, gameCommandColor, defaultColor);

			// Process initial text if any
			rawText = inputField.text;
			UpdateDisplayText();
		}


		// ADD: Public method to refresh game commands (call this when scene changes)

		/// <summary>
		/// Refresh game command highlighting when scene controller changes
		/// Call this method when switching scenes or when game commands change
		/// </summary>
		public void RefreshGameCommands()
		{
			if (syntaxHighlighter != null)
			{
				syntaxHighlighter.UpdateGameCommandRegex();
				UpdateDisplayText(); // Re-process text with new commands
			}
		}

		void ConfigureInputFieldWrapping()
		{
			TMP_InputField codeInputField = GetComponent<TMP_InputField>();
			if (codeInputField == null) return;

			// Disable word wrapping
			codeInputField.lineType = TMP_InputField.LineType.MultiLineNewline;

			// Configure text component for no wrapping
			var textComponent = codeInputField.textComponent;
			textComponent.enableWordWrapping = false;
			textComponent.overflowMode = TextOverflowModes.Overflow;

			// Enable horizontal scrolling

			var scrollRect = codeInputField.GetComponent<ScrollRect>();
			if (scrollRect == null)
			{
				// InputField handles its own scrolling, but we can configure it
				textComponent.rectTransform.anchorMin = new Vector2(0, 0);
				textComponent.rectTransform.anchorMax = new Vector2(0, 1);
				textComponent.rectTransform.pivot = new Vector2(0, 1);
			}

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

		#region Syntax Highlight, Twin text element approach
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

		// ADD new fields to SyntaxHighlighter class (around line 165):
		private Color gameCommandColor;
		private string gameCommandColorCode;
		private Regex gameCommandRegex;

		// ADD new fields to SyntaxHighlighter class (around line 180):
		private Color operatorColor;
		private Color builtinFunctionColor;
		private string operatorColorCode;
		private string builtinFunctionColorCode;
		private Regex operatorRegex;
		private Regex builtinFunctionRegex;



		// MODIFY SyntaxHighlighter class Initialize method (around line 200):
		public void Initialize(Color keyword, Color stringCol, Color number, Color comment,
							  Color operatorCol, Color builtinFunc, Color gameCommand, Color defaultCol)
		{
			keywordColor = keyword;
			stringColor = stringCol;
			numberColor = number;
			commentColor = comment;
			operatorColor = operatorCol; // ADD this line
			builtinFunctionColor = builtinFunc; // ADD this line
			gameCommandColor = gameCommand;
			defaultColor = defaultCol;

			// Convert colors to hex codes
			keywordColorCode = ColorToHex(keywordColor);
			stringColorCode = ColorToHex(stringColor);
			numberColorCode = ColorToHex(numberColor);
			commentColorCode = ColorToHex(commentColor);
			operatorColorCode = ColorToHex(operatorColor); // ADD this line
			builtinFunctionColorCode = ColorToHex(builtinFunctionColor); // ADD this line
			gameCommandColorCode = ColorToHex(gameCommandColor);

			// Compile regex patterns
			string keywordPattern = @"\b(" + string.Join("|", PythonCodeEditorSyntaxHighlight.PythonKeywords) + @")\b";
			keywordRegex = new Regex(keywordPattern, RegexOptions.Compiled);

			// ADD: Built-in function regex
			string builtinPattern = @"\b(" + string.Join("|", PythonCodeEditorSyntaxHighlight.PythonBuiltins) + @")\b";
			builtinFunctionRegex = new Regex(builtinPattern, RegexOptions.Compiled);

			// Game command regex (will be updated dynamically)
			UpdateGameCommandRegex();

			stringRegex = new Regex(@"(""[^""]*""|'[^']*')", RegexOptions.Compiled);
			numberRegex = new Regex(@"\b\d+(?:\.\d+)?\b", RegexOptions.Compiled);
			commentRegex = new Regex(@"#.*$", RegexOptions.Compiled | RegexOptions.Multiline);
			// ADD: Operator regex
			operatorRegex = new Regex(@"[+\-*/=<>!&|%]+|==|!=|<=|>=|and\b|or\b|not\b", RegexOptions.Compiled);
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


		// MODIFY ProcessSegments method in SyntaxHighlighter class (around line 280):
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

			// ADD: Find built-in functions (avoiding strings and keywords)
			if (builtinFunctionRegex != null)
			{
				var builtinMatches = builtinFunctionRegex.Matches(text);
				foreach (Match match in builtinMatches)
				{
					if (!IsInStringRange(match.Index, stringRanges) && !IsInKeywordRange(match.Index, keywordMatches))
					{
						segments.Add(new TextSegment(match.Index, match.Length, builtinFunctionColorCode));
					}
				}
			}

			// Find game commands (avoiding strings, keywords, and built-ins)
			if (gameCommandRegex != null)
			{
				var gameCommandMatches = gameCommandRegex.Matches(text);
				foreach (Match match in gameCommandMatches)
				{
					if (!IsInStringRange(match.Index, stringRanges) &&
						!IsInKeywordRange(match.Index, keywordMatches) &&
						!IsInBuiltinRange(match.Index, builtinFunctionRegex?.Matches(text)))
					{
						segments.Add(new TextSegment(match.Index, match.Length, gameCommandColorCode));
					}
				}
			}

			// ADD: Find operators (avoiding strings)
			if (operatorRegex != null)
			{
				var operatorMatches = operatorRegex.Matches(text);
				foreach (Match match in operatorMatches)
				{
					if (!IsInStringRange(match.Index, stringRanges))
					{
						segments.Add(new TextSegment(match.Index, match.Length, operatorColorCode));
					}
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

		// ADD helper method for built-in range checking:
		private bool IsInBuiltinRange(int position, MatchCollection builtinMatches)
		{
			if (builtinMatches == null) return false;

			foreach (Match match in builtinMatches)
			{
				if (position >= match.Index && position < match.Index + match.Length)
					return true;
			}
			return false;
		}


		// ADD: Helper method to check keyword ranges
		private bool IsInKeywordRange(int position, MatchCollection keywordMatches)
		{
			foreach (Match match in keywordMatches)
			{
				if (position >= match.Index && position < match.Index + match.Length)
					return true;
			}
			return false;
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


		// ADD new methods to SyntaxHighlighter class:

		/// <summary>
		/// Update game command regex based on current scene's available commands
		/// </summary>
		public void UpdateGameCommandRegex()
		{
			var gameCommands = GetSceneCommands();
			if (gameCommands.Count > 0)
			{
				string gameCommandPattern = @"\b(" + string.Join("|", gameCommands) + @")\b";
				gameCommandRegex = new Regex(gameCommandPattern, RegexOptions.Compiled);
			}
			else
			{
				gameCommandRegex = null;
			}
		}

		/// <summary>
		/// Get available game commands from current scene
		/// </summary>
		private List<string> GetSceneCommands()
		{
			try
			{
				return GameBuiltinMethods.GetAllAvailableCommands();
			}
			catch
			{
				return new List<string>();
			}
		}
	} 
	#endregion
}
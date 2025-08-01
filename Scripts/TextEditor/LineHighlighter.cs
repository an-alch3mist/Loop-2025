using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace GptDeepResearch
{
	public class LineHighlighter : MonoBehaviour
	{
		[Header("UI References")]
		public TMP_InputField codeInputField;
		public Image highlightOverlay; // Semi-transparent overlay for highlighting
		public RectTransform highlightBar; // A colored bar to show current line

		[Header("Highlight Settings")]
		public Color highlightColor = new Color(1f, 1f, 0f, 0.3f); // Yellow with transparency
		public float highlightHeight = 20f; // Height of one line

		private int currentHighlightedLine = -1;
		private List<string> codeLines = new List<string>();

		void Start()
		{
			if (highlightOverlay != null)
				highlightOverlay.gameObject.SetActive(false);

			if (highlightBar != null)
				highlightBar.gameObject.SetActive(false);
		}

		public void UpdateCodeLines(string code)
		{
			codeLines.Clear();
			if (!string.IsNullOrEmpty(code))
			{
				codeLines.AddRange(code.Split('\n'));
			}
		}

		public void HighlightLine(int lineNumber)
		{
			if (lineNumber <= 0 || lineNumber > codeLines.Count)
			{
				HideHighlight();
				return;
			}

			currentHighlightedLine = lineNumber;

			if (highlightBar != null)
			{
				highlightBar.gameObject.SetActive(true);

				// Calculate position based on line number
				float yOffset = CalculateLineYOffset(lineNumber);
				Vector3 pos = highlightBar.localPosition;
				pos.y = yOffset;
				highlightBar.localPosition = pos;

				// Set size
				Vector2 size = highlightBar.sizeDelta;
				size.y = highlightHeight;
				highlightBar.sizeDelta = size;

				// Set color
				Image barImage = highlightBar.GetComponent<Image>();
				if (barImage != null)
					barImage.color = highlightColor;
			}
		}

		public void HideHighlight()
		{
			currentHighlightedLine = -1;

			if (highlightOverlay != null)
				highlightOverlay.gameObject.SetActive(false);

			if (highlightBar != null)
				highlightBar.gameObject.SetActive(false);
		}

		private float CalculateLineYOffset(int lineNumber)
		{
			// This calculation depends on your text component's line height
			// You may need to adjust this based on your font size and line spacing
			TMP_Text textComponent = codeInputField.textComponent;
			if (textComponent != null)
			{
				float lineHeight = textComponent.fontSize + textComponent.lineSpacing;
				return -(lineNumber - 1) * lineHeight;
			}

			return -(lineNumber - 1) * highlightHeight;
		}

		// Call this method to animate the highlight (optional)
		public void AnimateHighlight()
		{
			if (highlightBar != null && highlightBar.gameObject.activeInHierarchy)
			{
				// Simple pulse animation
				LeanTween.scale(highlightBar.gameObject, Vector3.one * 1.1f, 0.2f)
					.setEaseInOutSine()
					.setLoopPingPong(1);
			}
		}
	}

	// Extension to ScriptRunner to support line highlighting
	public class ExecutionTracker
	{
		public static System.Action<int> OnLineExecuted;

		public static void NotifyLineExecution(int lineNumber)
		{
			OnLineExecuted?.Invoke(lineNumber);
		}
	}
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GptDeepResearch
{
	public class CodeEditorWithLineNumbers : MonoBehaviour
	{
		[Header("UI References")]
		public TMP_InputField codeInputField;
		public TMP_Text lineNumbersText;
		public ScrollRect scrollRect;

		private string previousText = "";
		private int previousLineCount = 0;

		void Start()
		{
			if (codeInputField != null)
			{
				codeInputField.onValueChanged.AddListener(OnTextChanged);
				// Initialize with line 1
				UpdateLineNumbers();
			}
		}

		void OnTextChanged(string newText)
		{
			if (newText != previousText)
			{
				UpdateLineNumbers();
				previousText = newText;
			}
		}

		void UpdateLineNumbers()
		{
			if (codeInputField == null || lineNumbersText == null) return;

			string text = codeInputField.text;
			int lineCount = text.Split('\n').Length;

			// Only update if line count changed (performance optimization)
			if (lineCount != previousLineCount)
			{
				System.Text.StringBuilder lineNumbers = new System.Text.StringBuilder();
				for (int i = 1; i <= lineCount; i++)
				{
					lineNumbers.AppendLine(i.ToString());
				}

				lineNumbersText.text = lineNumbers.ToString().TrimEnd('\n');
				previousLineCount = lineCount;
			}

			// Sync scroll position
			SyncScrollPosition();
		}

		void SyncScrollPosition()
		{
			if (scrollRect != null)
			{
				// This ensures line numbers scroll with the code
				Canvas.ForceUpdateCanvases();
			}
		}

		void Update()
		{
			// Continuously sync scroll in case user scrolls
			if (scrollRect != null)
			{
				SyncScrollPosition();
			}
		}
	}
}
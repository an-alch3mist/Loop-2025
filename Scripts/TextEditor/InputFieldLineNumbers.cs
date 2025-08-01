using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text;
using System.Collections;


namespace GptDeepResearch
{
	public class InputFieldLineNumbers : MonoBehaviour
	{
		[Header("Components")]
		public TMP_InputField inputField;
		public TextMeshProUGUI lineNumbersText;
		public ScrollRect lineNumbersScrollRect;

		[Header("Settings")]
		public int maxLineNumbers = 1000;
		public string lineNumberFormat = "{0:D3}"; // 001, 002, etc.
		public float syncUpdateInterval = 0.02f; // Update frequency for scroll sync

		private int previousLineCount = 0;
		private StringBuilder lineNumbersBuilder = new StringBuilder();
		private RectTransform inputFieldTextArea;
		private TMP_Text inputFieldTextComponent;
		private Coroutine scrollSyncCoroutine;

		void Start()
		{
			if (inputField == null)
				inputField = GetComponent<TMP_InputField>();

			// Get InputField's text area and text component
			if (inputField != null)
			{
				inputFieldTextArea = inputField.textViewport;
				inputFieldTextComponent = inputField.textComponent;

				// Subscribe to input field events
				inputField.onValueChanged.AddListener(OnInputFieldChanged);
			}

			// Initialize line numbers
			UpdateLineNumbers();

			// Start scroll synchronization coroutine
			if (scrollSyncCoroutine != null)
				StopCoroutine(scrollSyncCoroutine);
			scrollSyncCoroutine = StartCoroutine(SynchronizeScrolling());
		}

		void OnDestroy()
		{
			if (inputField != null)
			{
				inputField.onValueChanged.RemoveListener(OnInputFieldChanged);
			}

			if (scrollSyncCoroutine != null)
			{
				StopCoroutine(scrollSyncCoroutine);
			}
		}

		void OnInputFieldChanged(string text)
		{
			UpdateLineNumbers();
		}

		void UpdateLineNumbers()
		{
			if (inputField == null || lineNumbersText == null) return;

			string text = inputField.text;
			int lineCount = GetLineCount(text);

			// Only update if line count changed
			if (lineCount != previousLineCount)
			{
				lineNumbersBuilder.Clear();

				for (int i = 1; i <= lineCount && i <= maxLineNumbers; i++)
				{
					if (i > 1) lineNumbersBuilder.AppendLine();
					lineNumbersBuilder.Append(string.Format(lineNumberFormat, i));
				}

				lineNumbersText.text = lineNumbersBuilder.ToString();
				previousLineCount = lineCount;

				// Force layout rebuild
				LayoutRebuilder.ForceRebuildLayoutImmediate(lineNumbersText.rectTransform);
			}
		}

		IEnumerator SynchronizeScrolling()
		{
			while (true)
			{
				yield return new WaitForSeconds(syncUpdateInterval);

				if (inputFieldTextComponent != null && lineNumbersScrollRect != null && inputFieldTextArea != null)
				{
					// Calculate the scroll position based on InputField's text component position
					float inputFieldHeight = inputFieldTextArea.rect.height;
					float textHeight = inputFieldTextComponent.preferredHeight;

					if (textHeight > inputFieldHeight)
					{
						// Calculate normalized position (0 = top, 1 = bottom)
						float textYPosition = inputFieldTextComponent.rectTransform.anchoredPosition.y;
						float maxScroll = textHeight - inputFieldHeight;
						float normalizedPosition = Mathf.Clamp01(textYPosition / maxScroll);

						// Apply to line numbers scroll rect (inverted because UI coordinates)
						lineNumbersScrollRect.verticalNormalizedPosition = 1f - normalizedPosition;
					}
					else
					{
						// Reset to top when text fits entirely
						lineNumbersScrollRect.verticalNormalizedPosition = 1f;
					}
				}
			}
		}

		int GetLineCount(string text)
		{
			if (string.IsNullOrEmpty(text))
				return 1;

			int lineCount = 1;
			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] == '\n')
					lineCount++;
			}
			return lineCount;
		}

		// Alternative method using TextInfo for more accurate line counting
		int GetVisualLineCount()
		{
			if (inputFieldTextComponent == null) return 1;

			// Force text generation
			inputFieldTextComponent.ForceMeshUpdate();

			// Get text info
			TMP_TextInfo textInfo = inputFieldTextComponent.textInfo;
			return textInfo.lineCount;
		}

		// Method to manually force synchronization
		public void ForceSyncScroll()
		{
			if (scrollSyncCoroutine != null)
				StopCoroutine(scrollSyncCoroutine);
			scrollSyncCoroutine = StartCoroutine(SynchronizeScrolling());
		}

		// Method to set update interval
		public void SetSyncUpdateInterval(float interval)
		{
			syncUpdateInterval = interval;
			ForceSyncScroll();
		}
	}

	// Alternative approach using Unity Events (more performance friendly)
	[System.Serializable]
	public class InputFieldScrollTracker : MonoBehaviour
	{
		[Header("Components")]
		public TMP_InputField inputField;
		public ScrollRect lineNumbersScrollRect;

		private Vector2 lastTextPosition;
		private RectTransform inputTextTransform;

		void Start()
		{
			if (inputField != null)
			{
				inputTextTransform = inputField.textComponent.rectTransform;
				lastTextPosition = inputTextTransform.anchoredPosition;
			}
		}

		void LateUpdate()
		{
			if (inputTextTransform != null && lineNumbersScrollRect != null)
			{
				Vector2 currentTextPosition = inputTextTransform.anchoredPosition;

				// Only update if position changed
				if (currentTextPosition != lastTextPosition)
				{
					SyncScrollPosition();
					lastTextPosition = currentTextPosition;
				}
			}
		}

		void SyncScrollPosition()
		{
			if (inputField == null || lineNumbersScrollRect == null) return;

			RectTransform textArea = inputField.textViewport;
			var textComponent = inputField.textComponent;

			float viewportHeight = textArea.rect.height;
			float contentHeight = textComponent.preferredHeight;

			if (contentHeight > viewportHeight)
			{
				float scrollOffset = textComponent.rectTransform.anchoredPosition.y;
				float maxScroll = contentHeight - viewportHeight;
				float normalizedPosition = 1f - Mathf.Clamp01(scrollOffset / maxScroll);

				lineNumbersScrollRect.verticalNormalizedPosition = normalizedPosition;
			}
			else
			{
				lineNumbersScrollRect.verticalNormalizedPosition = 1f;
			}
		}
	} 
}
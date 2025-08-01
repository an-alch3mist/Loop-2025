using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SPACE_UTIL;
using System;

// works only on left mouse click event
public class ADragableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	[SerializeField] RectTransform entireWindowRect;

	Vector2 delta;
	Transform entireWindowParentBeforeDrag;
	public void OnBeginDrag(PointerEventData eventData)
	{
		Debug.Log("OnBeginDrag");
		if (this.entireWindowRect == null)
			this.entireWindowRect = this.gameObject.GC<RectTransform>();

		this.entireWindowParentBeforeDrag = this.entireWindowRect.parent;
		this.entireWindowRect.SetParent(this.entireWindowRect.root);
		this.entireWindowRect.SetAsLastSibling();

		this.delta = entireWindowRect.anchoredPosition - INPUT.UI.pos;
		Vector2 targetPos = INPUT.UI.pos + this.delta;
		// restriction within bounds >>

		// << restriction within bounds
		entireWindowRect.anchoredPosition = targetPos;

		//throw new System.NotImplementedException();
	}

	public void OnDrag(PointerEventData eventData)
	{
		Debug.Log("OnDrag");
		// this.delta = WindowRect.anchoredPosition - INPUT.UI.pos;
		Vector2 targetPos = INPUT.UI.pos + this.delta;
		// restriction within bounds >>

		// << restriction within bounds
		entireWindowRect.anchoredPosition = targetPos;
		//throw new System.NotImplementedException();
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		Debug.Log("OnEndDrag");
		// this.delta = WindowRect.anchoredPosition - INPUT.UI.pos;
		Vector2 targetPos = INPUT.UI.pos + this.delta;
		// restriction within bounds >>

		// << restriction within bounds
		entireWindowRect.anchoredPosition = targetPos;

		this.entireWindowRect.parent = this.entireWindowParentBeforeDrag;
		//throw new System.NotImplementedException();
	}


}



// Works with custom mouse buttons (Left, Right, Middle)
public class DragableItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
	[Header("Mouse Button Settings")]
	public bool useMiddleMouseButton = true;
	public bool useLeftMouseButton = true;
	public bool useRightMouseButton = true;

	[SerializeField] RectTransform entireWindowRect;

	private Vector2 delta;
	private Transform entireWindowParentBeforeDrag;
	private bool isDragging = false;
	private bool isMouseOver = false;
	private int dragButton = -1; // Which button started the drag (0=Left, 1=Right, 2=Middle)

	void Update()
	{
		if (isDragging)
		{
			// Continue dragging while button is held
			bool buttonStillDown = false;

			if (dragButton == 0 && Input.GetMouseButton(0)) buttonStillDown = true;
			else if (dragButton == 1 && Input.GetMouseButton(1)) buttonStillDown = true;
			else if (dragButton == 2 && Input.GetMouseButton(2)) buttonStillDown = true;

			if (buttonStillDown)
			{
				OnDrag();
			}
			else
			{
				OnEndDrag();
			}
		}
		else if (isMouseOver)
		{
			// Check for drag start with custom mouse buttons
			if (useMiddleMouseButton && Input.GetMouseButtonDown(2))
			{
				OnBeginDrag(2);
			}
			else if (useRightMouseButton && Input.GetMouseButtonDown(1))
			{
				OnBeginDrag(1);
			}
			else if (useLeftMouseButton && Input.GetMouseButtonDown(0))
			{
				OnBeginDrag(0);
			}
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		// Handle left mouse button through EventSystem if enabled
		if (useLeftMouseButton && eventData.button == PointerEventData.InputButton.Left)
		{
			OnBeginDrag(0);
		}
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		// Handle mouse up through EventSystem
		if (isDragging &&
			((eventData.button == PointerEventData.InputButton.Left && dragButton == 0) ||
			 (eventData.button == PointerEventData.InputButton.Right && dragButton == 1) ||
			 (eventData.button == PointerEventData.InputButton.Middle && dragButton == 2)))
		{
			OnEndDrag();
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		isMouseOver = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		isMouseOver = false;
	}

	// Modified version of your original OnBeginDrag
	public void OnBeginDrag(int buttonIndex)
	{
		if (isDragging) return;

		Debug.Log($"OnBeginDrag with button {buttonIndex} (0=Left, 1=Right, 2=Middle)");
		isDragging = true;
		dragButton = buttonIndex;

		if (this.entireWindowRect == null)
		{
			this.entireWindowRect = this.gameObject.GC<RectTransform>();
		}

		this.entireWindowParentBeforeDrag = this.entireWindowRect.parent;
		this.entireWindowRect.SetParent(this.entireWindowRect.root);
		this.entireWindowRect.SetAsLastSibling();

		this.delta = entireWindowRect.anchoredPosition - INPUT.UI.pos;
		Vector2 targetPos = INPUT.UI.pos + this.delta;

		// restriction within bounds >>
		// << restriction within bounds

		entireWindowRect.anchoredPosition = targetPos;
	}

	// Modified version of your original OnDrag
	public void OnDrag()
	{
		if (!isDragging) return;

		Debug.Log("OnDrag");
		Vector2 targetPos = INPUT.UI.pos + this.delta;

		// restriction within bounds >>
		// << restriction within bounds

		entireWindowRect.anchoredPosition = targetPos;
	}

	// Modified version of your original OnEndDrag
	public void OnEndDrag()
	{
		if (!isDragging) return;

		Debug.Log("OnEndDrag");
		Vector2 targetPos = INPUT.UI.pos + this.delta;

		// restriction within bounds >>
		// << restriction within bounds

		entireWindowRect.anchoredPosition = targetPos;
		this.entireWindowRect.parent = this.entireWindowParentBeforeDrag;

		isDragging = false;
		dragButton = -1;
	}

	// Legacy method overloads for compatibility (in case other code calls these)
	public void OnBeginDrag(PointerEventData eventData)
	{
		if (useLeftMouseButton && eventData.button == PointerEventData.InputButton.Left)
		{
			OnBeginDrag(0);
		}
	}

	public void OnDrag(PointerEventData eventData)
	{
		// This will be handled by the Update method
		// keeping this method for interface compatibility
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (isDragging && eventData.button == PointerEventData.InputButton.Left && dragButton == 0)
		{
			OnEndDrag();
		}
	}

	// Public utility methods
	public bool IsDragging()
	{
		return isDragging;
	}

	public int GetDragButton()
	{
		return dragButton;
	}

	public void SetDragButtons(bool middle, bool left, bool right)
	{
		useMiddleMouseButton = middle;
		useLeftMouseButton = left;
		useRightMouseButton = right;
	}

	// Method to manually set the window rect if needed
	public void SetEntireWindowRect(RectTransform windowRect)
	{
		this.entireWindowRect = windowRect;
	}
}
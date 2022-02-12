using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleFileBrowser
{
	public class FileBrowserMovement : MonoBehaviour
	{
		#region Variables
#pragma warning disable 0649
		private FileBrowser fileBrowser;
		private RectTransform canvasTR;
		private Camera canvasCam;

		[SerializeField]
		private RectTransform window;

		[SerializeField]
		private RecycledListView listView;
#pragma warning restore 0649

		private Vector2 initialTouchPos = Vector2.zero;
		private Vector2 initialAnchoredPos, initialSizeDelta;
		#endregion

		#region Initialization Functions
		public void Initialize(FileBrowser fileBrowser)
		{
			this.fileBrowser = fileBrowser;
			canvasTR = fileBrowser.GetComponent<RectTransform>();
		}
		#endregion

		#region Pointer Events
		public void OnDragStarted(BaseEventData data)
		{
			PointerEventData pointer = (PointerEventData)data;

			canvasCam = pointer.pressEventCamera;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(window, pointer.pressPosition, canvasCam, out initialTouchPos);
		}

		public void OnDrag(BaseEventData data)
		{
			PointerEventData pointer = (PointerEventData)data;

			Vector2 touchPos;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(window, pointer.position, canvasCam, out touchPos);
			window.anchoredPosition += touchPos - initialTouchPos;
		}

		public void OnEndDrag(BaseEventData data)
		{
			fileBrowser.EnsureWindowIsWithinBounds();
		}
		public void OnEndDrag2(BaseEventData data)
		{
			Vector2 canvasSize = new Vector2(Screen.width, Screen.height);
			Vector2 windowSize = ((RectTransform)transform).sizeDelta;

			if (windowSize.x > canvasSize.x)
				windowSize.x = canvasSize.x;
			if (windowSize.y > canvasSize.y)
				windowSize.y = canvasSize.y;

			Vector2 windowPos = ((RectTransform)transform).anchoredPosition;
			Vector2 canvasHalfSize = canvasSize * 0.5f;
			Vector2 windowHalfSize = windowSize * 0.5f;
			Vector2 windowBottomLeft = windowPos - windowHalfSize + canvasHalfSize;
			Vector2 windowTopRight = windowPos + windowHalfSize + canvasHalfSize;

			if (windowBottomLeft.x < 0f)
				windowPos.x -= windowBottomLeft.x;
			else if (windowTopRight.x > canvasSize.x)
				windowPos.x -= windowTopRight.x - canvasSize.x;

			if (windowBottomLeft.y < 0f)
				windowPos.y -= windowBottomLeft.y;
			else if (windowTopRight.y > canvasSize.y)
				windowPos.y -= windowTopRight.y - canvasSize.y;

			((RectTransform)transform).anchoredPosition = windowPos;
			((RectTransform)transform).sizeDelta = windowSize;
		}

		public void OnResizeStarted(BaseEventData data)
		{
			PointerEventData pointer = (PointerEventData)data;

			canvasCam = pointer.pressEventCamera;
			initialAnchoredPos = window.anchoredPosition;
			initialSizeDelta = window.sizeDelta;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasTR, pointer.pressPosition, canvasCam, out initialTouchPos);
		}

		public void OnResize(BaseEventData data)
		{
			PointerEventData pointer = (PointerEventData)data;

			Vector2 touchPos;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasTR, pointer.position, canvasCam, out touchPos);

			Vector2 delta = touchPos - initialTouchPos;
			Vector2 newSize = initialSizeDelta + new Vector2(delta.x, -delta.y);

			if (newSize.x < fileBrowser.minWidth) newSize.x = fileBrowser.minWidth;
			if (newSize.y < fileBrowser.minHeight) newSize.y = fileBrowser.minHeight;

			newSize.x = (int)newSize.x;
			newSize.y = (int)newSize.y;

			delta = newSize - initialSizeDelta;

			window.anchoredPosition = initialAnchoredPos + new Vector2(delta.x * 0.5f, delta.y * -0.5f);
			window.sizeDelta = newSize;

			listView.OnViewportDimensionsChanged();
		}

		public void OnEndResize(BaseEventData data)
		{
			fileBrowser.EnsureWindowIsWithinBounds();
		}
		#endregion
	}
}
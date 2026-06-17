using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


/// <summary>
/// 인벤토리 드래그 아이템(개별 모듈). 길게 눌러서 드래그하면 함선 슬롯에 부착되고,
/// 짧게 끌면 스크롤뷰 스크롤로 전달된다(스크롤과 픽업 분리).
/// </summary>
public class InventoryDragItem : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	/// <summary>
	/// 픽업으로 인정하는 누름 유지 시간(초).
	/// </summary>
	private const float HoldThreshold = 0.22f;

	private int m_InstanceId;
	private ModuleType m_Type;
	private PlayerState m_PlayerState;
	private ShipBuilder m_ShipBuilder;
	private Canvas m_Canvas;
	private ScrollRect m_ScrollRect;
	private Color m_Color;
	private GameObject m_Ghost;
	private float m_PointerDownTime;
	private bool m_Scrolling;

	/// <summary>
	/// 드래그 아이템을 초기화한다.
	/// </summary>
	public void Initialize(int instanceId, ModuleType type, PlayerState playerState, ShipBuilder shipBuilder, Canvas canvas, ScrollRect scrollRect, Color color)
	{
		m_InstanceId = instanceId;
		m_Type = type;
		m_PlayerState = playerState;
		m_ShipBuilder = shipBuilder;
		m_Canvas = canvas;
		m_ScrollRect = scrollRect;
		m_Color = color;
	}

	/// <summary>
	/// 누름 시각을 기록한다.
	/// </summary>
	public void OnPointerDown(PointerEventData eventData)
	{
		m_PointerDownTime = Time.unscaledTime;
	}

	/// <summary>
	/// 드래그 시작. 길게 눌렀으면 픽업, 아니면 스크롤로 전달한다.
	/// </summary>
	public void OnBeginDrag(PointerEventData eventData)
	{
		var held = Time.unscaledTime - m_PointerDownTime;
		var canPickup = held >= HoldThreshold && m_PlayerState != null && m_ShipBuilder != null && m_PlayerState.ContainsModule(m_InstanceId);
		if (!canPickup)
		{
			m_Scrolling = true;
			if (m_ScrollRect != null)
			{
				m_ScrollRect.OnBeginDrag(eventData);
			}

			return;
		}

		m_Scrolling = false;

		m_Ghost = new GameObject("DragGhost", typeof(RectTransform), typeof(Image));
		m_Ghost.layer = gameObject.layer;
		m_Ghost.transform.SetParent(m_Canvas.transform, false);
		m_Ghost.transform.SetAsLastSibling();
		var ghostImage = m_Ghost.GetComponent<Image>();
		ghostImage.color = new Color(m_Color.r, m_Color.g, m_Color.b, 0.85f);
		ghostImage.raycastTarget = false;
		var ghostRect = (RectTransform)m_Ghost.transform;
		ghostRect.sizeDelta = new Vector2(120f, 120f);

		m_ShipBuilder.BeginDragAttach();
		MoveGhost(eventData.position);
	}

	/// <summary>
	/// 드래그 중. 스크롤이면 전달, 픽업이면 고스트/미리보기 갱신.
	/// </summary>
	public void OnDrag(PointerEventData eventData)
	{
		if (m_Scrolling)
		{
			if (m_ScrollRect != null)
			{
				m_ScrollRect.OnDrag(eventData);
			}

			return;
		}

		if (m_Ghost == null)
		{
			return;
		}

		MoveGhost(eventData.position);
		m_ShipBuilder.UpdateAttachPreview(eventData.position, m_Type);
	}

	/// <summary>
	/// 드래그 종료. 스크롤이면 전달, 픽업이면 슬롯 드롭 시 부착하고 소모한다.
	/// </summary>
	public void OnEndDrag(PointerEventData eventData)
	{
		if (m_Scrolling)
		{
			m_Scrolling = false;
			if (m_ScrollRect != null)
			{
				m_ScrollRect.OnEndDrag(eventData);
			}

			return;
		}

		if (m_Ghost == null)
		{
			return;
		}

		Destroy(m_Ghost);
		m_Ghost = null;

		var attached = false;
		if (m_PlayerState.ContainsModule(m_InstanceId))
		{
			attached = m_ShipBuilder.TryAttachAtScreenPosition(eventData.position, m_Type);
		}

		m_ShipBuilder.EndDragAttach();

		if (attached)
		{
			m_PlayerState.RemoveModule(m_InstanceId);
		}
	}

	/// <summary>
	/// 고스트를 화면 좌표로 이동한다.
	/// </summary>
	private void MoveGhost(Vector2 screenPosition)
	{
		if (m_Ghost == null)
		{
			return;
		}

		var canvasRect = m_Canvas.transform as RectTransform;
		Vector3 worldPoint;
		if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screenPosition, m_Canvas.worldCamera, out worldPoint))
		{
			m_Ghost.transform.position = worldPoint;
		}
	}
}

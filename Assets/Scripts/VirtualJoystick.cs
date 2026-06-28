using UnityEngine;
using UnityEngine.EventSystems;


/// <summary>
/// 가상 조이스틱. 배경 영역을 드래그하면 핸들이 따라오고, 중심으로부터의 방향(-1~1)을 제공한다.
/// 전투 씬에서 수동 이동 입력으로 사용한다.
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
	private RectTransform m_Background;
	private RectTransform m_Handle;
	private float m_Radius;
	private Vector2 m_Direction;

	/// <summary>
	/// 현재 입력 방향(x: 가로, y: 세로, 크기 0~1).
	/// </summary>
	public Vector2 Direction
	{
		get { return m_Direction; }
	}

	/// <summary>
	/// 배경/핸들/반경을 설정한다.
	/// </summary>
	public void Configure(RectTransform background, RectTransform handle, float radius)
	{
		m_Background = background;
		m_Handle = handle;
		m_Radius = radius;
	}

	/// <summary>
	/// 눌렀을 때. 즉시 드래그로 처리한다.
	/// </summary>
	public void OnPointerDown(PointerEventData eventData)
	{
		OnDrag(eventData);
	}

	/// <summary>
	/// 드래그 중. 핸들을 옮기고 방향을 갱신한다.
	/// </summary>
	public void OnDrag(PointerEventData eventData)
	{
		Vector2 localPoint;
		if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Background, eventData.position, eventData.pressEventCamera, out localPoint))
		{
			return;
		}

		var offset = Vector2.ClampMagnitude(localPoint, m_Radius);
		m_Handle.anchoredPosition = offset;
		m_Direction = offset / m_Radius;
	}

	/// <summary>
	/// 떼었을 때. 핸들과 방향을 초기화한다.
	/// </summary>
	public void OnPointerUp(PointerEventData eventData)
	{
		m_Direction = Vector2.zero;
		m_Handle.anchoredPosition = Vector2.zero;
	}
}

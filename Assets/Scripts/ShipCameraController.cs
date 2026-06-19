using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


/// <summary>
/// 함선 카메라 컨트롤러. 대상 지점을 바라보며 거리(줌)를 조절한다.
/// 마우스 휠과 터치 핀치로 확대/축소하고, 드래그로 시점(영역)을 이동한다.
/// </summary>
public class ShipCameraController : MonoBehaviour
{
	#region INSPECTOR
	[SerializeField] private Transform m_Target;
	[SerializeField] private float m_WheelZoomSpeed = 1f;
	[SerializeField] private float m_PinchZoomSpeed = 0.02f;
	[SerializeField] private float m_MinDistance = 2f;
	[SerializeField] private float m_MaxDistance = 16f;
	[SerializeField] private float m_PanSpeed = 0.01f;
	[SerializeField] private float m_MaxPan = 6f;
	#endregion

	/// <summary>
	/// 드래그로 누적된 시점 이동량(지면 평면).
	/// </summary>
	private Vector3 m_PanOffset;

	/// <summary>
	/// 직전 프레임의 포인터 위치(드래그 계산용).
	/// </summary>
	private Vector2 m_PreviousPointer;

	/// <summary>
	/// 드래그 진행 여부.
	/// </summary>
	private bool m_Dragging;

	/// <summary>
	/// 대상에서 카메라로 향하는 정규화된 방향.
	/// </summary>
	private Vector3 m_Direction;

	/// <summary>
	/// 대상과 카메라 사이의 현재 거리.
	/// </summary>
	private float m_Distance;

	/// <summary>
	/// 직전 프레임의 두 손가락 거리(핀치 계산용).
	/// </summary>
	private float m_PreviousPinchDistance;

	/// <summary>
	/// 초기화됨.
	/// </summary>
	private void Start()
	{
		var targetPosition = GetTargetPosition();
		var offset = transform.position - targetPosition;
		m_Distance = offset.magnitude;
		m_Direction = offset.normalized;
	}

	/// <summary>
	/// 매 프레임 줌 입력을 처리한다.
	/// </summary>
	private void Update()
	{
		var zoomDelta = ReadWheelZoom() + ReadPinchZoom();
		if (Mathf.Abs(zoomDelta) > Mathf.Epsilon)
		{
			m_Distance = Mathf.Clamp(m_Distance - zoomDelta, m_MinDistance, m_MaxDistance);
			ApplyDistance();
		}

		if (HandlePan())
		{
			ApplyDistance();
		}
	}

	/// <summary>
	/// 드래그로 시점을 이동한다. 이동이 발생하면 true. UI 위 드래그는 무시한다.
	/// </summary>
	private bool HandlePan()
	{
		var pointer = Pointer.current;
		if (pointer == null)
		{
			return false;
		}

		var isPressed = pointer.press.isPressed;
		var position = pointer.position.ReadValue();

		if (isPressed && !m_Dragging && !IsOverUi())
		{
			m_Dragging = true;
			m_PreviousPointer = position;
			return false;
		}

		if (isPressed && m_Dragging)
		{
			var delta = position - m_PreviousPointer;
			m_PreviousPointer = position;
			var move = (-transform.right * delta.x - transform.up * delta.y) * m_PanSpeed;
			move.y = 0f;
			m_PanOffset += move;
			m_PanOffset.x = Mathf.Clamp(m_PanOffset.x, -m_MaxPan, m_MaxPan);
			m_PanOffset.z = Mathf.Clamp(m_PanOffset.z, -m_MaxPan, m_MaxPan);
			return true;
		}

		if (!isPressed)
		{
			m_Dragging = false;
		}

		return false;
	}

	/// <summary>
	/// 포인터가 UI 위에 있는지 확인한다.
	/// </summary>
	private bool IsOverUi()
	{
		return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
	}

	/// <summary>
	/// 마우스 휠 줌 입력 값.
	/// </summary>
	private float ReadWheelZoom()
	{
		var mouse = Mouse.current;
		if (mouse == null)
		{
			return 0f;
		}

		var scrollY = mouse.scroll.ReadValue().y;
		return scrollY * m_WheelZoomSpeed * 0.01f;
	}

	/// <summary>
	/// 터치 핀치 줌 입력 값.
	/// </summary>
	private float ReadPinchZoom()
	{
		var touchscreen = Touchscreen.current;
		if (touchscreen == null)
		{
			return 0f;
		}

		var firstTouch = touchscreen.touches[0];
		var secondTouch = touchscreen.touches[1];
		if (!firstTouch.press.isPressed || !secondTouch.press.isPressed)
		{
			m_PreviousPinchDistance = 0f;
			return 0f;
		}

		var firstPosition = firstTouch.position.ReadValue();
		var secondPosition = secondTouch.position.ReadValue();
		var pinchDistance = Vector2.Distance(firstPosition, secondPosition);

		if (m_PreviousPinchDistance <= 0f)
		{
			m_PreviousPinchDistance = pinchDistance;
			return 0f;
		}

		var pinchDelta = pinchDistance - m_PreviousPinchDistance;
		m_PreviousPinchDistance = pinchDistance;
		return pinchDelta * m_PinchZoomSpeed;
	}

	/// <summary>
	/// 현재 거리로 카메라 위치를 갱신한다.
	/// </summary>
	private void ApplyDistance()
	{
		var targetPosition = GetTargetPosition();
		transform.position = targetPosition + m_Direction * m_Distance;
		transform.LookAt(targetPosition);
	}

	/// <summary>
	/// 대상 지점 위치(대상 미지정 시 원점).
	/// </summary>
	private Vector3 GetTargetPosition()
	{
		if (m_Target != null)
		{
			return m_Target.position + m_PanOffset;
		}

		return m_PanOffset;
	}
}

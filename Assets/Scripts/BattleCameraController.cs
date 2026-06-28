using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


/// <summary>
/// 전투 카메라 컨트롤러. 드래그로 화면(공간)을 부드럽게 이동한다.
/// 함선을 조작하는 것이 아니라 시점을 옮기는 용도이며, UI 위 드래그는 무시한다.
/// </summary>
public class BattleCameraController : MonoBehaviour
{
	[SerializeField] private float m_PanSpeed = 0.03f;
	[SerializeField] private float m_MaxPan = 13f;
	[SerializeField] private float m_Smooth = 9f;

	private Vector3 m_BasePosition;
	private Vector3 m_TargetOffset;
	private Vector2 m_PreviousPointer;
	private bool m_PressActive;
	private bool m_IgnorePress;

	/// <summary>
	/// 초기화됨.
	/// </summary>
	private void Start()
	{
		m_BasePosition = transform.position;
	}

	/// <summary>
	/// 매 프레임 드래그 입력으로 시점을 이동한다.
	/// </summary>
	private void Update()
	{
		var pointer = Pointer.current;
		if (pointer != null)
		{
			var isPressed = pointer.press.isPressed;
			var position = pointer.position.ReadValue();

			if (isPressed && !m_PressActive)
			{
				m_PressActive = true;
				m_IgnorePress = IsOverUi();
				m_PreviousPointer = position;
			}
			else if (isPressed && m_PressActive)
			{
				if (!m_IgnorePress)
				{
					var delta = position - m_PreviousPointer;
					m_TargetOffset += new Vector3(-delta.x, 0f, -delta.y) * m_PanSpeed;
					m_TargetOffset.x = Mathf.Clamp(m_TargetOffset.x, -m_MaxPan, m_MaxPan);
					m_TargetOffset.z = Mathf.Clamp(m_TargetOffset.z, -m_MaxPan, m_MaxPan);
				}

				m_PreviousPointer = position;
			}
			else if (!isPressed)
			{
				m_PressActive = false;
				m_IgnorePress = false;
			}
		}

		var targetPosition = m_BasePosition + m_TargetOffset;
		transform.position = Vector3.Lerp(transform.position, targetPosition, Time.unscaledDeltaTime * m_Smooth);
	}

	/// <summary>
	/// 포인터가 UI 위에 있는지 확인한다.
	/// </summary>
	private bool IsOverUi()
	{
		return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
	}
}

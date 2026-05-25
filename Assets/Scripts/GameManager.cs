using UnityEngine;



/// <summary>
/// 게임 매니저.
/// </summary>
public class GameManager : SharedComponent2<GameManager>
{
	#region INSPECTOR
	[SerializeField] private RectTransform m_HUDRectTransform;
	#endregion

	/// <summary>
	/// 생성됨.
	/// </summary>
	protected override void Awake()
	{
		base.Awake();

		if (m_HUDRectTransform == null)
		{
			//GameObject.FindFirstObjectByType<RectTransform>();
			var obj = GameObject.Find("UI/Canvas/HUD");
			m_HUDRectTransform = obj.GetComponent<RectTransform>();
		}
	}

	/// <summary>
	/// 초기화됨.
	/// </summary>
	protected override void Start()
	{
		base.Start();
	}
}
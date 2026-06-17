using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 메인 UI 컨트롤러. 하단 네비게이션 탭 전환과 상단 HUD 표시를 담당한다.
/// 씬에 구성된 UI 오브젝트를 이름으로 찾아 연결한다.
/// </summary>
public class MainUi : MonoBehaviour
{
	#region INSPECTOR
	[SerializeField] private Color m_ActiveColor = new Color(0.15f, 0.5f, 0.55f, 1f);
	[SerializeField] private Color m_InactiveColor = new Color(0.16f, 0.18f, 0.22f, 1f);
	[SerializeField] private Color m_ActiveLabelColor = Color.white;
	[SerializeField] private Color m_InactiveLabelColor = new Color(0.7f, 0.72f, 0.78f, 1f);
	#endregion

	/// <summary>
	/// 탭/화면 이름(좌→우 순서).
	/// </summary>
	private static readonly string[] s_ScreenNames = new string[] { "개조", "상점", "전투", "설정" };

	private PlayerState m_PlayerState;
	private GameObject m_Ship;
	private Text m_LevelText;
	private Text m_ExperienceText;
	private Text m_ActivityText;
	private Text m_CurrencyText;
	private readonly List<Button> m_NavButtons = new List<Button>();
	private readonly List<GameObject> m_Screens = new List<GameObject>();

	/// <summary>
	/// 초기화됨.
	/// </summary>
	private void Start()
	{
		var canvasObject = GameObject.Find("UI/Canvas");
		var canvasTransform = canvasObject.transform;
		m_PlayerState = FindFirstObjectByType<PlayerState>();
		m_Ship = GameObject.Find("Ship");

		var topHudTransform = canvasTransform.Find("TopHud");
		m_LevelText = topHudTransform.Find("LevelText").GetComponent<Text>();
		m_ActivityText = topHudTransform.Find("ActivityText").GetComponent<Text>();
		m_CurrencyText = topHudTransform.Find("CurrencyText").GetComponent<Text>();

		var experienceTransform = topHudTransform.Find("ExperienceText");
		if (experienceTransform != null)
		{
			m_ExperienceText = experienceTransform.GetComponent<Text>();
		}

		m_LevelText.font = UiFont.Default;
		m_ActivityText.font = UiFont.Default;
		m_CurrencyText.font = UiFont.Default;
		if (m_ExperienceText != null)
		{
			m_ExperienceText.font = UiFont.Default;
		}

		var screensTransform = canvasTransform.Find("Screens");
		var navigationTransform = canvasTransform.Find("BottomNavigation");
		for (int index = 0; index < s_ScreenNames.Length; index++)
		{
			var screenName = s_ScreenNames[index];
			var screenObject = screensTransform.Find("Screen_" + screenName).gameObject;
			m_Screens.Add(screenObject);

			var navButton = navigationTransform.Find("NavButton_" + screenName).GetComponent<Button>();
			m_NavButtons.Add(navButton);
			var capturedIndex = index;
			navButton.onClick.AddListener(() => SelectScreen(capturedIndex));
		}

		if (m_PlayerState != null)
		{
			m_PlayerState.Changed += RefreshHud;
		}

		RefreshHud();
		SelectScreen(0);
	}

	/// <summary>
	/// 파괴됨.
	/// </summary>
	private void OnDestroy()
	{
		if (m_PlayerState != null)
		{
			m_PlayerState.Changed -= RefreshHud;
		}
	}

	/// <summary>
	/// 지정한 탭 화면을 활성화한다.
	/// </summary>
	public void SelectScreen(int activeIndex)
	{
		for (int index = 0; index < m_Screens.Count; index++)
		{
			var isActive = (index == activeIndex);
			m_Screens[index].SetActive(isActive);

			var buttonImage = m_NavButtons[index].GetComponent<Image>();
			buttonImage.color = isActive ? m_ActiveColor : m_InactiveColor;

			var buttonLabel = m_NavButtons[index].GetComponentInChildren<Text>();
			buttonLabel.color = isActive ? m_ActiveLabelColor : m_InactiveLabelColor;
		}

		if (m_Ship != null)
		{
			m_Ship.SetActive(activeIndex == 0);
		}
	}

	/// <summary>
	/// 상단 HUD 텍스트를 갱신한다.
	/// </summary>
	private void RefreshHud()
	{
		if (m_PlayerState == null)
		{
			return;
		}

		var level = m_PlayerState.Level;
		m_LevelText.text = "Lv. " + level;

		if (m_ExperienceText != null)
		{
			var experience = m_PlayerState.Experience;
			m_ExperienceText.text = "EXP " + experience + " / " + level;
		}

		var activity = m_PlayerState.Activity;
		var maxActivity = m_PlayerState.MaxActivity;
		m_ActivityText.text = "활동력 " + activity + " / " + maxActivity;

		var currency = m_PlayerState.Currency;
		m_CurrencyText.text = "재화 " + currency;
	}
}

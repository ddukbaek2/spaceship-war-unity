using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 메인 UI 컨트롤러. 하단 네비게이션 탭 전환과 상단 HUD 표시를 담당한다.
/// 상단 HUD/탭 라벨/설정 화면 텍스트를 TextMeshPro로 구성한다.
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
	private TMP_Text m_LevelText;
	private TMP_Text m_ExperienceText;
	private TMP_Text m_ActivityText;
	private TMP_Text m_CurrencyText;
	private readonly List<Button> m_NavButtons = new List<Button>();
	private readonly List<TMP_Text> m_NavLabels = new List<TMP_Text>();
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

		BuildTopHud(canvasTransform.Find("TopHud"));

		var screensTransform = canvasTransform.Find("Screens");
		var navigationTransform = canvasTransform.Find("BottomNavigation");
		for (int index = 0; index < s_ScreenNames.Length; index++)
		{
			var screenName = s_ScreenNames[index];
			var screenObject = screensTransform.Find("Screen_" + screenName).gameObject;
			m_Screens.Add(screenObject);

			var navButton = navigationTransform.Find("NavButton_" + screenName).GetComponent<Button>();
			m_NavButtons.Add(navButton);
			var navLabel = ReplaceLabel(navButton.transform, screenName, 40);
			m_NavLabels.Add(navLabel);

			var capturedIndex = index;
			navButton.onClick.AddListener(() => SelectScreen(capturedIndex));
		}

		BuildSettingsPlaceholder(screensTransform.Find("Screen_설정"));

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
	/// 상단 HUD 텍스트(레벨/경험치/활동력/재화)를 TMP로 구성한다.
	/// </summary>
	private void BuildTopHud(Transform topHud)
	{
		for (int index = topHud.childCount - 1; index >= 0; index--)
		{
			DestroyImmediate(topHud.GetChild(index).gameObject);
		}

		m_LevelText = MakeHudText(topHud, "LevelText", 0f, 0.25f, 24f, 0f, 30, Color.white, TextAnchor.MiddleLeft);
		m_ExperienceText = MakeHudText(topHud, "ExperienceText", 0.25f, 0.5f, 0f, 0f, 28, new Color(0.65f, 0.9f, 0.55f, 1f), TextAnchor.MiddleCenter);
		m_ActivityText = MakeHudText(topHud, "ActivityText", 0.5f, 0.78f, 0f, 0f, 28, new Color(0.6f, 0.85f, 1f, 1f), TextAnchor.MiddleCenter);
		m_CurrencyText = MakeHudText(topHud, "CurrencyText", 0.78f, 1f, 0f, -24f, 30, new Color(1f, 0.85f, 0.4f, 1f), TextAnchor.MiddleRight);
	}

	/// <summary>
	/// HUD 텍스트 한 칸을 생성한다.
	/// </summary>
	private TMP_Text MakeHudText(Transform parent, string name, float minX, float maxX, float padMin, float padMax, int fontSize, Color color, TextAnchor anchor)
	{
		var text = UiFactory.CreateText(name, parent, null, "", fontSize, color, anchor);
		var rectTransform = text.rectTransform;
		rectTransform.anchorMin = new Vector2(minX, 0f);
		rectTransform.anchorMax = new Vector2(maxX, 1f);
		rectTransform.offsetMin = new Vector2(padMin, 0f);
		rectTransform.offsetMax = new Vector2(padMax, 0f);
		return text;
	}

	/// <summary>
	/// 버튼의 기존 라벨을 제거하고 TMP 라벨로 교체한다.
	/// </summary>
	private TMP_Text ReplaceLabel(Transform buttonTransform, string content, int fontSize)
	{
		var existing = buttonTransform.Find("Label");
		if (existing != null)
		{
			DestroyImmediate(existing.gameObject);
		}

		return UiFactory.CreateText("Label", buttonTransform, null, content, fontSize, Color.white, TextAnchor.MiddleCenter);
	}

	/// <summary>
	/// 설정 화면 플레이스홀더를 TMP로 구성한다.
	/// </summary>
	private void BuildSettingsPlaceholder(Transform settingsScreen)
	{
		if (settingsScreen == null)
		{
			return;
		}

		for (int index = settingsScreen.childCount - 1; index >= 0; index--)
		{
			DestroyImmediate(settingsScreen.GetChild(index).gameObject);
		}

		UiFactory.CreateText("Title", settingsScreen, null, "설정", 80, new Color(0.85f, 0.88f, 0.95f, 1f), TextAnchor.MiddleCenter);
		var note = UiFactory.CreateText("Note", settingsScreen, null, "준비 중", 36, new Color(0.5f, 0.52f, 0.58f, 1f), TextAnchor.MiddleCenter);
		note.rectTransform.anchoredPosition = new Vector2(0f, -80f);
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

			m_NavLabels[index].color = isActive ? m_ActiveLabelColor : m_InactiveLabelColor;
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

		var experience = m_PlayerState.Experience;
		m_ExperienceText.text = "EXP " + experience + " / " + level;

		var activity = m_PlayerState.Activity;
		var maxActivity = m_PlayerState.MaxActivity;
		m_ActivityText.text = "활동력 " + activity + " / " + maxActivity;

		var currency = m_PlayerState.Currency;
		m_CurrencyText.text = "재화 " + currency;
	}
}

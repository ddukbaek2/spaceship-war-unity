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
	/// 탭/화면 오브젝트 이름(좌→우 순서).
	/// </summary>
	private static readonly string[] s_ScreenNames = new string[] { "개조", "상점", "전투", "모험", "이벤트", "설정", "우편함" };

	/// <summary>
	/// 탭에 표시할 라벨(좌→우 순서). 오브젝트 이름과 별개로 화면 표기만 담당한다.
	/// </summary>
	private static readonly string[] s_DisplayNames = new string[] { "데크", "상점", "스테이지", "모험", "이벤트", "설정", "우편함" };

	/// <summary>
	/// 데크 화면의 서브탭(개조/연구/승무원).
	/// </summary>
	private static readonly string[] s_SubTabNames = new string[] { "개조", "연구", "승무원" };

	/// <summary>
	/// 하단 탭 개수(앞 5개는 하단 네비, 이후는 상단 아이콘).
	/// </summary>
	private const int BottomTabCount = 5;

	/// <summary>
	/// 기본 진입 탭(모험=보스 전투).
	/// </summary>
	private const int DefaultScreenIndex = 3;

	private PlayerState m_PlayerState;
	private GameObject m_Ship;
	private GameObject m_TopIcons;
	private InventoryView m_Inventory;
	private ShopController m_Shop;
	private BattleController m_Battle;
	private ShipCameraController m_ShipCamera;
	private readonly List<Button> m_SubTabButtons = new List<Button>();
	private readonly List<TMP_Text> m_SubTabLabels = new List<TMP_Text>();
	private GameObject m_ResearchPanel;
	private GameObject m_CrewPanel;
	private TMP_Text m_AutoPreviewLabel;
	private int m_CurrentSubTab;
	private TMP_Text m_MoveModeLabel;
	private TMP_Text m_LevelText;
	private RectTransform m_ExpFillRect;
	private TMP_Text m_ActivityText;
	private TMP_Text m_ActivityTimerText;
	private TMP_Text m_CurrencyText;
	private TMP_Text m_MetalText;
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
		m_Inventory = FindFirstObjectByType<InventoryView>();
		m_Shop = FindFirstObjectByType<ShopController>();
		m_Battle = FindFirstObjectByType<BattleController>();
		m_ShipCamera = FindFirstObjectByType<ShipCameraController>();

		BuildTopHud(canvasTransform.Find("TopHud"));

		var screensTransform = canvasTransform.Find("Screens");
		var navigationTransform = canvasTransform.Find("BottomNavigation");
		var topIconsTransform = canvasTransform.Find("TopIcons");
		m_TopIcons = topIconsTransform.gameObject;

		for (int index = 0; index < s_ScreenNames.Length; index++)
		{
			var screenName = s_ScreenNames[index];
			var screenObject = screensTransform.Find("Screen_" + screenName).gameObject;
			m_Screens.Add(screenObject);

			var navTransform = navigationTransform.Find("NavButton_" + screenName);
			if (navTransform == null)
			{
				navTransform = topIconsTransform.Find("NavButton_" + screenName);
			}

			var navButton = navTransform.GetComponent<Button>();
			m_NavButtons.Add(navButton);

			TMP_Text navLabel;
			if (index < BottomTabCount)
			{
				navLabel = ReplaceLabel(navButton.transform, s_DisplayNames[index], 34);
			}
			else
			{
				navLabel = navButton.GetComponentInChildren<TMP_Text>();
			}

			m_NavLabels.Add(navLabel);

			var capturedIndex = index;
			navButton.onClick.RemoveAllListeners();
			navButton.onClick.AddListener(() => SelectScreen(capturedIndex));
		}

		BuildDeckSubTabs(screensTransform.Find("Screen_개조"));
		BuildSettingsPlaceholder(screensTransform.Find("Screen_설정"));

		if (m_PlayerState != null)
		{
			m_PlayerState.Changed += RefreshHud;
		}

		RefreshHud();
		SelectScreen(DefaultScreenIndex);
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

		// 경험치 게이지 (레벨을 게이지 안에 표시) (0 ~ 0.4)
		var gaugeBg = UiFactory.CreateImage("ExpGaugeBg", topHud, new Color(0.16f, 0.18f, 0.22f, 1f));
		var bgImage = gaugeBg.GetComponent<Image>();
		bgImage.sprite = null;
		bgImage.type = Image.Type.Simple;
		bgImage.raycastTarget = false;
		var bgRect = (RectTransform)gaugeBg.transform;
		bgRect.anchorMin = new Vector2(0f, 0.24f);
		bgRect.anchorMax = new Vector2(0.4f, 0.76f);
		bgRect.offsetMin = new Vector2(20f, 0f);
		bgRect.offsetMax = new Vector2(-8f, 0f);

		var gaugeFill = UiFactory.CreateImage("ExpGaugeFill", gaugeBg.transform, new Color(0.35f, 0.72f, 0.9f, 1f));
		var fillImage = gaugeFill.GetComponent<Image>();
		fillImage.sprite = null;
		fillImage.type = Image.Type.Simple;
		fillImage.raycastTarget = false;
		m_ExpFillRect = (RectTransform)gaugeFill.transform;
		m_ExpFillRect.anchorMin = new Vector2(0f, 0f);
		m_ExpFillRect.anchorMax = new Vector2(0f, 1f);
		m_ExpFillRect.pivot = new Vector2(0f, 0.5f);
		m_ExpFillRect.offsetMin = new Vector2(0f, 0f);
		m_ExpFillRect.offsetMax = new Vector2(0f, 0f);

		m_LevelText = UiFactory.CreateText("LevelText", gaugeBg.transform, null, "", 24, Color.white, TextAnchor.MiddleCenter);
		m_LevelText.raycastTarget = false;
		var levelRect = m_LevelText.rectTransform;
		levelRect.anchorMin = new Vector2(0f, 0f);
		levelRect.anchorMax = new Vector2(1f, 1f);
		levelRect.offsetMin = new Vector2(0f, 0f);
		levelRect.offsetMax = new Vector2(0f, 0f);

		// 활동력(위 절반) + 회복 타이머(아래 절반) — 겹치지 않게 분리
		m_ActivityText = MakeHudText(topHud, "ActivityText", 0.4f, 0.62f, 0f, 0f, 24, new Color(0.6f, 0.85f, 1f, 1f), TextAnchor.LowerCenter);
		var actRect = m_ActivityText.rectTransform;
		actRect.anchorMin = new Vector2(0.4f, 0.5f);
		actRect.anchorMax = new Vector2(0.62f, 1f);

		m_ActivityTimerText = MakeHudText(topHud, "ActivityTimer", 0.4f, 0.62f, 0f, 0f, 17, new Color(0.5f, 0.62f, 0.74f, 1f), TextAnchor.UpperCenter);
		var timerRect = m_ActivityTimerText.rectTransform;
		timerRect.anchorMin = new Vector2(0.4f, 0f);
		timerRect.anchorMax = new Vector2(0.62f, 0.5f);

		// 금속(좌) + 크레딧(우) 가로 배치 (0.62 ~ 1)
		m_MetalText = MakeHudText(topHud, "MetalText", 0.62f, 0.8f, 8f, 0f, 26, new Color(0.72f, 0.79f, 0.86f, 1f), TextAnchor.MiddleRight);
		m_CurrencyText = MakeHudText(topHud, "CurrencyText", 0.8f, 1f, 0f, -24f, 26, new Color(1f, 0.85f, 0.4f, 1f), TextAnchor.MiddleRight);
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

		var title = UiFactory.CreateText("Title", settingsScreen, null, "설정", 80, new Color(0.85f, 0.88f, 0.95f, 1f), TextAnchor.UpperCenter);
		var titleRect = title.rectTransform;
		titleRect.anchorMin = new Vector2(0f, 1f);
		titleRect.anchorMax = new Vector2(1f, 1f);
		titleRect.pivot = new Vector2(0.5f, 1f);
		titleRect.sizeDelta = new Vector2(0f, 120f);
		titleRect.anchoredPosition = new Vector2(0f, -140f);

		var moveModeButton = UiFactory.CreateImage("MoveModeButton", settingsScreen, new Color(0.2f, 0.42f, 0.48f, 1f));
		var moveModeRect = (RectTransform)moveModeButton.transform;
		moveModeRect.anchorMin = new Vector2(0.5f, 1f);
		moveModeRect.anchorMax = new Vector2(0.5f, 1f);
		moveModeRect.pivot = new Vector2(0.5f, 1f);
		moveModeRect.sizeDelta = new Vector2(680f, 130f);
		moveModeRect.anchoredPosition = new Vector2(0f, -360f);
		moveModeButton.AddComponent<Button>().onClick.AddListener(ToggleMoveMode);
		m_MoveModeLabel = UiFactory.CreateText("Label", moveModeButton.transform, null, "", 40, Color.white, TextAnchor.MiddleCenter);
		m_MoveModeLabel.raycastTarget = false;

		var fillButton = UiFactory.CreateImage("ActivityFillButton", settingsScreen, new Color(0.42f, 0.34f, 0.2f, 1f));
		var fillRect = (RectTransform)fillButton.transform;
		fillRect.anchorMin = new Vector2(0.5f, 1f);
		fillRect.anchorMax = new Vector2(0.5f, 1f);
		fillRect.pivot = new Vector2(0.5f, 1f);
		fillRect.sizeDelta = new Vector2(680f, 130f);
		fillRect.anchoredPosition = new Vector2(0f, -510f);
		fillButton.AddComponent<Button>().onClick.AddListener(FillActivityNow);
		var fillLabel = UiFactory.CreateText("Label", fillButton.transform, null, "활동력 가득 채우기", 40, Color.white, TextAnchor.MiddleCenter);
		fillLabel.raycastTarget = false;

		RefreshMoveModeLabel();
	}

	/// <summary>
	/// 활동력을 최대치로 채운다.
	/// </summary>
	private void FillActivityNow()
	{
		if (m_PlayerState == null)
		{
			return;
		}

		m_PlayerState.FillActivity();
	}

	/// <summary>
	/// 전투 이동 방식을 자동/수동으로 토글한다.
	/// </summary>
	private void ToggleMoveMode()
	{
		if (m_PlayerState == null)
		{
			return;
		}

		m_PlayerState.SetManualMove(!m_PlayerState.ManualMove);
		RefreshMoveModeLabel();
	}

	/// <summary>
	/// 전투 이동 방식 라벨을 갱신한다.
	/// </summary>
	private void RefreshMoveModeLabel()
	{
		if (m_MoveModeLabel == null)
		{
			return;
		}

		var manual = m_PlayerState != null && m_PlayerState.ManualMove;
		m_MoveModeLabel.text = "전투 이동: " + (manual ? "수동 (조이스틱)" : "자동");
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

		if (activeIndex == 0)
		{
			SelectSubTab(0);
		}
		else if (m_Ship != null)
		{
			m_Ship.SetActive(false);
		}

		if (m_TopIcons != null)
		{
			m_TopIcons.SetActive(s_ScreenNames[activeIndex] == "모험");
		}

		ResetActiveScreen(activeIndex);
	}

	/// <summary>
	/// 데크 화면 안에 서브탭(개조/연구/승무원) 바와 연구·승무원 패널을 구성한다.
	/// </summary>
	private void BuildDeckSubTabs(Transform deckScreen)
	{
		var bar = UiFactory.CreateImage("SubTabBar", deckScreen, new Color(0.1f, 0.11f, 0.14f, 0.95f));
		var barImage = bar.GetComponent<Image>();
		barImage.sprite = null;
		barImage.type = Image.Type.Simple;
		var barRect = (RectTransform)bar.transform;
		barRect.anchorMin = new Vector2(0f, 1f);
		barRect.anchorMax = new Vector2(1f, 1f);
		barRect.pivot = new Vector2(0.5f, 1f);
		barRect.sizeDelta = new Vector2(0f, 84f);

		for (int index = 0; index < s_SubTabNames.Length; index++)
		{
			var buttonObject = UiFactory.CreateImage("SubTab_" + s_SubTabNames[index], bar.transform, m_InactiveColor);
			var buttonImage = buttonObject.GetComponent<Image>();
			buttonImage.sprite = null;
			buttonImage.type = Image.Type.Simple;
			var buttonRect = (RectTransform)buttonObject.transform;
			buttonRect.anchorMin = new Vector2((float)index / s_SubTabNames.Length, 0f);
			buttonRect.anchorMax = new Vector2((float)(index + 1) / s_SubTabNames.Length, 1f);
			buttonRect.offsetMin = new Vector2(2f, 2f);
			buttonRect.offsetMax = new Vector2(-2f, -2f);

			var button = buttonObject.AddComponent<Button>();
			var label = UiFactory.CreateText("Label", buttonObject.transform, null, s_SubTabNames[index], 32, Color.white, TextAnchor.MiddleCenter);
			label.raycastTarget = false;
			m_SubTabButtons.Add(button);
			m_SubTabLabels.Add(label);

			var capturedIndex = index;
			button.onClick.AddListener(() => SelectSubTab(capturedIndex));
		}

		m_ResearchPanel = CreateSubPanel(deckScreen, "Sub_연구", "연구");
		m_CrewPanel = CreateSubPanel(deckScreen, "Sub_승무원", "승무원");

		// 자동 발사 미리보기 토글(데크 우상단, 서브탭바 아래)
		var autoButton = UiFactory.CreateImage("AutoPreviewToggle", deckScreen, new Color(0.2f, 0.42f, 0.48f, 1f));
		var autoRect = (RectTransform)autoButton.transform;
		autoRect.anchorMin = new Vector2(1f, 1f);
		autoRect.anchorMax = new Vector2(1f, 1f);
		autoRect.pivot = new Vector2(1f, 1f);
		autoRect.sizeDelta = new Vector2(240f, 64f);
		autoRect.anchoredPosition = new Vector2(-16f, -96f);
		autoButton.AddComponent<Button>().onClick.AddListener(ToggleAutoPreview);
		m_AutoPreviewLabel = UiFactory.CreateText("Label", autoButton.transform, null, "", 26, Color.white, TextAnchor.MiddleCenter);
		m_AutoPreviewLabel.raycastTarget = false;
		RefreshAutoPreviewLabel();
	}

	/// <summary>
	/// 자동 발사 미리보기를 켜고 끈다.
	/// </summary>
	private void ToggleAutoPreview()
	{
		if (m_PlayerState == null)
		{
			return;
		}

		m_PlayerState.SetAutoPreview(!m_PlayerState.AutoPreview);
		RefreshAutoPreviewLabel();
	}

	/// <summary>
	/// 자동 발사 토글 라벨을 갱신한다.
	/// </summary>
	private void RefreshAutoPreviewLabel()
	{
		if (m_AutoPreviewLabel == null)
		{
			return;
		}

		var on = m_PlayerState != null && m_PlayerState.AutoPreview;
		m_AutoPreviewLabel.text = "자동발사 " + (on ? "ON" : "OFF");
	}

	/// <summary>
	/// 서브탭 '준비 중' 패널을 만든다(서브탭바 아래 전체).
	/// </summary>
	private GameObject CreateSubPanel(Transform parent, string name, string display)
	{
		var panel = UiFactory.CreateImage(name, parent, new Color(0.08f, 0.09f, 0.12f, 1f));
		var rect = (RectTransform)panel.transform;
		rect.anchorMin = new Vector2(0f, 0f);
		rect.anchorMax = new Vector2(1f, 1f);
		rect.offsetMin = new Vector2(0f, 0f);
		rect.offsetMax = new Vector2(0f, -84f);

		var title = UiFactory.CreateText("Title", panel.transform, null, display, 72, new Color(0.85f, 0.88f, 0.95f, 1f), TextAnchor.MiddleCenter);
		title.rectTransform.anchoredPosition = new Vector2(0f, 40f);
		var note = UiFactory.CreateText("Note", panel.transform, null, "준비 중", 36, new Color(0.5f, 0.52f, 0.58f, 1f), TextAnchor.MiddleCenter);
		note.rectTransform.anchoredPosition = new Vector2(0f, -60f);
		panel.SetActive(false);
		return panel;
	}

	/// <summary>
	/// 데크 서브탭을 전환한다(개조=인벤토리+함선, 연구/승무원=준비 중).
	/// </summary>
	private void SelectSubTab(int index)
	{
		m_CurrentSubTab = index;
		var isBuild = (index == 0);

		if (m_Inventory != null)
		{
			m_Inventory.SetPanelVisible(isBuild);
		}

		if (m_Ship != null)
		{
			m_Ship.SetActive(isBuild);
		}

		if (m_ResearchPanel != null)
		{
			m_ResearchPanel.SetActive(index == 1);
		}

		if (m_CrewPanel != null)
		{
			m_CrewPanel.SetActive(index == 2);
		}

		for (int i = 0; i < m_SubTabButtons.Count; i++)
		{
			var isActive = (i == index);
			m_SubTabButtons[i].GetComponent<Image>().color = isActive ? m_ActiveColor : m_InactiveColor;
			m_SubTabLabels[i].color = isActive ? m_ActiveLabelColor : m_InactiveLabelColor;
		}

		if (isBuild)
		{
			if (m_ShipCamera != null)
			{
				m_ShipCamera.ResetView();
			}

			if (m_Inventory != null)
			{
				m_Inventory.ResetView();
			}
		}
	}

	/// <summary>
	/// 탭 진입 시 해당 화면의 상태(스크롤 위치/카메라/우주선 위치 등)를 초기화한다.
	/// </summary>
	private void ResetActiveScreen(int activeIndex)
	{
		if (activeIndex < 0 || activeIndex >= s_ScreenNames.Length)
		{
			return;
		}

		var screenName = s_ScreenNames[activeIndex];
		if (screenName == "상점")
		{
			if (m_Shop != null)
			{
				m_Shop.ResetView();
			}
		}
		else if (screenName == "모험")
		{
			if (m_Battle != null)
			{
				m_Battle.ResetView();
			}
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
		var ratio = level > 0 ? (float)experience / level : 0f;
		m_ExpFillRect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);

		var activity = m_PlayerState.Activity;
		m_ActivityText.text = "활동력 " + activity;

		var currency = m_PlayerState.Currency;
		m_CurrencyText.text = "크레딧 " + currency;

		var metal = m_PlayerState.Metal;
		m_MetalText.text = "금속 " + metal;
	}

	/// <summary>
	/// 매 프레임 활동력 회복 타이머를 갱신한다.
	/// </summary>
	private void Update()
	{
		if (m_ActivityTimerText == null || m_PlayerState == null)
		{
			return;
		}

		if (m_PlayerState.Activity >= m_PlayerState.MaxActivity)
		{
			m_ActivityTimerText.text = "가득 참";
			return;
		}

		var seconds = Mathf.CeilToInt(m_PlayerState.SecondsToNextRecovery);
		var minutes = seconds / 60;
		var rest = seconds % 60;
		m_ActivityTimerText.text = "+1까지 " + minutes + ":" + rest.ToString("00");
	}
}

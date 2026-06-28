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
	private static readonly string[] s_DisplayNames = new string[] { "개조", "상점", "스테이지", "모험", "이벤트", "설정", "우편함" };

	/// <summary>
	/// 하단 탭 개수(앞 5개는 하단 네비, 이후는 상단 아이콘).
	/// </summary>
	private const int BottomTabCount = 5;

	/// <summary>
	/// 기본 진입 탭(스테이지).
	/// </summary>
	private const int DefaultScreenIndex = 2;

	private PlayerState m_PlayerState;
	private GameObject m_Ship;
	private InventoryView m_Inventory;
	private ShopController m_Shop;
	private BattleController m_Battle;
	private ShipCameraController m_ShipCamera;
	private TMP_Text m_MoveModeLabel;
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
		m_Inventory = FindFirstObjectByType<InventoryView>();
		m_Shop = FindFirstObjectByType<ShopController>();
		m_Battle = FindFirstObjectByType<BattleController>();
		m_ShipCamera = FindFirstObjectByType<ShipCameraController>();

		BuildTopHud(canvasTransform.Find("TopHud"));

		var screensTransform = canvasTransform.Find("Screens");
		var navigationTransform = canvasTransform.Find("BottomNavigation");
		var topIconsTransform = canvasTransform.Find("TopIcons");

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

		if (m_Ship != null)
		{
			m_Ship.SetActive(activeIndex == 0);
		}

		ResetActiveScreen(activeIndex);
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
		if (screenName == "개조")
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
		else if (screenName == "상점")
		{
			if (m_Shop != null)
			{
				m_Shop.ResetView();
			}
		}
		else if (screenName == "전투")
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
		m_ExperienceText.text = "EXP " + experience + " / " + level;

		var activity = m_PlayerState.Activity;
		var maxActivity = m_PlayerState.MaxActivity;
		m_ActivityText.text = "활동력 " + activity + " / " + maxActivity;

		var currency = m_PlayerState.Currency;
		m_CurrencyText.text = "재화 " + currency;
	}
}

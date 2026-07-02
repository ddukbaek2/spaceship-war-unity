using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 우편함 컨트롤러. '알림'(게임 중 발생한 토스트/확인 팝업 기록)과 '공지'(개발사 우편) 탭을
/// 상단에 두고, 선택한 탭의 목록을 스크롤로 보여준다.
/// </summary>
public class MailboxController : MonoBehaviour
{
	/// <summary>
	/// 개발사 공지(우편). 최신이 위.
	/// </summary>
	private static readonly string[] s_Notices = new string[]
	{
		"[업데이트] 동력로 모듈이 추가되었습니다. 무기·추진체를 더 많이 가동하려면 동력로로 동력을 확보하세요.",
		"[안내] 스테이지는 이전 스테이지를 클리어해야 열립니다. 함선을 강화해 차례로 도전하세요.",
		"[안내] 데크에서 모듈을 장착해 함선을 개조할 수 있습니다. 무기·장갑·추진체·동력로를 조합하세요.",
		"[환영] 스페이스십 워에 오신 것을 환영합니다. 상점에서 모듈을 구매하고 전투에 도전해 보세요.",
	};

	private static readonly string[] s_TabNames = new string[] { "알림", "공지" };
	private const float RowSpacing = 12f;

	private RectTransform m_Content;
	private ScrollRect m_ScrollRect;
	private readonly List<Button> m_TabButtons = new List<Button>();
	private readonly List<TMP_Text> m_TabLabels = new List<TMP_Text>();
	private int m_CurrentTab;

	private readonly Color m_ActiveColor = new Color(0.2f, 0.42f, 0.48f, 1f);
	private readonly Color m_InactiveColor = new Color(0.16f, 0.18f, 0.22f, 1f);
	private readonly Color m_ActiveLabelColor = Color.white;
	private readonly Color m_InactiveLabelColor = new Color(0.7f, 0.72f, 0.78f, 1f);

	/// <summary>
	/// 초기화됨.
	/// </summary>
	private void Start()
	{
		var canvasObject = GameObject.Find("UI/Canvas");
		var mailScreen = canvasObject.transform.Find("Screens").Find("Screen_우편함");
		Build(mailScreen);

		NotificationLog.Changed += OnLogChanged;
		SelectTab(0);
	}

	/// <summary>
	/// 파괴됨.
	/// </summary>
	private void OnDestroy()
	{
		NotificationLog.Changed -= OnLogChanged;
	}

	/// <summary>
	/// 알림 로그 변경 시 알림 탭이면 목록을 갱신한다.
	/// </summary>
	private void OnLogChanged()
	{
		if (m_CurrentTab == 0)
		{
			RefreshList();
		}
	}

	/// <summary>
	/// 우편함 화면(배너 + 서브탭 + 스크롤 목록)을 구성한다.
	/// </summary>
	private void Build(Transform screen)
	{
		for (int index = screen.childCount - 1; index >= 0; index--)
		{
			DestroyImmediate(screen.GetChild(index).gameObject);
		}

		var background = UiFactory.CreateImage("Background", screen, new Color(0.08f, 0.09f, 0.12f, 1f));
		var backgroundRect = (RectTransform)background.transform;
		backgroundRect.anchorMin = new Vector2(0f, 0f);
		backgroundRect.anchorMax = new Vector2(1f, 1f);
		backgroundRect.offsetMin = new Vector2(0f, 0f);
		backgroundRect.offsetMax = new Vector2(0f, 0f);

		var banner = UiFactory.CreateImage("Banner", screen, new Color(0.16f, 0.18f, 0.26f, 1f));
		var bannerRect = (RectTransform)banner.transform;
		bannerRect.anchorMin = new Vector2(0f, 1f);
		bannerRect.anchorMax = new Vector2(1f, 1f);
		bannerRect.pivot = new Vector2(0.5f, 1f);
		bannerRect.sizeDelta = new Vector2(0f, 104f);
		UiFactory.CreateText("Title", banner.transform, null, "우편함", 52, new Color(0.85f, 0.9f, 1f, 1f), TextAnchor.MiddleCenter);

		var bar = UiFactory.CreateImage("TabBar", screen, new Color(0.1f, 0.11f, 0.14f, 0.95f));
		var barImage = bar.GetComponent<Image>();
		barImage.sprite = null;
		barImage.type = Image.Type.Simple;
		var barRect = (RectTransform)bar.transform;
		barRect.anchorMin = new Vector2(0f, 1f);
		barRect.anchorMax = new Vector2(1f, 1f);
		barRect.pivot = new Vector2(0.5f, 1f);
		barRect.sizeDelta = new Vector2(0f, 80f);
		barRect.anchoredPosition = new Vector2(0f, -104f);

		for (int index = 0; index < s_TabNames.Length; index++)
		{
			var buttonObject = UiFactory.CreateImage("Tab_" + s_TabNames[index], bar.transform, m_InactiveColor);
			var buttonImage = buttonObject.GetComponent<Image>();
			buttonImage.sprite = null;
			buttonImage.type = Image.Type.Simple;
			var buttonRect = (RectTransform)buttonObject.transform;
			buttonRect.anchorMin = new Vector2((float)index / s_TabNames.Length, 0f);
			buttonRect.anchorMax = new Vector2((float)(index + 1) / s_TabNames.Length, 1f);
			buttonRect.offsetMin = new Vector2(2f, 2f);
			buttonRect.offsetMax = new Vector2(-2f, -2f);

			var button = buttonObject.AddComponent<Button>();
			var label = UiFactory.CreateText("Label", buttonObject.transform, null, s_TabNames[index], 32, Color.white, TextAnchor.MiddleCenter);
			label.raycastTarget = false;
			m_TabButtons.Add(button);
			m_TabLabels.Add(label);

			var capturedIndex = index;
			button.onClick.AddListener(() => SelectTab(capturedIndex));
		}

		var scrollViewObject = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
		scrollViewObject.layer = screen.gameObject.layer;
		scrollViewObject.transform.SetParent(screen, false);
		var scrollViewRect = (RectTransform)scrollViewObject.transform;
		scrollViewRect.anchorMin = new Vector2(0f, 0f);
		scrollViewRect.anchorMax = new Vector2(1f, 1f);
		scrollViewRect.offsetMin = new Vector2(12f, 12f);
		scrollViewRect.offsetMax = new Vector2(-12f, -196f);

		var viewport = UiFactory.CreateImage("Viewport", scrollViewRect, new Color(0f, 0f, 0f, 0.12f));
		var viewportRect = (RectTransform)viewport.transform;
		viewportRect.anchorMin = new Vector2(0f, 0f);
		viewportRect.anchorMax = new Vector2(1f, 1f);
		viewportRect.offsetMin = new Vector2(0f, 0f);
		viewportRect.offsetMax = new Vector2(0f, 0f);
		viewport.AddComponent<RectMask2D>();

		var contentObject = new GameObject("Content", typeof(RectTransform));
		contentObject.layer = screen.gameObject.layer;
		contentObject.transform.SetParent(viewportRect, false);
		m_Content = (RectTransform)contentObject.transform;
		m_Content.anchorMin = new Vector2(0f, 1f);
		m_Content.anchorMax = new Vector2(1f, 1f);
		m_Content.pivot = new Vector2(0.5f, 1f);

		m_ScrollRect = scrollViewObject.GetComponent<ScrollRect>();
		m_ScrollRect.content = m_Content;
		m_ScrollRect.viewport = viewportRect;
		m_ScrollRect.horizontal = false;
		m_ScrollRect.vertical = true;
		m_ScrollRect.movementType = ScrollRect.MovementType.Elastic;
		m_ScrollRect.scrollSensitivity = 30f;
	}

	/// <summary>
	/// 탭을 전환한다(0=알림, 1=공지).
	/// </summary>
	private void SelectTab(int index)
	{
		m_CurrentTab = index;
		for (int i = 0; i < m_TabButtons.Count; i++)
		{
			var isActive = (i == index);
			m_TabButtons[i].GetComponent<Image>().color = isActive ? m_ActiveColor : m_InactiveColor;
			m_TabLabels[i].color = isActive ? m_ActiveLabelColor : m_InactiveLabelColor;
		}

		RefreshList();
		if (m_ScrollRect != null)
		{
			m_ScrollRect.verticalNormalizedPosition = 1f;
		}
	}

	/// <summary>
	/// 현재 탭의 목록을 다시 그린다.
	/// </summary>
	private void RefreshList()
	{
		if (m_Content == null)
		{
			return;
		}

		for (int index = m_Content.childCount - 1; index >= 0; index--)
		{
			Destroy(m_Content.GetChild(index).gameObject);
		}

		var items = new List<string>();
		if (m_CurrentTab == 0)
		{
			var entries = NotificationLog.Entries;
			for (int index = 0; index < entries.Count; index++)
			{
				items.Add(entries[index]);
			}

			if (items.Count == 0)
			{
				items.Add("아직 알림이 없습니다.");
			}
		}
		else
		{
			for (int index = 0; index < s_Notices.Length; index++)
			{
				items.Add(s_Notices[index]);
			}
		}

		var topPad = 8f;
		var offsetY = topPad;
		for (int index = 0; index < items.Count; index++)
		{
			offsetY += CreateRow(items[index], offsetY);
		}

		m_Content.sizeDelta = new Vector2(0f, offsetY + topPad);
	}

	/// <summary>
	/// 목록 한 줄(자동 높이)을 만들고 사용한 높이를 반환한다.
	/// </summary>
	private float CreateRow(string message, float offsetY)
	{
		var row = UiFactory.CreateImage("Row", m_Content, new Color(0.13f, 0.15f, 0.2f, 1f));
		var rowText = UiFactory.CreateText("Text", row.transform, null, message, 28, new Color(0.85f, 0.88f, 0.92f, 1f), TextAnchor.UpperLeft);
		rowText.raycastTarget = false;
		rowText.enableWordWrapping = true;
		rowText.overflowMode = TextOverflowModes.Overflow;
		var textRect = rowText.rectTransform;
		textRect.anchorMin = new Vector2(0f, 0f);
		textRect.anchorMax = new Vector2(1f, 1f);
		textRect.offsetMin = new Vector2(20f, 14f);
		textRect.offsetMax = new Vector2(-20f, -14f);

		var lines = Mathf.Max(1, Mathf.CeilToInt(message.Length / 28f));
		var height = 40f + 34f * lines;

		var rowRect = (RectTransform)row.transform;
		rowRect.anchorMin = new Vector2(0f, 1f);
		rowRect.anchorMax = new Vector2(1f, 1f);
		rowRect.pivot = new Vector2(0.5f, 1f);
		rowRect.sizeDelta = new Vector2(0f, height);
		rowRect.anchoredPosition = new Vector2(0f, -offsetY);

		return height + RowSpacing;
	}
}

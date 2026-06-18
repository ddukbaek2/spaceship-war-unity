using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 인벤토리 표시. 개조 화면 하단에 보유 모듈을 2열 스크롤뷰로 표시한다.
/// 각 모듈은 개별 셀이며, 함선 슬롯으로 드래그하면 부착된다.
/// </summary>
public class InventoryView : MonoBehaviour
{
	private PlayerState m_PlayerState;
	private ShipBuilder m_ShipBuilder;
	private Canvas m_Canvas;
	private Font m_Font;
	private RectTransform m_Content;
	private ScrollRect m_ScrollRect;

	/// <summary>
	/// 초기화됨.
	/// </summary>
	private void Start()
	{
		m_PlayerState = FindFirstObjectByType<PlayerState>();
		m_ShipBuilder = FindFirstObjectByType<ShipBuilder>();
		m_Font = UiFont.Default;

		var canvasObject = GameObject.Find("UI/Canvas");
		m_Canvas = canvasObject.GetComponent<Canvas>();
		var screensTransform = canvasObject.transform.Find("Screens");
		var modifyScreen = screensTransform.Find("Screen_개조");
		Build(modifyScreen);

		if (m_PlayerState != null)
		{
			m_PlayerState.Changed += Refresh;
		}

		Refresh();
	}

	/// <summary>
	/// 파괴됨.
	/// </summary>
	private void OnDestroy()
	{
		if (m_PlayerState != null)
		{
			m_PlayerState.Changed -= Refresh;
		}
	}

	/// <summary>
	/// 인벤토리 패널과 스크롤뷰를 구성한다.
	/// </summary>
	private void Build(Transform modifyScreen)
	{
		var panel = UiFactory.CreateImage("InventoryPanel", modifyScreen, new Color(0.07f, 0.08f, 0.11f, 0.92f));
		var panelRect = (RectTransform)panel.transform;
		panelRect.anchorMin = new Vector2(0f, 0f);
		panelRect.anchorMax = new Vector2(1f, 0f);
		panelRect.pivot = new Vector2(0.5f, 0f);
		panelRect.offsetMin = new Vector2(8f, 0f);
		panelRect.offsetMax = new Vector2(-8f, 480f);

		var header = UiFactory.CreateText("Header", panel.transform, m_Font, "인벤토리 (항목 선택 → 빈 칸 탭하여 부착 · 함선 모듈 탭하여 제거)", 24, new Color(0.7f, 0.72f, 0.78f, 1f), TextAnchor.MiddleCenter);
		header.raycastTarget = false;
		var headerRect = header.rectTransform;
		headerRect.anchorMin = new Vector2(0f, 1f);
		headerRect.anchorMax = new Vector2(1f, 1f);
		headerRect.pivot = new Vector2(0.5f, 1f);
		headerRect.sizeDelta = new Vector2(0f, 50f);
		headerRect.anchoredPosition = new Vector2(0f, -6f);

		var scrollViewObject = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
		scrollViewObject.layer = panel.layer;
		scrollViewObject.transform.SetParent(panel.transform, false);
		var scrollViewRect = (RectTransform)scrollViewObject.transform;
		scrollViewRect.anchorMin = new Vector2(0f, 0f);
		scrollViewRect.anchorMax = new Vector2(1f, 1f);
		scrollViewRect.offsetMin = new Vector2(8f, 8f);
		scrollViewRect.offsetMax = new Vector2(-8f, -60f);

		var viewport = UiFactory.CreateImage("Viewport", scrollViewRect, new Color(0f, 0f, 0f, 0.15f));
		var viewportRect = (RectTransform)viewport.transform;
		viewportRect.anchorMin = new Vector2(0f, 0f);
		viewportRect.anchorMax = new Vector2(1f, 1f);
		viewportRect.offsetMin = new Vector2(0f, 0f);
		viewportRect.offsetMax = new Vector2(-22f, 0f);
		viewport.AddComponent<RectMask2D>();

		var contentObject = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
		contentObject.layer = panel.layer;
		contentObject.transform.SetParent(viewportRect, false);
		m_Content = (RectTransform)contentObject.transform;
		m_Content.anchorMin = new Vector2(0f, 1f);
		m_Content.anchorMax = new Vector2(1f, 1f);
		m_Content.pivot = new Vector2(0.5f, 1f);
		m_Content.anchoredPosition = new Vector2(0f, 0f);

		var grid = contentObject.GetComponent<GridLayoutGroup>();
		grid.cellSize = new Vector2(490f, 150f);
		grid.spacing = new Vector2(16f, 16f);
		grid.padding = new RectOffset(16, 16, 10, 10);
		grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		grid.constraintCount = 2;
		grid.childAlignment = TextAnchor.UpperCenter;

		var sizeFitter = contentObject.GetComponent<ContentSizeFitter>();
		sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

		var scrollbar = CreateScrollbar(scrollViewRect);

		m_ScrollRect = scrollViewObject.GetComponent<ScrollRect>();
		m_ScrollRect.content = m_Content;
		m_ScrollRect.viewport = viewportRect;
		m_ScrollRect.horizontal = false;
		m_ScrollRect.vertical = true;
		m_ScrollRect.movementType = ScrollRect.MovementType.Elastic;
		m_ScrollRect.scrollSensitivity = 30f;
		m_ScrollRect.verticalScrollbar = scrollbar;
		m_ScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
	}

	/// <summary>
	/// 세로 스크롤바를 생성한다.
	/// </summary>
	private Scrollbar CreateScrollbar(Transform parent)
	{
		var scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
		scrollbarObject.layer = gameObject.layer;
		scrollbarObject.transform.SetParent(parent, false);
		var scrollbarRect = (RectTransform)scrollbarObject.transform;
		scrollbarRect.anchorMin = new Vector2(1f, 0f);
		scrollbarRect.anchorMax = new Vector2(1f, 1f);
		scrollbarRect.pivot = new Vector2(1f, 0.5f);
		scrollbarRect.sizeDelta = new Vector2(16f, 0f);
		scrollbarRect.anchoredPosition = new Vector2(0f, 0f);
		scrollbarObject.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.07f, 0.6f);

		var handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
		handleObject.layer = gameObject.layer;
		handleObject.transform.SetParent(scrollbarRect, false);
		var handleRect = (RectTransform)handleObject.transform;
		handleRect.anchorMin = new Vector2(0f, 0f);
		handleRect.anchorMax = new Vector2(1f, 1f);
		handleRect.offsetMin = new Vector2(2f, 2f);
		handleRect.offsetMax = new Vector2(-2f, -2f);
		handleObject.GetComponent<Image>().color = new Color(0.4f, 0.45f, 0.55f, 1f);

		var scrollbar = scrollbarObject.GetComponent<Scrollbar>();
		scrollbar.direction = Scrollbar.Direction.BottomToTop;
		scrollbar.handleRect = handleRect;
		scrollbar.targetGraphic = handleObject.GetComponent<Image>();
		return scrollbar;
	}

	/// <summary>
	/// 인벤토리 항목 선택 시 함선의 빈 칸(슬롯)을 표시한다(배치 모드 시작).
	/// 실제 부착은 표시된 슬롯을 탭할 때 이뤄진다.
	/// </summary>
	private void OnSelectModule(int instanceId, ModuleType type)
	{
		if (m_ShipBuilder == null || m_PlayerState == null)
		{
			return;
		}

		if (!m_PlayerState.ContainsModule(instanceId))
		{
			return;
		}

		m_ShipBuilder.BeginPlacement(instanceId, type);
	}

	/// <summary>
	/// 보유 모듈 셀들을 다시 생성한다.
	/// </summary>
	private void Refresh()
	{
		if (m_PlayerState == null || m_Content == null)
		{
			return;
		}

		for (int index = m_Content.childCount - 1; index >= 0; index--)
		{
			Destroy(m_Content.GetChild(index).gameObject);
		}

		var modules = m_PlayerState.Modules;
		for (int index = 0; index < modules.Count; index++)
		{
			CreateCell(modules[index]);
		}
	}

	/// <summary>
	/// 모듈 한 개의 드래그 가능한 셀을 생성한다.
	/// </summary>
	private void CreateCell(ModuleInstance instance)
	{
		var definition = ModuleCatalog.Get(instance.Type);

		var cell = UiFactory.CreateImage("Cell_" + instance.Id, m_Content, new Color(0.16f, 0.18f, 0.22f, 1f));
		var button = cell.AddComponent<Button>();
		var capturedId = instance.Id;
		var capturedType = instance.Type;
		button.onClick.AddListener(() => OnSelectModule(capturedId, capturedType));

		var icon = UiFactory.CreateImage("Icon", cell.transform, definition.Color);
		icon.GetComponent<Image>().raycastTarget = false;
		var iconRect = (RectTransform)icon.transform;
		iconRect.anchorMin = new Vector2(0f, 0.5f);
		iconRect.anchorMax = new Vector2(0f, 0.5f);
		iconRect.pivot = new Vector2(0f, 0.5f);
		iconRect.sizeDelta = new Vector2(96f, 96f);
		iconRect.anchoredPosition = new Vector2(22f, 0f);

		var nameText = UiFactory.CreateText("Name", cell.transform, m_Font, definition.DisplayName, 32, Color.white, TextAnchor.MiddleLeft);
		nameText.raycastTarget = false;
		var nameRect = nameText.rectTransform;
		nameRect.anchorMin = new Vector2(0f, 0f);
		nameRect.anchorMax = new Vector2(1f, 1f);
		nameRect.offsetMin = new Vector2(140f, 0f);
		nameRect.offsetMax = new Vector2(-12f, 0f);
	}
}

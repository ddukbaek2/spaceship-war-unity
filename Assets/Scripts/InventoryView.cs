using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 인벤토리 표시(개조 화면). 보유 모듈을 썸네일 그리드로 보여준다.
/// 모듈을 탭하면 선택(선택 시 함선에 빈 칸 표시 + 상단에 정보 표시)되고,
/// 장착된 모듈을 탭하면 장착 해제된다. 선택/비선택, 장착/미장착 상태를 표시한다.
/// </summary>
public class InventoryView : MonoBehaviour
{
	private PlayerState m_PlayerState;
	private RectTransform m_Content;
	private ScrollRect m_ScrollRect;
	private TMPro.TMP_Text m_Header;
	private TMPro.TMP_Text m_InfoName;
	private TMPro.TMP_Text m_InfoStat;
	private GameObject m_EquipButton;
	private GameObject m_UnequipButton;

	/// <summary>
	/// 초기화됨.
	/// </summary>
	private void Start()
	{
		m_PlayerState = FindFirstObjectByType<PlayerState>();

		var canvasObject = GameObject.Find("UI/Canvas");
		var modifyScreen = canvasObject.transform.Find("Screens").Find("Screen_개조");
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
	/// 인벤토리 패널(정보 영역 + 썸네일 그리드)을 구성한다.
	/// </summary>
	private void Build(Transform modifyScreen)
	{
		var panel = UiFactory.CreateImage("InventoryPanel", modifyScreen, new Color(0.07f, 0.08f, 0.11f, 0.92f));
		var panelRect = (RectTransform)panel.transform;
		panelRect.anchorMin = new Vector2(0f, 0f);
		panelRect.anchorMax = new Vector2(1f, 0f);
		panelRect.pivot = new Vector2(0.5f, 0f);
		panelRect.offsetMin = new Vector2(8f, 0f);
		panelRect.offsetMax = new Vector2(-8f, 560f);

		var header = UiFactory.CreateText("Header", panel.transform, null, "인벤토리", 28, new Color(0.7f, 0.72f, 0.78f, 1f), TextAnchor.UpperCenter);
		header.raycastTarget = false;
		m_Header = header;
		var headerRect = header.rectTransform;
		headerRect.anchorMin = new Vector2(0f, 1f);
		headerRect.anchorMax = new Vector2(1f, 1f);
		headerRect.pivot = new Vector2(0.5f, 1f);
		headerRect.sizeDelta = new Vector2(0f, 44f);
		headerRect.anchoredPosition = new Vector2(0f, -6f);

		var infoPanel = UiFactory.CreateImage("InfoPanel", panel.transform, new Color(0.12f, 0.14f, 0.18f, 1f));
		var infoRect = (RectTransform)infoPanel.transform;
		infoRect.anchorMin = new Vector2(0f, 1f);
		infoRect.anchorMax = new Vector2(1f, 1f);
		infoRect.pivot = new Vector2(0.5f, 1f);
		infoRect.sizeDelta = new Vector2(-16f, 120f);
		infoRect.anchoredPosition = new Vector2(0f, -52f);

		m_InfoName = UiFactory.CreateText("InfoName", infoPanel.transform, null, "", 34, Color.white, TextAnchor.UpperLeft);
		m_InfoName.raycastTarget = false;
		var infoNameRect = m_InfoName.rectTransform;
		infoNameRect.anchorMin = new Vector2(0f, 0f);
		infoNameRect.anchorMax = new Vector2(1f, 1f);
		infoNameRect.offsetMin = new Vector2(24f, 60f);
		infoNameRect.offsetMax = new Vector2(-260f, -12f);

		m_InfoStat = UiFactory.CreateText("InfoStat", infoPanel.transform, null, "", 26, new Color(0.7f, 0.85f, 1f, 1f), TextAnchor.LowerLeft);
		m_InfoStat.raycastTarget = false;
		var infoStatRect = m_InfoStat.rectTransform;
		infoStatRect.anchorMin = new Vector2(0f, 0f);
		infoStatRect.anchorMax = new Vector2(1f, 1f);
		infoStatRect.offsetMin = new Vector2(24f, 14f);
		infoStatRect.offsetMax = new Vector2(-260f, -64f);

		m_EquipButton = CreateInfoButton(infoPanel.transform, "장착", new Color(0.2f, 0.7f, 0.35f, 1f), OnEquip);
		m_UnequipButton = CreateInfoButton(infoPanel.transform, "해제", new Color(0.75f, 0.3f, 0.3f, 1f), OnUnequip);

		var scrollViewObject = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
		scrollViewObject.layer = panel.layer;
		scrollViewObject.transform.SetParent(panel.transform, false);
		var scrollViewRect = (RectTransform)scrollViewObject.transform;
		scrollViewRect.anchorMin = new Vector2(0f, 0f);
		scrollViewRect.anchorMax = new Vector2(1f, 1f);
		scrollViewRect.offsetMin = new Vector2(8f, 8f);
		scrollViewRect.offsetMax = new Vector2(-8f, -184f);

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

		var grid = contentObject.GetComponent<GridLayoutGroup>();
		grid.cellSize = new Vector2(110f, 110f);
		grid.spacing = new Vector2(8f, 8f);
		grid.padding = new RectOffset(8, 8, 8, 8);
		grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		grid.constraintCount = 8;
		grid.childAlignment = TextAnchor.UpperLeft;

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
	/// 모듈 탭 처리. 장착이면 해제, 미장착이면 선택(토글).
	/// </summary>
	private void OnTapModule(int instanceId, bool equipped)
	{
		if (m_PlayerState == null)
		{
			return;
		}

		m_PlayerState.SelectModule(instanceId);
	}

	/// <summary>
	/// 그리드와 정보 영역을 갱신한다.
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
			CreateThumbnail(modules[index]);
		}

		RefreshHeader();
		RefreshInfo();
	}

	/// <summary>
	/// 헤더에 현재 장착 상태 기준 전투력을 표시한다.
	/// </summary>
	private void RefreshHeader()
	{
		if (m_Header == null)
		{
			return;
		}

		var equipped = m_PlayerState.GetEquipped();
		var power = ComputePower(equipped);
		m_Header.text = "인벤토리    |    전투력 " + power;
	}

	/// <summary>
	/// 장착 배치로부터 전투력을 계산한다(전투 컨트롤러와 동일 기준).
	/// </summary>
	private int ComputePower(System.Collections.Generic.List<ModulePlacement> layout)
	{
		var attack = 2;
		var health = 60;
		var speed = 1;
		for (int index = 0; index < layout.Count; index++)
		{
			var definition = ModuleCatalog.Get(layout[index].Type);
			attack += definition.Attack;
			health += definition.Health;
			speed += definition.Speed;
		}

		return attack * 3 + health + speed * 2;
	}

	/// <summary>
	/// 모듈 썸네일 한 칸을 생성한다(선택/장착 상태 표시).
	/// </summary>
	private void CreateThumbnail(ModuleInstance instance)
	{
		var definition = ModuleCatalog.Get(instance.Type);
		var isSelected = m_PlayerState.SelectedId == instance.Id;

		var cell = UiFactory.CreateImage("Cell_" + instance.Id, m_Content, new Color(0f, 0f, 0f, 0f));
		var button = cell.AddComponent<Button>();
		var capturedId = instance.Id;
		var capturedEquipped = instance.Equipped;
		button.onClick.AddListener(() => OnTapModule(capturedId, capturedEquipped));

		if (isSelected)
		{
			var outline = UiFactory.CreateImage("Outline", cell.transform, new Color(1f, 0.92f, 0.4f, 1f));
			outline.GetComponent<Image>().raycastTarget = false;
			var outlineRect = (RectTransform)outline.transform;
			outlineRect.anchorMin = new Vector2(0f, 0f);
			outlineRect.anchorMax = new Vector2(1f, 1f);
			outlineRect.offsetMin = new Vector2(-5f, -5f);
			outlineRect.offsetMax = new Vector2(5f, 5f);
		}

		var iconColor = instance.Equipped ? definition.Color * 0.4f : definition.Color;
		iconColor.a = 1f;
		var icon = UiFactory.CreateImage("Icon", cell.transform, iconColor);
		icon.GetComponent<Image>().raycastTarget = false;
		var iconRect = (RectTransform)icon.transform;
		iconRect.anchorMin = new Vector2(0f, 0f);
		iconRect.anchorMax = new Vector2(1f, 1f);
		iconRect.offsetMin = new Vector2(6f, 6f);
		iconRect.offsetMax = new Vector2(-6f, -6f);

		var nameText = UiFactory.CreateText("Name", icon.transform, null, definition.DisplayName, 24, Color.white, TextAnchor.UpperCenter);
		nameText.raycastTarget = false;
		var nameRect = nameText.rectTransform;
		nameRect.anchorMin = new Vector2(0f, 0.55f);
		nameRect.anchorMax = new Vector2(1f, 1f);
		nameRect.offsetMin = new Vector2(0f, 0f);
		nameRect.offsetMax = new Vector2(0f, -6f);

		if (instance.Equipped)
		{
			var badge = UiFactory.CreateText("Badge", icon.transform, null, "장착됨", 22, new Color(0.7f, 1f, 0.7f, 1f), TextAnchor.LowerCenter);
			badge.raycastTarget = false;
			var badgeRect = badge.rectTransform;
			badgeRect.anchorMin = new Vector2(0f, 0f);
			badgeRect.anchorMax = new Vector2(1f, 0.45f);
			badgeRect.offsetMin = new Vector2(0f, 4f);
			badgeRect.offsetMax = new Vector2(0f, 0f);
		}
	}

	/// <summary>
	/// 선택된 모듈 정보를 갱신한다.
	/// </summary>
	private void RefreshInfo()
	{
		var selectedId = m_PlayerState.SelectedId;
		var module = selectedId != -1 ? m_PlayerState.GetModule(selectedId) : null;

		if (module == null)
		{
			m_InfoName.text = "모듈 선택";
			m_InfoStat.text = "모듈을 눌러 선택한 뒤, 빈 칸을 탭하거나 장착 버튼을 누르세요.";
			ShowButtons(false, false);
			return;
		}

		var definition = ModuleCatalog.Get(module.Type);
		m_InfoName.text = definition.DisplayName + (module.Equipped ? "  (장착됨)" : "");
		m_InfoStat.text = BuildStatText(definition);
		ShowButtons(!module.Equipped, module.Equipped);
	}

	/// <summary>
	/// 장착/해제 버튼 표시를 설정한다.
	/// </summary>
	private void ShowButtons(bool equip, bool unequip)
	{
		if (m_EquipButton != null)
		{
			m_EquipButton.SetActive(equip);
		}

		if (m_UnequipButton != null)
		{
			m_UnequipButton.SetActive(unequip);
		}
	}

	/// <summary>
	/// 정보 영역 버튼을 생성한다.
	/// </summary>
	private GameObject CreateInfoButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
	{
		var buttonObject = UiFactory.CreateImage("Button_" + label, parent, color);
		var rect = (RectTransform)buttonObject.transform;
		rect.anchorMin = new Vector2(1f, 0.5f);
		rect.anchorMax = new Vector2(1f, 0.5f);
		rect.pivot = new Vector2(1f, 0.5f);
		rect.sizeDelta = new Vector2(210f, 84f);
		rect.anchoredPosition = new Vector2(-20f, 0f);
		buttonObject.AddComponent<Button>().onClick.AddListener(onClick);
		var text = UiFactory.CreateText("Label", buttonObject.transform, null, label, 36, Color.white, TextAnchor.MiddleCenter);
		text.raycastTarget = false;
		return buttonObject;
	}

	/// <summary>
	/// 선택한 미장착 모듈을 가장 가까운 빈 칸에 장착한다.
	/// </summary>
	private void OnEquip()
	{
		var id = m_PlayerState.SelectedId;
		if (id == -1)
		{
			return;
		}

		var module = m_PlayerState.GetModule(id);
		if (module != null && !module.Equipped)
		{
			m_PlayerState.TryEquipFirstAvailable(id);
		}
	}

	/// <summary>
	/// 선택한 장착 모듈을 해제한다.
	/// </summary>
	private void OnUnequip()
	{
		var id = m_PlayerState.SelectedId;
		if (id == -1)
		{
			return;
		}

		var module = m_PlayerState.GetModule(id);
		if (module != null && module.Equipped)
		{
			m_PlayerState.Unequip(id);
		}
	}

	/// <summary>
	/// 모듈 스탯 문자열을 만든다.
	/// </summary>
	private string BuildStatText(ModuleDefinition definition)
	{
		var content = "";
		if (definition.Attack != 0)
		{
			content += "공격 +" + definition.Attack + "   ";
		}

		if (definition.Health != 0)
		{
			content += "체력 +" + definition.Health + "   ";
		}

		if (definition.Armor != 0)
		{
			content += "방어 +" + definition.Armor + "   ";
		}

		if (definition.Speed != 0)
		{
			content += "이동 +" + definition.Speed + "   ";
		}

		if (definition.Range != 0)
		{
			content += "사거리 " + definition.Range;
		}

		return content;
	}
}

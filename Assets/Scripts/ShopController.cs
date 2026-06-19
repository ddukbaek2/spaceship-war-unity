using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 상점 컨트롤러. 모듈(9종)을 카드 형태의 스크롤 목록으로 진열하고 구매를 처리한다.
/// 구매 시 재화를 소비하고 인벤토리에 개별 모듈을 추가한다.
/// </summary>
public class ShopController : MonoBehaviour
{
	private const float CardHeight = 250f;
	private const float CardSpacing = 18f;

	private PlayerState m_PlayerState;
	private Font m_Font;

	/// <summary>
	/// 초기화됨.
	/// </summary>
	private void Start()
	{
		m_PlayerState = FindFirstObjectByType<PlayerState>();
		m_Font = UiFont.Default;

		var canvasObject = GameObject.Find("UI/Canvas");
		var shopScreen = canvasObject.transform.Find("Screens").Find("Screen_상점");
		Build(shopScreen);
	}

	/// <summary>
	/// 상점 화면을 구성한다.
	/// </summary>
	private void Build(Transform shopScreen)
	{
		for (int index = shopScreen.childCount - 1; index >= 0; index--)
		{
			DestroyImmediate(shopScreen.GetChild(index).gameObject);
		}

		var banner = UiFactory.CreateImage("Banner", shopScreen, new Color(0.18f, 0.14f, 0.28f, 1f));
		var bannerRect = (RectTransform)banner.transform;
		bannerRect.anchorMin = new Vector2(0f, 1f);
		bannerRect.anchorMax = new Vector2(1f, 1f);
		bannerRect.pivot = new Vector2(0.5f, 1f);
		bannerRect.sizeDelta = new Vector2(0f, 110f);
		UiFactory.CreateText("Title", banner.transform, m_Font, "★ 상점 ★", 56, new Color(1f, 0.92f, 0.5f, 1f), TextAnchor.MiddleCenter);

		var scrollViewObject = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
		scrollViewObject.layer = shopScreen.gameObject.layer;
		scrollViewObject.transform.SetParent(shopScreen, false);
		var scrollViewRect = (RectTransform)scrollViewObject.transform;
		scrollViewRect.anchorMin = new Vector2(0f, 0f);
		scrollViewRect.anchorMax = new Vector2(1f, 1f);
		scrollViewRect.offsetMin = new Vector2(12f, 12f);
		scrollViewRect.offsetMax = new Vector2(-12f, -122f);

		var viewport = UiFactory.CreateImage("Viewport", scrollViewRect, new Color(0f, 0f, 0f, 0.12f));
		var viewportRect = (RectTransform)viewport.transform;
		viewportRect.anchorMin = new Vector2(0f, 0f);
		viewportRect.anchorMax = new Vector2(1f, 1f);
		viewportRect.offsetMin = new Vector2(0f, 0f);
		viewportRect.offsetMax = new Vector2(0f, 0f);
		viewport.AddComponent<RectMask2D>();

		var contentObject = new GameObject("Content", typeof(RectTransform));
		contentObject.layer = shopScreen.gameObject.layer;
		contentObject.transform.SetParent(viewportRect, false);
		var content = (RectTransform)contentObject.transform;
		content.anchorMin = new Vector2(0f, 1f);
		content.anchorMax = new Vector2(1f, 1f);
		content.pivot = new Vector2(0.5f, 1f);

		var definitions = ModuleCatalog.Definitions;
		var topPad = 8f;
		var stride = CardHeight + CardSpacing;
		content.sizeDelta = new Vector2(0f, topPad * 2f + definitions.Length * stride - CardSpacing);

		var scrollRect = scrollViewObject.GetComponent<ScrollRect>();
		scrollRect.content = content;
		scrollRect.viewport = viewportRect;
		scrollRect.horizontal = false;
		scrollRect.vertical = true;
		scrollRect.movementType = ScrollRect.MovementType.Elastic;
		scrollRect.scrollSensitivity = 30f;

		for (int index = 0; index < definitions.Length; index++)
		{
			CreateCard(definitions[index], content, index, topPad, stride);
		}
	}

	/// <summary>
	/// 모듈 카드를 생성한다.
	/// </summary>
	private void CreateCard(ModuleDefinition definition, Transform parent, int index, float topPad, float stride)
	{
		var frame = UiFactory.CreateImage("Card_" + definition.DisplayName, parent, definition.Color);
		var frameRect = (RectTransform)frame.transform;
		frameRect.anchorMin = new Vector2(0f, 1f);
		frameRect.anchorMax = new Vector2(1f, 1f);
		frameRect.pivot = new Vector2(0.5f, 1f);
		frameRect.sizeDelta = new Vector2(-16f, CardHeight);
		frameRect.anchoredPosition = new Vector2(0f, -(topPad + index * stride));

		var inner = UiFactory.CreateImage("Inner", frame.transform, new Color(0.11f, 0.12f, 0.16f, 1f));
		var innerRect = (RectTransform)inner.transform;
		innerRect.anchorMin = new Vector2(0f, 0f);
		innerRect.anchorMax = new Vector2(1f, 1f);
		innerRect.offsetMin = new Vector2(7f, 7f);
		innerRect.offsetMax = new Vector2(-7f, -7f);

		var icon = UiFactory.CreateImage("Icon", inner.transform, definition.Color);
		var iconRect = (RectTransform)icon.transform;
		iconRect.anchorMin = new Vector2(0f, 0.5f);
		iconRect.anchorMax = new Vector2(0f, 0.5f);
		iconRect.pivot = new Vector2(0f, 0.5f);
		iconRect.sizeDelta = new Vector2(150f, 150f);
		iconRect.anchoredPosition = new Vector2(28f, 0f);

		var name = UiFactory.CreateText("Name", inner.transform, m_Font, definition.DisplayName, 44, Color.white, TextAnchor.UpperLeft);
		var nameRect = name.rectTransform;
		nameRect.anchorMin = new Vector2(0f, 1f);
		nameRect.anchorMax = new Vector2(1f, 1f);
		nameRect.pivot = new Vector2(0f, 1f);
		nameRect.sizeDelta = new Vector2(-220f, 60f);
		nameRect.anchoredPosition = new Vector2(210f, -24f);

		var stat = UiFactory.CreateText("Stat", inner.transform, m_Font, BuildStatText(definition), 30, new Color(0.7f, 0.85f, 1f, 1f), TextAnchor.UpperLeft);
		var statRect = stat.rectTransform;
		statRect.anchorMin = new Vector2(0f, 1f);
		statRect.anchorMax = new Vector2(1f, 1f);
		statRect.pivot = new Vector2(0f, 1f);
		statRect.sizeDelta = new Vector2(-220f, 90f);
		statRect.anchoredPosition = new Vector2(210f, -92f);

		var price = UiFactory.CreateText("Price", inner.transform, m_Font, "재화 " + definition.Price, 34, new Color(1f, 0.9f, 0.5f, 1f), TextAnchor.LowerLeft);
		var priceRect = price.rectTransform;
		priceRect.anchorMin = new Vector2(0f, 0f);
		priceRect.anchorMax = new Vector2(0f, 0f);
		priceRect.pivot = new Vector2(0f, 0f);
		priceRect.sizeDelta = new Vector2(300f, 50f);
		priceRect.anchoredPosition = new Vector2(210f, 24f);

		var buyObject = UiFactory.CreateImage("Buy", inner.transform, new Color(0.2f, 0.7f, 0.35f, 1f));
		var buyRect = (RectTransform)buyObject.transform;
		buyRect.anchorMin = new Vector2(1f, 0f);
		buyRect.anchorMax = new Vector2(1f, 0f);
		buyRect.pivot = new Vector2(1f, 0f);
		buyRect.sizeDelta = new Vector2(240f, 96f);
		buyRect.anchoredPosition = new Vector2(-24f, 24f);
		var buyButton = buyObject.AddComponent<Button>();
		var buyLabel = UiFactory.CreateText("Label", buyObject.transform, m_Font, "구매", 38, Color.white, TextAnchor.MiddleCenter);
		buyLabel.raycastTarget = false;

		var capturedType = definition.Type;
		var capturedPrice = definition.Price;
		buyButton.onClick.AddListener(() => Buy(capturedType, capturedPrice));
	}

	/// <summary>
	/// 모듈 정의의 스탯 표시 문자열을 만든다.
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

	/// <summary>
	/// 모듈을 구매한다. 재화가 부족하면 아무 일도 하지 않는다.
	/// </summary>
	private void Buy(ModuleType type, int price)
	{
		if (m_PlayerState == null)
		{
			return;
		}

		if (m_PlayerState.TrySpendCurrency(price))
		{
			m_PlayerState.AddModule(type);
		}
	}
}

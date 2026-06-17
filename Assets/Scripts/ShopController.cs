using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 상점 컨트롤러. 상점 화면에 모듈 목록을 구성하고 구매를 처리한다.
/// 구매 시 재화를 소비하고 인벤토리에 모듈을 추가한다.
/// </summary>
public class ShopController : MonoBehaviour
{
	private PlayerState m_PlayerState;
	private Font m_Font;

	/// <summary>
	/// 초기화됨.
	/// </summary>
	private void Start()
	{
		m_PlayerState = FindFirstObjectByType<PlayerState>();
		m_Font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 36);

		var canvasObject = GameObject.Find("UI/Canvas");
		var screensTransform = canvasObject.transform.Find("Screens");
		var shopScreen = screensTransform.Find("Screen_상점");
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

		var header = UiFactory.CreateText("Header", shopScreen, m_Font, "상점", 56, new Color(0.85f, 0.88f, 0.95f, 1f), TextAnchor.UpperCenter);
		var headerRect = header.rectTransform;
		headerRect.anchorMin = new Vector2(0f, 1f);
		headerRect.anchorMax = new Vector2(1f, 1f);
		headerRect.pivot = new Vector2(0.5f, 1f);
		headerRect.sizeDelta = new Vector2(0f, 80f);
		headerRect.anchoredPosition = new Vector2(0f, -30f);

		var definitions = ModuleCatalog.Definitions;
		for (int index = 0; index < definitions.Length; index++)
		{
			CreateRow(definitions[index], shopScreen, index);
		}
	}

	/// <summary>
	/// 모듈 한 줄(이름/스탯/가격/구매)을 생성한다.
	/// </summary>
	private void CreateRow(ModuleDefinition definition, Transform parent, int index)
	{
		var row = UiFactory.CreateImage("Row_" + definition.DisplayName, parent, new Color(0.12f, 0.14f, 0.18f, 1f));
		var rowRect = (RectTransform)row.transform;
		rowRect.anchorMin = new Vector2(0f, 1f);
		rowRect.anchorMax = new Vector2(1f, 1f);
		rowRect.pivot = new Vector2(0.5f, 1f);
		rowRect.sizeDelta = new Vector2(-48f, 150f);
		rowRect.anchoredPosition = new Vector2(0f, -150f - index * 166f);

		var infoContent = BuildStatText(definition);
		var info = UiFactory.CreateText("Info", row.transform, m_Font, infoContent, 32, Color.white, TextAnchor.MiddleLeft);
		var infoRect = info.rectTransform;
		infoRect.anchorMin = new Vector2(0f, 0f);
		infoRect.anchorMax = new Vector2(0.58f, 1f);
		infoRect.offsetMin = new Vector2(28f, 0f);
		infoRect.offsetMax = new Vector2(0f, 0f);

		var price = UiFactory.CreateText("Price", row.transform, m_Font, "재화 " + definition.Price, 32, new Color(1f, 0.85f, 0.4f, 1f), TextAnchor.MiddleRight);
		var priceRect = price.rectTransform;
		priceRect.anchorMin = new Vector2(0.55f, 0f);
		priceRect.anchorMax = new Vector2(0.78f, 1f);

		var buttonObject = new GameObject("Buy", typeof(RectTransform), typeof(Image), typeof(Button));
		buttonObject.layer = parent.gameObject.layer;
		var buttonRect = (RectTransform)buttonObject.transform;
		buttonRect.SetParent(row.transform, false);
		buttonRect.anchorMin = new Vector2(0.8f, 0.18f);
		buttonRect.anchorMax = new Vector2(0.97f, 0.82f);
		buttonRect.offsetMin = new Vector2(0f, 0f);
		buttonRect.offsetMax = new Vector2(0f, 0f);
		buttonObject.GetComponent<Image>().color = new Color(0.15f, 0.5f, 0.55f, 1f);
		UiFactory.CreateText("Label", buttonObject.transform, m_Font, "구매", 32, Color.white, TextAnchor.MiddleCenter);

		var capturedType = definition.Type;
		var capturedPrice = definition.Price;
		buttonObject.GetComponent<Button>().onClick.AddListener(() => Buy(capturedType, capturedPrice));
	}

	/// <summary>
	/// 모듈 정의의 스탯 표시 문자열을 만든다.
	/// </summary>
	private string BuildStatText(ModuleDefinition definition)
	{
		var content = definition.DisplayName;
		if (definition.Attack != 0)
		{
			content += "  공격+" + definition.Attack;
		}

		if (definition.Health != 0)
		{
			content += "  체력+" + definition.Health;
		}

		if (definition.Speed != 0)
		{
			content += "  이동+" + definition.Speed;
		}

		if (definition.Range != 0)
		{
			content += "  사거리 " + definition.Range;
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

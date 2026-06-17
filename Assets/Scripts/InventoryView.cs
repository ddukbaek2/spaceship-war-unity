using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 인벤토리 표시. 개조 화면 하단에 보유 모듈 수량을 표시한다.
/// </summary>
public class InventoryView : MonoBehaviour
{
	private PlayerState m_PlayerState;
	private Font m_Font;
	private Text m_InventoryText;

	/// <summary>
	/// 초기화됨.
	/// </summary>
	private void Start()
	{
		m_PlayerState = FindFirstObjectByType<PlayerState>();
		m_Font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 32);

		var canvasObject = GameObject.Find("UI/Canvas");
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
	/// 인벤토리 바를 구성한다.
	/// </summary>
	private void Build(Transform modifyScreen)
	{
		var bar = UiFactory.CreateImage("InventoryBar", modifyScreen, new Color(0.08f, 0.09f, 0.12f, 0.9f));
		var barRect = (RectTransform)bar.transform;
		barRect.anchorMin = new Vector2(0f, 0f);
		barRect.anchorMax = new Vector2(1f, 0f);
		barRect.pivot = new Vector2(0.5f, 0f);
		barRect.sizeDelta = new Vector2(0f, 96f);
		barRect.anchoredPosition = new Vector2(0f, 0f);

		m_InventoryText = UiFactory.CreateText("InventoryText", bar.transform, m_Font, "", 34, new Color(0.85f, 0.88f, 0.95f, 1f), TextAnchor.MiddleCenter);
	}

	/// <summary>
	/// 보유 수량 표시를 갱신한다.
	/// </summary>
	private void Refresh()
	{
		if (m_PlayerState == null || m_InventoryText == null)
		{
			return;
		}

		var weaponCount = m_PlayerState.GetModuleCount(ModuleType.Weapon);
		var armorCount = m_PlayerState.GetModuleCount(ModuleType.Armor);
		var engineCount = m_PlayerState.GetModuleCount(ModuleType.Engine);
		m_InventoryText.text = "인벤토리    무기 " + weaponCount + "    장갑 " + armorCount + "    추진체 " + engineCount;
	}
}

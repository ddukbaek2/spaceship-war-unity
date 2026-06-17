using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// uGUI 요소 생성 헬퍼.
/// </summary>
public static class UiFactory
{
	/// <summary>
	/// 이미지 오브젝트를 생성한다.
	/// </summary>
	public static GameObject CreateImage(string name, Transform parent, Color color)
	{
		var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
		imageObject.layer = parent.gameObject.layer;
		imageObject.transform.SetParent(parent, false);
		imageObject.GetComponent<Image>().color = color;
		return imageObject;
	}

	/// <summary>
	/// 부모 전체에 펼쳐진 텍스트 오브젝트를 생성한다.
	/// </summary>
	public static Text CreateText(string name, Transform parent, Font font, string content, int fontSize, Color color, TextAnchor anchor)
	{
		var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
		textObject.layer = parent.gameObject.layer;
		var rectTransform = (RectTransform)textObject.transform;
		rectTransform.SetParent(parent, false);
		rectTransform.anchorMin = new Vector2(0f, 0f);
		rectTransform.anchorMax = new Vector2(1f, 1f);
		rectTransform.offsetMin = new Vector2(0f, 0f);
		rectTransform.offsetMax = new Vector2(0f, 0f);

		var text = textObject.GetComponent<Text>();
		text.text = content;
		text.font = font;
		text.fontSize = fontSize;
		text.alignment = anchor;
		text.color = color;
		text.horizontalOverflow = HorizontalWrapMode.Overflow;
		text.verticalOverflow = VerticalWrapMode.Overflow;
		return text;
	}
}

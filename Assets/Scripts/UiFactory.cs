using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// uGUI 요소 생성 헬퍼. 텍스트는 TextMeshPro(SDF)로 생성한다.
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
		var image = imageObject.GetComponent<Image>();
		image.color = color;
		var roundedSprite = UiSprite.RoundedCorner;
		if (roundedSprite != null)
		{
			image.sprite = roundedSprite;
			image.type = Image.Type.Sliced;
		}

		return imageObject;
	}

	/// <summary>
	/// 라운드 코너 이미지 오브젝트를 생성한다(9-슬라이스).
	/// </summary>
	public static GameObject CreateRoundedImage(string name, Transform parent, Color color)
	{
		var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
		imageObject.layer = parent.gameObject.layer;
		imageObject.transform.SetParent(parent, false);
		var image = imageObject.GetComponent<Image>();
		image.color = color;
		image.sprite = UiSprite.RoundedCorner;
		image.type = Image.Type.Sliced;
		return imageObject;
	}

	/// <summary>
	/// 부모 전체에 펼쳐진 TextMeshPro 텍스트 오브젝트를 생성한다.
	/// (font 인자는 호환을 위해 유지하나 TMP 폰트를 사용한다.)
	/// </summary>
	public static TMP_Text CreateText(string name, Transform parent, Font font, string content, int fontSize, Color color, TextAnchor anchor)
	{
		var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
		textObject.layer = parent.gameObject.layer;
		var rectTransform = (RectTransform)textObject.transform;
		rectTransform.SetParent(parent, false);
		rectTransform.anchorMin = new Vector2(0f, 0f);
		rectTransform.anchorMax = new Vector2(1f, 1f);
		rectTransform.offsetMin = new Vector2(0f, 0f);
		rectTransform.offsetMax = new Vector2(0f, 0f);

		var text = textObject.GetComponent<TextMeshProUGUI>();
		text.font = TmpFont.Default;
		text.text = content;
		text.fontSize = fontSize;
		text.color = color;
		text.alignment = ToAlignment(anchor);
		text.enableWordWrapping = false;
		text.overflowMode = TextOverflowModes.Overflow;
		return text;
	}

	/// <summary>
	/// TextAnchor 를 TMP 정렬로 변환한다.
	/// </summary>
	private static TextAlignmentOptions ToAlignment(TextAnchor anchor)
	{
		switch (anchor)
		{
			case TextAnchor.UpperLeft:
			{
				return TextAlignmentOptions.TopLeft;
			}
			case TextAnchor.UpperCenter:
			{
				return TextAlignmentOptions.Top;
			}
			case TextAnchor.UpperRight:
			{
				return TextAlignmentOptions.TopRight;
			}
			case TextAnchor.MiddleLeft:
			{
				return TextAlignmentOptions.Left;
			}
			case TextAnchor.MiddleCenter:
			{
				return TextAlignmentOptions.Center;
			}
			case TextAnchor.MiddleRight:
			{
				return TextAlignmentOptions.Right;
			}
			case TextAnchor.LowerLeft:
			{
				return TextAlignmentOptions.BottomLeft;
			}
			case TextAnchor.LowerCenter:
			{
				return TextAlignmentOptions.Bottom;
			}
			case TextAnchor.LowerRight:
			{
				return TextAlignmentOptions.BottomRight;
			}
			default:
			{
				return TextAlignmentOptions.Center;
			}
		}
	}
}

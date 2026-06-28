using UnityEngine;


/// <summary>
/// UI 공용 스프라이트. 절차적으로 생성한 라운드 코너 스프라이트를 제공한다.
/// </summary>
public static class UiSprite
{
	private static Sprite s_RoundedCorner;
	private static Sprite s_Circle;

	/// <summary>
	/// 라운드 코너 스프라이트(9-슬라이스). Image.type=Sliced 로 사용한다.
	/// </summary>
	public static Sprite RoundedCorner
	{
		get
		{
			if (s_RoundedCorner == null)
			{
				s_RoundedCorner = Resources.Load<Sprite>("UI/RoundedCorner");
			}

			return s_RoundedCorner;
		}
	}

	/// <summary>
	/// 절차 생성한 흰색 원형 스프라이트(가장자리 안티에일리어싱). Image.type=Simple 로 사용한다.
	/// 색은 Image.color 로 입힌다.
	/// </summary>
	public static Sprite Circle
	{
		get
		{
			if (s_Circle == null)
			{
				s_Circle = CreateCircle(128);
			}

			return s_Circle;
		}
	}

	/// <summary>
	/// 지정 해상도의 흰색 원 스프라이트를 생성한다(가장자리 1px 안티에일리어싱).
	/// </summary>
	private static Sprite CreateCircle(int size)
	{
		var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = FilterMode.Bilinear;

		var center = (size - 1) * 0.5f;
		var radius = size * 0.5f - 1f;
		var pixels = new Color32[size * size];
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				var deltaX = x - center;
				var deltaY = y - center;
				var distance = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
				var alpha = Mathf.Clamp01(radius - distance + 0.5f);
				var value = (byte)(alpha * 255f);
				pixels[y * size + x] = new Color32(255, 255, 255, value);
			}
		}

		texture.SetPixels32(pixels);
		texture.Apply();

		var rect = new Rect(0f, 0f, size, size);
		var pivot = new Vector2(0.5f, 0.5f);
		return Sprite.Create(texture, rect, pivot);
	}
}

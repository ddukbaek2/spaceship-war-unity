using UnityEngine;


/// <summary>
/// UI 공용 스프라이트. 절차적으로 생성한 라운드 코너 스프라이트를 제공한다.
/// </summary>
public static class UiSprite
{
	private static Sprite s_RoundedCorner;

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
}

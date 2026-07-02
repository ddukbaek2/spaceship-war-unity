using TMPro;
using UnityEngine;


/// <summary>
/// TextMeshPro 공용 폰트. 게임 문자를 미리 구운 정적 SDF 폰트 에셋(Resources)을 로드한다.
/// 런타임에 폰트를 생성하지 않으므로 플레이 모드/빌드에서 글리프가 사라지지 않는다.
/// </summary>
public static class TmpFont
{
	private static TMP_FontAsset s_Default;

	/// <summary>
	/// 기본 TMP 폰트(경기천년바탕 Regular, 정적 SDF).
	/// </summary>
	public static TMP_FontAsset Default
	{
		get
		{
			if (s_Default == null)
			{
				s_Default = Resources.Load<TMP_FontAsset>("Fonts/GyeonggiBatang-SDF");
			}

			return s_Default;
		}
	}
}

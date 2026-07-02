using UnityEngine;


/// <summary>
/// UI 공용 폰트. WebGL에서도 동작하도록 OS 동적 폰트 대신
/// 프로젝트에 포함된 폰트 에셋(Resources)을 사용한다.
/// </summary>
public static class UiFont
{
	private static Font s_Default;

	/// <summary>
	/// 기본 폰트(한글 지원).
	/// </summary>
	public static Font Default
	{
		get
		{
			if (s_Default == null)
			{
				s_Default = Resources.Load<Font>("Fonts/NotoSansKR-Regular");
			}

			return s_Default;
		}
	}
}

using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;


/// <summary>
/// TextMeshPro 공용 폰트. 프로젝트에 포함된 한글 ttf로 동적 SDF 폰트 에셋을 만든다.
/// (글리프를 런타임에 추가하므로 한글 전체를 미리 굽지 않아도 되고 WebGL에서도 동작)
/// </summary>
public static class TmpFont
{
	private static TMP_FontAsset s_Default;

	/// <summary>
	/// 기본 TMP 폰트(한글 지원, 동적 SDF).
	/// </summary>
	public static TMP_FontAsset Default
	{
		get
		{
			if (s_Default == null)
			{
				var sourceFont = Resources.Load<Font>("Fonts/malgun");
				s_Default = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
			}

			return s_Default;
		}
	}
}

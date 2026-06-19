using UnityEngine;


/// <summary>
/// 런타임 머티리얼 생성 헬퍼. 셰이더를 안전하게 찾고(없으면 폴백) 속성을 가드한다.
/// (WebGL 등에서 셰이더가 스트립되어 Shader.Find가 null을 반환해도 크래시하지 않도록)
/// </summary>
public static class MaterialFactory
{
	private static Shader s_LitShader;

	/// <summary>
	/// URP Lit 셰이더(없으면 폴백).
	/// </summary>
	private static Shader LitShader
	{
		get
		{
			if (s_LitShader == null)
			{
				s_LitShader = Shader.Find("Universal Render Pipeline/Lit");
			}

			if (s_LitShader == null)
			{
				s_LitShader = Shader.Find("Universal Render Pipeline/Simple Lit");
			}

			if (s_LitShader == null)
			{
				s_LitShader = Shader.Find("Sprites/Default");
			}

			return s_LitShader;
		}
	}

	/// <summary>
	/// Lit 머티리얼을 생성한다(이미션/금속/매끄러움/투명, 셰이더·속성 가드).
	/// </summary>
	public static Material CreateLit(Color baseColor, Color emission, float metallic, float smoothness, bool transparent)
	{
		var shader = LitShader;
		var material = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));

		if (material.HasProperty("_BaseColor"))
		{
			material.SetColor("_BaseColor", baseColor);
		}

		material.color = baseColor;

		if (material.HasProperty("_Metallic"))
		{
			material.SetFloat("_Metallic", metallic);
		}

		if (material.HasProperty("_Smoothness"))
		{
			material.SetFloat("_Smoothness", smoothness);
		}

		if (emission.maxColorComponent > 0f && material.HasProperty("_EmissionColor"))
		{
			material.EnableKeyword("_EMISSION");
			material.SetColor("_EmissionColor", emission);
			material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
		}

		if (transparent && material.HasProperty("_Surface"))
		{
			material.SetFloat("_Surface", 1f);
			material.SetFloat("_Blend", 0f);
			material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			material.SetFloat("_ZWrite", 0f);
			material.DisableKeyword("_ALPHATEST_ON");
			material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
		}

		return material;
	}
}

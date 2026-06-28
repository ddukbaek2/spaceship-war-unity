using UnityEngine;


/// <summary>
/// 별이 박힌 우주 스카이박스를 적용한다. Resources의 절차 생성 머티리얼(Skybox/StarfieldSkybox)을
/// RenderSettings.skybox 로 설정한다. 카메라의 Clear Flags 가 Skybox 여야 화면에 보인다.
/// </summary>
public static class SkyboxFactory
{
	private static Material s_Skybox;

	/// <summary>
	/// 별 스카이박스를 현재 씬에 적용한다(머티리얼 로드 실패 시 무시).
	/// </summary>
	public static void Apply()
	{
		if (s_Skybox == null)
		{
			s_Skybox = Resources.Load<Material>("Skybox/StarfieldSkybox");
		}

		if (s_Skybox == null)
		{
			return;
		}

		RenderSettings.skybox = s_Skybox;
		DynamicGI.UpdateEnvironment();
	}
}

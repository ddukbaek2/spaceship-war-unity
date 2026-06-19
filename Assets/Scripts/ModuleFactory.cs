using UnityEngine;


/// <summary>
/// 모듈 팩토리. Resources/Modules 의 프리팹을 로드해 인스턴스화한다.
/// </summary>
public static class ModuleFactory
{
	private const float OutlineScale = 1.06f;
	private static Material s_OutlineMaterial;

	/// <summary>
	/// 코어 모듈 인스턴스를 만든다.
	/// </summary>
	public static GameObject CreateCore(Transform parent, Vector3 localPosition)
	{
		return Spawn("Core", parent, localPosition);
	}

	/// <summary>
	/// 종류별 모듈 인스턴스를 만든다.
	/// </summary>
	public static GameObject CreateModule(ModuleType type, Transform parent, Vector3 localPosition)
	{
		return Spawn(type.ToString(), parent, localPosition);
	}

	/// <summary>
	/// 프리팹을 로드해 부모 아래에 생성한다.
	/// </summary>
	private static GameObject Spawn(string prefabName, Transform parent, Vector3 localPosition)
	{
		var prefab = Resources.Load<GameObject>("Modules/" + prefabName);
		if (prefab == null)
		{
			return null;
		}

		var instance = Object.Instantiate(prefab, parent);
		instance.transform.localPosition = localPosition;
		instance.transform.localRotation = Quaternion.identity;
		AddOutline(instance);
		return instance;
	}

	/// <summary>
	/// 모듈의 각 메시에 검은색 외곽선(인버티드 헐)을 덧붙인다.
	/// </summary>
	private static void AddOutline(GameObject root)
	{
		var material = GetOutlineMaterial();
		if (material == null)
		{
			return;
		}

		var meshFilters = root.GetComponentsInChildren<MeshFilter>();
		for (int index = 0; index < meshFilters.Length; index++)
		{
			var meshFilter = meshFilters[index];
			if (meshFilter.sharedMesh == null)
			{
				continue;
			}

			var outlineObject = new GameObject("Outline");
			outlineObject.layer = meshFilter.gameObject.layer;
			var outlineTransform = outlineObject.transform;
			outlineTransform.SetParent(meshFilter.transform, false);
			outlineTransform.localPosition = Vector3.zero;
			outlineTransform.localRotation = Quaternion.identity;
			outlineTransform.localScale = Vector3.one * OutlineScale;

			var outlineFilter = outlineObject.AddComponent<MeshFilter>();
			outlineFilter.sharedMesh = meshFilter.sharedMesh;

			var outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
			outlineRenderer.sharedMaterial = material;
			outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			outlineRenderer.receiveShadows = false;
		}
	}

	/// <summary>
	/// 외곽선용 검은색 머티리얼(앞면 컬링)을 반환한다.
	/// </summary>
	private static Material GetOutlineMaterial()
	{
		if (s_OutlineMaterial != null)
		{
			return s_OutlineMaterial;
		}

		var material = MaterialFactory.CreateLit(Color.black, Color.black, 0f, 0f, false);
		if (material == null)
		{
			return null;
		}

		if (material.HasProperty("_Cull"))
		{
			material.SetFloat("_Cull", 1f);
		}

		s_OutlineMaterial = material;
		return material;
	}
}

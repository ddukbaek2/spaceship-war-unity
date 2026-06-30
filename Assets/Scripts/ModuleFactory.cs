using UnityEngine;


/// <summary>
/// 모듈 팩토리. Resources/Modules 의 프리팹을 로드해 인스턴스화한다.
/// </summary>
public static class ModuleFactory
{
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
		return instance;
	}
}

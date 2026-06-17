using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// 런타임 초기화 처리기.
/// </summary>
public class RuntimeInitializer
{
	/// <summary>
	/// 초기화.
	/// </summary>
	[RuntimeInitializeOnLoadMethod]
	private static void Initialize()
	{
		var scene = SceneManager.GetActiveScene();
		if (scene.name == "Scene")
		{
			Debug.Log("[RuntimeInitializer] Initialize()");
		}
	}
}

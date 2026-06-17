using UnityEngine;


/// <summary>
/// 공유 컴포넌트.
/// </summary>
public class SharedComponent<TComponent> : MonoBehaviour where TComponent : Component
{
	/// <summary>
	/// 공유 인스턴스.
	/// </summary>
	private static TComponent s_Instance;

	/// <summary>
	/// 공유 인스턴스.
	/// </summary>
	public static TComponent Instance
	{
		get
		{
			if (s_Instance == null)
			{
				Create();
			}

			return s_Instance;
		}
	}

	/// <summary>
	/// 생성됨.
	/// </summary>
	protected virtual void Awake()
	{

	}

	/// <summary>
	/// 초기화됨.
	/// </summary>
	protected virtual void Start()
	{
	}

	/// <summary>
	/// 파괴됨.
	/// </summary>
	protected virtual void OnDestroy()
	{
	}

	/// <summary>
	/// 찾기.
	/// </summary>
	/// <returns></returns>
	public static TComponent Find()
	{
		var component = GameObject.FindFirstObjectByType<TComponent>();
		return component;
	}

	/// <summary>
	/// 생성.
	/// </summary>
	public static void Create()
	{
		if (s_Instance == null)
		{
			s_Instance = Find();
			if (s_Instance != null)
			{
				GameObject.DontDestroyOnLoad(s_Instance.gameObject);
			}
		}

		if (s_Instance == null)
		{
			var type = typeof(TComponent);

			var obj = new GameObject(type.Name);
			s_Instance = obj.AddComponent<TComponent>();
			GameObject.DontDestroyOnLoad(obj);
			if (s_Instance != null)
			{
				GameObject.DontDestroyOnLoad(s_Instance.gameObject);
			}
		}
	}

	/// <summary>
	/// 해제.
	/// </summary>
	public static void Dispose()
	{
		if (s_Instance != null)
		{
			GameObject.DestroyImmediate(s_Instance.gameObject, true);
			s_Instance = null;
		}
	}
}
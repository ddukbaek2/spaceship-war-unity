using UnityEngine;


/// <summary>
/// 액터. 월드에 배치되는 모든 객체의 베이스.
/// </summary>
public class Actor : MonoBehaviour
{
	/// <summary>
	/// 생성됨.
	/// </summary>
	protected virtual void Awake()
	{
	}

	/// <summary>
	/// 파괴됨.
	/// </summary>
	protected virtual void OnDestroy()
	{
	}
}
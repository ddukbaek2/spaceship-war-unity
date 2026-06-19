using UnityEngine;


/// <summary>
/// 개별 모듈 인스턴스. 인벤토리에서 스택되지 않고 각각의 개체로 다룬다.
/// 장착 여부와 장착 위치(격자 좌표) 상태를 가진다.
/// </summary>
public class ModuleInstance
{
	public int Id;
	public ModuleType Type;
	public bool Equipped;
	public Vector2Int Coordinate;
}

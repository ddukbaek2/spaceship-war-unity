using System;
using UnityEngine;


/// <summary>
/// 모듈 테이블의 한 행(데이터).
/// </summary>
[Serializable]
public class ModuleRow
{
	public ModuleType Type;
	public ModuleCategory Category;
	public string DisplayName;
	public int Price;
	public int Attack;
	public int Health;
	public int Armor;
	public int Speed;
	public int Range;
	public Color Color = Color.white;
}


/// <summary>
/// 모듈 데이터 테이블(ScriptableObject). 모든 모듈 정의를 행으로 보관한다.
/// </summary>
[CreateAssetMenu(fileName = "ModuleTable", menuName = "Spaceship/Module Table")]
public class ModuleTable : ScriptableObject
{
	[SerializeField] private ModuleRow[] m_Rows;

	/// <summary>
	/// 행 배열.
	/// </summary>
	public ModuleRow[] Rows
	{
		get { return m_Rows; }
	}
}

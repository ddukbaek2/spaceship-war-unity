using UnityEngine;


/// <summary>
/// 모듈 정의(종류별 이름/가격/스탯/색상).
/// </summary>
public struct ModuleDefinition
{
	public ModuleType Type;
	public string DisplayName;
	public int Price;
	public int Attack;
	public int Health;
	public int Speed;
	public int Range;
	public Color Color;
}


/// <summary>
/// 모듈 카탈로그. 종류별 정의를 제공한다.
/// </summary>
public static class ModuleCatalog
{
	/// <summary>
	/// 모든 모듈 정의.
	/// </summary>
	public static readonly ModuleDefinition[] Definitions = new ModuleDefinition[]
	{
		new ModuleDefinition { Type = ModuleType.Weapon, DisplayName = "무기", Price = 100, Attack = 10, Health = 10, Speed = 0, Range = 4, Color = new Color(0.9f, 0.4f, 0.4f, 1f) },
		new ModuleDefinition { Type = ModuleType.Armor, DisplayName = "장갑", Price = 80, Attack = 0, Health = 40, Speed = 0, Range = 0, Color = new Color(0.5f, 0.6f, 0.75f, 1f) },
		new ModuleDefinition { Type = ModuleType.Engine, DisplayName = "추진체", Price = 120, Attack = 0, Health = 10, Speed = 5, Range = 0, Color = new Color(0.95f, 0.7f, 0.35f, 1f) },
	};

	/// <summary>
	/// 종류로 정의를 찾는다.
	/// </summary>
	public static ModuleDefinition Get(ModuleType type)
	{
		for (int index = 0; index < Definitions.Length; index++)
		{
			if (Definitions[index].Type == type)
			{
				return Definitions[index];
			}
		}

		return Definitions[0];
	}
}

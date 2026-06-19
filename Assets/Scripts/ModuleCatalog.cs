using UnityEngine;


/// <summary>
/// 모듈 정의(종류별 카테고리/이름/가격/스탯/색상). 런타임 표현.
/// </summary>
public struct ModuleDefinition
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
	public Color Color;
}


/// <summary>
/// 모듈 카탈로그. 모듈 테이블(Resources/ModuleTable)을 읽어 정의를 제공한다.
/// 테이블이 없으면 기본값으로 대체한다.
/// </summary>
public static class ModuleCatalog
{
	private static ModuleDefinition[] s_Definitions;

	/// <summary>
	/// 모든 모듈 정의.
	/// </summary>
	public static ModuleDefinition[] Definitions
	{
		get
		{
			EnsureLoaded();
			return s_Definitions;
		}
	}

	/// <summary>
	/// 종류로 정의를 찾는다.
	/// </summary>
	public static ModuleDefinition Get(ModuleType type)
	{
		EnsureLoaded();
		for (int index = 0; index < s_Definitions.Length; index++)
		{
			if (s_Definitions[index].Type == type)
			{
				return s_Definitions[index];
			}
		}

		return s_Definitions[0];
	}

	/// <summary>
	/// 종류의 카테고리를 반환한다.
	/// </summary>
	public static ModuleCategory GetCategory(ModuleType type)
	{
		return Get(type).Category;
	}

	/// <summary>
	/// 정의를 로드한다(테이블 우선, 없으면 기본값).
	/// </summary>
	private static void EnsureLoaded()
	{
		if (s_Definitions != null)
		{
			return;
		}

		var table = Resources.Load<ModuleTable>("ModuleTable");
		if (table != null && table.Rows != null && table.Rows.Length > 0)
		{
			s_Definitions = new ModuleDefinition[table.Rows.Length];
			for (int index = 0; index < table.Rows.Length; index++)
			{
				var row = table.Rows[index];
				var definition = new ModuleDefinition();
				definition.Type = row.Type;
				definition.Category = row.Category;
				definition.DisplayName = row.DisplayName;
				definition.Price = row.Price;
				definition.Attack = row.Attack;
				definition.Health = row.Health;
				definition.Armor = row.Armor;
				definition.Speed = row.Speed;
				definition.Range = row.Range;
				definition.Color = row.Color;
				s_Definitions[index] = definition;
			}

			return;
		}

		s_Definitions = DefaultDefinitions();
	}

	/// <summary>
	/// 기본 모듈 정의 9종(테이블 부재 시).
	/// </summary>
	private static ModuleDefinition[] DefaultDefinitions()
	{
		return new ModuleDefinition[]
		{
			new ModuleDefinition { Type = ModuleType.WeaponMachineGun, Category = ModuleCategory.Weapon, DisplayName = "기관포", Price = 90, Attack = 4, Health = 8, Armor = 1, Speed = 0, Range = 3, Color = new Color(0.95f, 0.45f, 0.4f, 1f) },
			new ModuleDefinition { Type = ModuleType.WeaponLaser, Category = ModuleCategory.Weapon, DisplayName = "레이저", Price = 130, Attack = 7, Health = 10, Armor = 1, Speed = 0, Range = 6, Color = new Color(1f, 0.35f, 0.5f, 1f) },
			new ModuleDefinition { Type = ModuleType.WeaponCannon, Category = ModuleCategory.Weapon, DisplayName = "캐논", Price = 180, Attack = 12, Health = 14, Armor = 2, Speed = 0, Range = 4, Color = new Color(0.8f, 0.25f, 0.25f, 1f) },
			new ModuleDefinition { Type = ModuleType.ArmorLight, Category = ModuleCategory.Armor, DisplayName = "경장갑", Price = 60, Attack = 0, Health = 30, Armor = 3, Speed = 0, Range = 0, Color = new Color(0.55f, 0.66f, 0.8f, 1f) },
			new ModuleDefinition { Type = ModuleType.ArmorHeavy, Category = ModuleCategory.Armor, DisplayName = "중장갑", Price = 110, Attack = 0, Health = 65, Armor = 6, Speed = 0, Range = 0, Color = new Color(0.42f, 0.5f, 0.62f, 1f) },
			new ModuleDefinition { Type = ModuleType.ArmorReactive, Category = ModuleCategory.Armor, DisplayName = "반응장갑", Price = 130, Attack = 0, Health = 45, Armor = 5, Speed = 0, Range = 0, Color = new Color(0.5f, 0.62f, 0.7f, 1f) },
			new ModuleDefinition { Type = ModuleType.EngineSmall, Category = ModuleCategory.Engine, DisplayName = "소형엔진", Price = 90, Attack = 0, Health = 8, Armor = 1, Speed = 4, Range = 0, Color = new Color(0.95f, 0.72f, 0.35f, 1f) },
			new ModuleDefinition { Type = ModuleType.EngineThrust, Category = ModuleCategory.Engine, DisplayName = "추진엔진", Price = 140, Attack = 0, Health = 10, Armor = 1, Speed = 7, Range = 0, Color = new Color(1f, 0.62f, 0.25f, 1f) },
			new ModuleDefinition { Type = ModuleType.EngineTwin, Category = ModuleCategory.Engine, DisplayName = "쌍발엔진", Price = 190, Attack = 0, Health = 12, Armor = 1, Speed = 10, Range = 0, Color = new Color(1f, 0.55f, 0.18f, 1f) },
		};
	}
}

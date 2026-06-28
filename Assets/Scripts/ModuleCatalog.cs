using System.Collections.Generic;
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
	public int PowerSupply;
	public int PowerCost;
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
	/// 코어 기본 전투력(코어 체력 기여).
	/// </summary>
	public const int CorePower = 10;

	/// <summary>
	/// 모듈 배치의 전투력을 각 모듈의 실제 스탯으로 계산한다.
	/// 공격/방어/속도는 전투 영향이 큰 만큼 가중하고, 코어 기본값을 더한다.
	/// </summary>
	public static int ComputePower(List<ModulePlacement> layout)
	{
		var power = CorePower;
		if (layout == null)
		{
			return power;
		}

		for (int index = 0; index < layout.Count; index++)
		{
			power += ComputeModulePower(layout[index].Type);
		}

		return power;
	}

	/// <summary>
	/// 단일 모듈의 전투력 기여도를 계산한다.
	/// </summary>
	public static int ComputeModulePower(ModuleType type)
	{
		var definition = Get(type);
		return definition.Attack * 4 + definition.Health + definition.Armor * 3 + definition.Speed * 5;
	}

	/// <summary>
	/// 코어 기본 동력 공급(동력로 없이도 최소 작동).
	/// </summary>
	public const int CorePowerSupply = 6;

	/// <summary>
	/// 배치의 총 동력 공급(코어 기본 + 동력로 모듈).
	/// </summary>
	public static int ComputeSupply(List<ModulePlacement> layout)
	{
		var supply = CorePowerSupply;
		if (layout == null)
		{
			return supply;
		}

		for (int index = 0; index < layout.Count; index++)
		{
			supply += Get(layout[index].Type).PowerSupply;
		}

		return supply;
	}

	/// <summary>
	/// 배치의 총 동력 소비(무기/추진체).
	/// </summary>
	public static int ComputeCost(List<ModulePlacement> layout)
	{
		var cost = 0;
		if (layout == null)
		{
			return cost;
		}

		for (int index = 0; index < layout.Count; index++)
		{
			cost += Get(layout[index].Type).PowerCost;
		}

		return cost;
	}

	/// <summary>
	/// 동력 예산 안에서 작동하는 모듈 좌표 집합을 계산한다.
	/// 소비 모듈(무기/추진체)은 코어에서 가까운 순으로 켜고, 예산 초과분은 정지한다.
	/// 소비가 없는 모듈(장갑/동력로)은 항상 포함된다.
	/// </summary>
	public static HashSet<Vector2Int> ComputeActive(List<ModulePlacement> layout)
	{
		var active = new HashSet<Vector2Int>();
		if (layout == null)
		{
			return active;
		}

		var supply = ComputeSupply(layout);
		var consumers = new List<ModulePlacement>();
		for (int index = 0; index < layout.Count; index++)
		{
			var definition = Get(layout[index].Type);
			if (definition.PowerCost <= 0)
			{
				active.Add(layout[index].Coordinate);
				continue;
			}

			consumers.Add(layout[index]);
		}

		consumers.Sort(CompareByCoreDistance);

		var used = 0;
		for (int index = 0; index < consumers.Count; index++)
		{
			var cost = Get(consumers[index].Type).PowerCost;
			if (used + cost <= supply)
			{
				active.Add(consumers[index].Coordinate);
				used += cost;
			}
		}

		return active;
	}

	/// <summary>
	/// 코어(0,0)와의 맨해튼 거리 오름차순 비교자.
	/// </summary>
	private static int CompareByCoreDistance(ModulePlacement first, ModulePlacement second)
	{
		var firstDistance = Mathf.Abs(first.Coordinate.x) + Mathf.Abs(first.Coordinate.y);
		var secondDistance = Mathf.Abs(second.Coordinate.x) + Mathf.Abs(second.Coordinate.y);
		return firstDistance.CompareTo(secondDistance);
	}

	/// <summary>
	/// JSON 테이블의 한 행(엑셀→JSON 변환 결과). Enum/색은 문자열로 보관한다.
	/// </summary>
	[System.Serializable]
	private class ModuleJsonRow
	{
		public string Type;
		public string Category;
		public string DisplayName;
		public int Price;
		public int Attack;
		public int Health;
		public int Armor;
		public int Speed;
		public int Range;
		public int PowerSupply;
		public int PowerCost;
		public string Color;
	}

	/// <summary>
	/// JSON 테이블(행 배열).
	/// </summary>
	[System.Serializable]
	private class ModuleJsonTable
	{
		public ModuleJsonRow[] rows;
	}

	/// <summary>
	/// JSON 행을 런타임 정의로 변환한다.
	/// </summary>
	private static ModuleDefinition FromJsonRow(ModuleJsonRow row)
	{
		var definition = new ModuleDefinition();

		ModuleType type;
		if (!System.Enum.TryParse(row.Type, out type))
		{
			type = ModuleType.WeaponMachineGun;
		}

		ModuleCategory category;
		if (!System.Enum.TryParse(row.Category, out category))
		{
			category = ModuleCategory.Weapon;
		}

		Color color;
		if (!ColorUtility.TryParseHtmlString(row.Color, out color))
		{
			color = Color.white;
		}

		definition.Type = type;
		definition.Category = category;
		definition.DisplayName = row.DisplayName;
		definition.Price = row.Price;
		definition.Attack = row.Attack;
		definition.Health = row.Health;
		definition.Armor = row.Armor;
		definition.Speed = row.Speed;
		definition.Range = row.Range;
		definition.PowerSupply = row.PowerSupply;
		definition.PowerCost = row.PowerCost;
		definition.Color = color;
		return definition;
	}

	/// <summary>
	/// 정의를 로드한다(JSON 테이블 우선 → ModuleTable SO → 기본값).
	/// </summary>
	private static void EnsureLoaded()
	{
		if (s_Definitions != null)
		{
			return;
		}

		var jsonAsset = Resources.Load<TextAsset>("Tables/Modules");
		if (jsonAsset != null)
		{
			var parsed = JsonUtility.FromJson<ModuleJsonTable>(jsonAsset.text);
			if (parsed != null && parsed.rows != null && parsed.rows.Length > 0)
			{
				s_Definitions = new ModuleDefinition[parsed.rows.Length];
				for (int index = 0; index < parsed.rows.Length; index++)
				{
					s_Definitions[index] = FromJsonRow(parsed.rows[index]);
				}

				return;
			}
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
				definition.PowerSupply = row.PowerSupply;
				definition.PowerCost = row.PowerCost;
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
			new ModuleDefinition { Type = ModuleType.WeaponMachineGun, Category = ModuleCategory.Weapon, DisplayName = "기관포", Price = 90, Attack = 4, Health = 8, Armor = 1, Speed = 0, Range = 3, PowerSupply = 0, PowerCost = 2, Color = new Color(0.95f, 0.45f, 0.4f, 1f) },
			new ModuleDefinition { Type = ModuleType.WeaponLaser, Category = ModuleCategory.Weapon, DisplayName = "레이저", Price = 130, Attack = 7, Health = 10, Armor = 1, Speed = 0, Range = 6, PowerSupply = 0, PowerCost = 4, Color = new Color(1f, 0.35f, 0.5f, 1f) },
			new ModuleDefinition { Type = ModuleType.WeaponCannon, Category = ModuleCategory.Weapon, DisplayName = "캐논", Price = 180, Attack = 12, Health = 14, Armor = 2, Speed = 0, Range = 4, PowerSupply = 0, PowerCost = 6, Color = new Color(0.8f, 0.25f, 0.25f, 1f) },
			new ModuleDefinition { Type = ModuleType.ArmorLight, Category = ModuleCategory.Armor, DisplayName = "경장갑", Price = 60, Attack = 0, Health = 30, Armor = 3, Speed = 0, Range = 0, PowerSupply = 0, PowerCost = 0, Color = new Color(0.55f, 0.66f, 0.8f, 1f) },
			new ModuleDefinition { Type = ModuleType.ArmorHeavy, Category = ModuleCategory.Armor, DisplayName = "중장갑", Price = 110, Attack = 0, Health = 65, Armor = 6, Speed = 0, Range = 0, PowerSupply = 0, PowerCost = 0, Color = new Color(0.42f, 0.5f, 0.62f, 1f) },
			new ModuleDefinition { Type = ModuleType.ArmorReactive, Category = ModuleCategory.Armor, DisplayName = "반응장갑", Price = 130, Attack = 0, Health = 45, Armor = 5, Speed = 0, Range = 0, PowerSupply = 0, PowerCost = 0, Color = new Color(0.5f, 0.62f, 0.7f, 1f) },
			new ModuleDefinition { Type = ModuleType.EngineSmall, Category = ModuleCategory.Engine, DisplayName = "소형엔진", Price = 90, Attack = 0, Health = 8, Armor = 1, Speed = 4, Range = 0, PowerSupply = 0, PowerCost = 2, Color = new Color(0.95f, 0.72f, 0.35f, 1f) },
			new ModuleDefinition { Type = ModuleType.EngineThrust, Category = ModuleCategory.Engine, DisplayName = "추진엔진", Price = 140, Attack = 0, Health = 10, Armor = 1, Speed = 7, Range = 0, PowerSupply = 0, PowerCost = 3, Color = new Color(1f, 0.62f, 0.25f, 1f) },
			new ModuleDefinition { Type = ModuleType.EngineTwin, Category = ModuleCategory.Engine, DisplayName = "쌍발엔진", Price = 190, Attack = 0, Health = 12, Armor = 1, Speed = 10, Range = 0, PowerSupply = 0, PowerCost = 5, Color = new Color(1f, 0.55f, 0.18f, 1f) },
			new ModuleDefinition { Type = ModuleType.ReactorCore, Category = ModuleCategory.Reactor, DisplayName = "동력로", Price = 100, Attack = 0, Health = 16, Armor = 2, Speed = 0, Range = 0, PowerSupply = 12, PowerCost = 0, Color = new Color(0.5f, 0.95f, 0.8f, 1f) },
		};
	}
}

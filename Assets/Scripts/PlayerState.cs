using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 플레이어 상태. 레벨/경험치/활동력/재화와 보유 모듈(장착/선택 상태)을 보관한다.
/// 인벤토리/함선/전투의 단일 진실 공급원이며, 변경 내용을 PlayerPrefs에 저장한다.
/// </summary>
public class PlayerState : MonoBehaviour
{
	#region INSPECTOR
	[SerializeField] private int m_Level = 1;
	[SerializeField] private int m_Experience = 0;
	[SerializeField] private int m_Activity = 100;
	[SerializeField] private int m_MaxActivity = 100;
	[SerializeField] private int m_Currency = 1000;
	[SerializeField] private int m_Metal = 30;
	#endregion

	private const string SaveKey = "spaceship_playerstate_v2";

	private static readonly Vector2Int s_CoreCell = new Vector2Int(0, 0);
	private static readonly Vector2Int[] s_Directions = new Vector2Int[]
	{
		new Vector2Int(0, 1),
		new Vector2Int(0, -1),
		new Vector2Int(-1, 0),
		new Vector2Int(1, 0),
	};

	/// <summary>
	/// 저장용 데이터.
	/// </summary>
	[Serializable]
	private class SaveData
	{
		public int Level;
		public int Experience;
		public int Activity;
		public int MaxActivity;
		public int Currency;
		public int Metal;
		public int NextModuleId;
		public int[] Ids;
		public int[] Types;
		public bool[] Equipped;
		public int[] CoordX;
		public int[] CoordY;
		public int[] ClearedStages;
		public int Wins;
		public bool ManualMove;
		public long ActivityTimestamp;
	}

	private readonly List<ModuleInstance> m_Modules = new List<ModuleInstance>();
	private readonly List<int> m_ClearedStages = new List<int>();
	private int m_NextModuleId = 1;
	private int m_SelectedId = -1;
	private int m_Wins;
	private bool m_ManualMove;
	private bool m_AutoPreview = true;
	private long m_ActivityTimestampTicks;
	private bool m_SuppressSave;

	/// <summary>
	/// 상태 값이 변경되면 발생한다.
	/// </summary>
	public event Action Changed;

	/// <summary> 레벨. </summary>
	public int Level { get { return m_Level; } }

	/// <summary> 경험치. </summary>
	public int Experience { get { return m_Experience; } }

	/// <summary> 활동력. </summary>
	public int Activity { get { return m_Activity; } }

	/// <summary> 최대 활동력. </summary>
	public int MaxActivity { get { return m_MaxActivity; } }

	/// <summary> 재화(크레딧). </summary>
	public int Currency { get { return m_Currency; } }

	/// <summary> 금속 자원. </summary>
	public int Metal { get { return m_Metal; } }

	/// <summary> 선택된 모듈 식별자(-1: 없음). </summary>
	public int SelectedId { get { return m_SelectedId; } }

	/// <summary> 누적 전투 승리 횟수. </summary>
	public int Wins { get { return m_Wins; } }

	/// <summary> 전투 수동 이동 설정 여부. </summary>
	public bool ManualMove { get { return m_ManualMove; } }

	/// <summary> 개조 화면 자동 발사 미리보기 여부(세션 설정, 기본 on). </summary>
	public bool AutoPreview { get { return m_AutoPreview; } }

	/// <summary> 자동 발사 미리보기를 켜고 끈다. </summary>
	public void SetAutoPreview(bool on)
	{
		if (m_AutoPreview == on)
		{
			return;
		}

		m_AutoPreview = on;
		RaiseChanged();
	}

	/// <summary> 활동력 1 회복까지 남은 시간(초). 가득이면 0. </summary>
	public float SecondsToNextRecovery
	{
		get
		{
			if (m_Activity >= m_MaxActivity)
			{
				return 0f;
			}

			var elapsed = DateTime.UtcNow.Ticks - m_ActivityTimestampTicks;
			var interval = TimeSpan.TicksPerMinute * ActivityRecoverMinutes;
			if (elapsed < 0)
			{
				elapsed = 0;
			}

			var remain = interval - (elapsed % interval);
			return (float)remain / TimeSpan.TicksPerSecond;
		}
	}

	/// <summary> 보유 모듈 목록. </summary>
	public IReadOnlyList<ModuleInstance> Modules { get { return m_Modules; } }

	/// <summary>
	/// 생성됨. 저장이 있으면 불러오고, 없으면 기본 모듈을 지급한다.
	/// </summary>
	private void Awake()
	{
		if (Load())
		{
			return;
		}

		m_SuppressSave = true;
		for (int type = 0; type < 10; type++)
		{
			for (int count = 0; count < 2; count++)
			{
				AddModule((ModuleType)type);
			}
		}

		m_ActivityTimestampTicks = DateTime.UtcNow.Ticks;
		m_SuppressSave = false;
		Save();
	}

	/// <summary>
	/// 매 프레임 경과 시간만큼 활동력을 회복한다.
	/// </summary>
	private void Update()
	{
		RecoverActivity();
	}

	/// <summary>
	/// 모듈을 인벤토리에 추가한다(미장착).
	/// </summary>
	public void AddModule(ModuleType type)
	{
		var instance = new ModuleInstance();
		instance.Id = m_NextModuleId;
		instance.Type = type;
		instance.Equipped = false;
		m_NextModuleId += 1;
		m_Modules.Add(instance);
		Save();
		RaiseChanged();
	}

	/// <summary>
	/// 식별자로 모듈을 찾는다(없으면 null).
	/// </summary>
	public ModuleInstance GetModule(int instanceId)
	{
		for (int index = 0; index < m_Modules.Count; index++)
		{
			if (m_Modules[index].Id == instanceId)
			{
				return m_Modules[index];
			}
		}

		return null;
	}

	/// <summary>
	/// 모듈을 선택한다(장착/미장착 모두 가능). 같은 모듈 다시 선택 시 해제.
	/// </summary>
	public void SelectModule(int instanceId)
	{
		if (GetModule(instanceId) == null)
		{
			return;
		}

		m_SelectedId = m_SelectedId == instanceId ? -1 : instanceId;
		RaiseChanged();
	}

	/// <summary>
	/// 선택을 해제한다.
	/// </summary>
	public void ClearSelection()
	{
		if (m_SelectedId == -1)
		{
			return;
		}

		m_SelectedId = -1;
		RaiseChanged();
	}

	/// <summary>
	/// 좌표에 장착된 모듈이 있는지 확인한다.
	/// </summary>
	public bool IsCoordinateOccupied(Vector2Int coordinate)
	{
		for (int index = 0; index < m_Modules.Count; index++)
		{
			if (m_Modules[index].Equipped && m_Modules[index].Coordinate == coordinate)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// 좌표에 장착된 모듈 식별자를 반환한다(-1: 없음).
	/// </summary>
	public int GetEquippedInstanceAt(Vector2Int coordinate)
	{
		for (int index = 0; index < m_Modules.Count; index++)
		{
			if (m_Modules[index].Equipped && m_Modules[index].Coordinate == coordinate)
			{
				return m_Modules[index].Id;
			}
		}

		return -1;
	}

	/// <summary>
	/// 모듈을 지정 좌표에 장착한다. 성공하면 true.
	/// </summary>
	public bool TryEquip(int instanceId, Vector2Int coordinate)
	{
		var module = GetModule(instanceId);
		if (module == null || module.Equipped || IsCoordinateOccupied(coordinate))
		{
			return false;
		}

		module.Equipped = true;
		module.Coordinate = coordinate;
		m_SelectedId = -1;
		Save();
		RaiseChanged();
		return true;
	}

	/// <summary>
	/// 코어에서 가장 가까운 빈 칸을 찾는다.
	/// </summary>
	public bool TryGetFirstAvailableSlot(out Vector2Int result)
	{
		var occupied = new HashSet<Vector2Int>();
		occupied.Add(s_CoreCell);
		for (int index = 0; index < m_Modules.Count; index++)
		{
			if (m_Modules[index].Equipped)
			{
				occupied.Add(m_Modules[index].Coordinate);
			}
		}

		result = s_CoreCell;
		var found = false;
		var best = int.MaxValue;
		foreach (var cell in occupied)
		{
			foreach (var direction in s_Directions)
			{
				var neighbor = cell + direction;
				if (occupied.Contains(neighbor))
				{
					continue;
				}

				var score = (Mathf.Abs(neighbor.x) + Mathf.Abs(neighbor.y)) * 100 - neighbor.y * 10 + neighbor.x;
				if (score < best)
				{
					best = score;
					result = neighbor;
					found = true;
				}
			}
		}

		return found;
	}

	/// <summary>
	/// 선택(또는 지정) 모듈을 가장 가까운 빈 칸에 장착한다.
	/// </summary>
	public bool TryEquipFirstAvailable(int instanceId)
	{
		Vector2Int slot;
		if (!TryGetFirstAvailableSlot(out slot))
		{
			return false;
		}

		return TryEquip(instanceId, slot);
	}

	/// <summary>
	/// 모듈을 장착 해제한다.
	/// </summary>
	public void Unequip(int instanceId)
	{
		var module = GetModule(instanceId);
		if (module == null || !module.Equipped)
		{
			return;
		}

		module.Equipped = false;
		Save();
		RaiseChanged();
	}

	/// <summary>
	/// 장착된 모듈 배치를 반환한다(함선/전투용).
	/// </summary>
	public List<ModulePlacement> GetEquipped()
	{
		var equipped = new List<ModulePlacement>();
		for (int index = 0; index < m_Modules.Count; index++)
		{
			if (!m_Modules[index].Equipped)
			{
				continue;
			}

			var placement = new ModulePlacement();
			placement.Coordinate = m_Modules[index].Coordinate;
			placement.Type = m_Modules[index].Type;
			equipped.Add(placement);
		}

		return equipped;
	}

	/// <summary>
	/// 재화를 소비한다.
	/// </summary>
	public bool TrySpendCurrency(int amount)
	{
		if (m_Currency < amount)
		{
			return false;
		}

		m_Currency -= amount;
		Save();
		RaiseChanged();
		return true;
	}

	/// <summary>
	/// 재화를 획득한다.
	/// </summary>
	public void AddCurrency(int amount)
	{
		m_Currency += amount;
		Save();
		RaiseChanged();
	}

	/// <summary>
	/// 금속을 획득한다.
	/// </summary>
	public void AddMetal(int amount)
	{
		m_Metal += amount;
		Save();
		RaiseChanged();
	}

	/// <summary>
	/// 금속을 소비한다.
	/// </summary>
	public bool TrySpendMetal(int amount)
	{
		if (m_Metal < amount)
		{
			return false;
		}

		m_Metal -= amount;
		Save();
		RaiseChanged();
		return true;
	}

	/// <summary>
	/// 활동력을 소비한다.
	/// </summary>
	public bool TrySpendActivity(int amount)
	{
		if (m_Activity < amount)
		{
			return false;
		}

		m_Activity -= amount;
		Save();
		RaiseChanged();
		return true;
	}

	/// <summary>
	/// 활동력 회복 간격(분당 1 회복).
	/// </summary>
	private const int ActivityRecoverMinutes = 1;

	/// <summary>
	/// 마지막 기준 시각으로부터 경과한 분만큼 활동력을 회복한다(최대치까지).
	/// 가득 차 있으면 기준 시각만 현재로 맞춘다. 게임을 닫았다 열어도 경과 시간이 반영된다.
	/// </summary>
	private void RecoverActivity()
	{
		var nowTicks = DateTime.UtcNow.Ticks;
		if (m_Activity >= m_MaxActivity)
		{
			m_ActivityTimestampTicks = nowTicks;
			return;
		}

		var elapsedTicks = nowTicks - m_ActivityTimestampTicks;
		if (elapsedTicks <= 0)
		{
			return;
		}

		var intervalTicks = TimeSpan.TicksPerMinute * ActivityRecoverMinutes;
		var gained = (int)(elapsedTicks / intervalTicks);
		if (gained <= 0)
		{
			return;
		}

		m_Activity = Mathf.Min(m_MaxActivity, m_Activity + gained);
		m_ActivityTimestampTicks += (long)gained * intervalTicks;
		if (m_Activity >= m_MaxActivity)
		{
			m_ActivityTimestampTicks = nowTicks;
		}

		Save();
		RaiseChanged();
	}

	/// <summary>
	/// 활동력을 최대치로 채운다(설정 메뉴용).
	/// </summary>
	public void FillActivity()
	{
		if (m_Activity >= m_MaxActivity)
		{
			return;
		}

		m_Activity = m_MaxActivity;
		m_ActivityTimestampTicks = DateTime.UtcNow.Ticks;
		Save();
		RaiseChanged();
	}

	/// <summary>
	/// 스테이지를 클리어 처리한다.
	/// </summary>
	public void MarkStageCleared(int stageIndex)
	{
		if (m_ClearedStages.Contains(stageIndex))
		{
			return;
		}

		m_ClearedStages.Add(stageIndex);
		Save();
		RaiseChanged();
	}

	/// <summary>
	/// 해당 스테이지를 클리어했는지 확인한다.
	/// </summary>
	public bool IsStageCleared(int stageIndex)
	{
		return m_ClearedStages.Contains(stageIndex);
	}

	/// <summary>
	/// 전투 승리를 기록한다(레벨 N에서 N승 시 레벨업).
	/// </summary>
	public void RegisterWin()
	{
		m_Wins += 1;
		m_Experience += 1;
		if (m_Experience >= m_Level)
		{
			m_Experience -= m_Level;
			m_Level += 1;
			NotificationLog.Add("레벨 " + m_Level + " 달성!");
		}

		Save();
		RaiseChanged();
	}

	/// <summary>
	/// 전투 수동 이동 설정을 변경한다.
	/// </summary>
	public void SetManualMove(bool manual)
	{
		if (m_ManualMove == manual)
		{
			return;
		}

		m_ManualMove = manual;
		Save();
		RaiseChanged();
	}

	/// <summary>
	/// 변경 이벤트를 발생시킨다.
	/// </summary>
	public void RaiseChanged()
	{
		if (Changed != null)
		{
			Changed();
		}
	}

	/// <summary>
	/// 현재 상태를 저장한다.
	/// </summary>
	private void Save()
	{
		if (m_SuppressSave)
		{
			return;
		}

		var data = new SaveData();
		data.Level = m_Level;
		data.Experience = m_Experience;
		data.Activity = m_Activity;
		data.MaxActivity = m_MaxActivity;
		data.Currency = m_Currency;
		data.Metal = m_Metal;
		data.NextModuleId = m_NextModuleId;

		var count = m_Modules.Count;
		data.Ids = new int[count];
		data.Types = new int[count];
		data.Equipped = new bool[count];
		data.CoordX = new int[count];
		data.CoordY = new int[count];
		for (int index = 0; index < count; index++)
		{
			data.Ids[index] = m_Modules[index].Id;
			data.Types[index] = (int)m_Modules[index].Type;
			data.Equipped[index] = m_Modules[index].Equipped;
			data.CoordX[index] = m_Modules[index].Coordinate.x;
			data.CoordY[index] = m_Modules[index].Coordinate.y;
		}

		data.ClearedStages = m_ClearedStages.ToArray();
		data.Wins = m_Wins;
		data.ManualMove = m_ManualMove;
		data.ActivityTimestamp = m_ActivityTimestampTicks;

		PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
		PlayerPrefs.Save();
	}

	/// <summary>
	/// 저장된 상태를 불러온다. 없으면 false.
	/// </summary>
	private bool Load()
	{
		if (!PlayerPrefs.HasKey(SaveKey))
		{
			return false;
		}

		var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
		if (data == null || data.Ids == null)
		{
			return false;
		}

		m_Level = data.Level;
		m_Experience = data.Experience;
		m_Activity = data.Activity;
		m_MaxActivity = data.MaxActivity;
		m_Currency = data.Currency;
		m_Metal = data.Metal;
		m_NextModuleId = data.NextModuleId;

		m_Modules.Clear();
		for (int index = 0; index < data.Ids.Length; index++)
		{
			var instance = new ModuleInstance();
			instance.Id = data.Ids[index];
			instance.Type = (ModuleType)data.Types[index];
			instance.Equipped = data.Equipped[index];
			instance.Coordinate = new Vector2Int(data.CoordX[index], data.CoordY[index]);
			m_Modules.Add(instance);
		}

		m_ClearedStages.Clear();
		if (data.ClearedStages != null)
		{
			for (int index = 0; index < data.ClearedStages.Length; index++)
			{
				m_ClearedStages.Add(data.ClearedStages[index]);
			}
		}

		m_Wins = data.Wins;
		m_ManualMove = data.ManualMove;
		m_ActivityTimestampTicks = data.ActivityTimestamp != 0 ? data.ActivityTimestamp : DateTime.UtcNow.Ticks;

		return true;
	}
}

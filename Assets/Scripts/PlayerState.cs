using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 플레이어 상태. 레벨/경험치/활동력/재화와 보유 모듈(개별 개체)을 보관한다.
/// </summary>
public class PlayerState : MonoBehaviour
{
	#region INSPECTOR
	[SerializeField] private int m_Level = 1;
	[SerializeField] private int m_Experience = 0;
	[SerializeField] private int m_Activity = 100;
	[SerializeField] private int m_MaxActivity = 100;
	[SerializeField] private int m_Currency = 1000;
	#endregion

	/// <summary>
	/// 보유 모듈(개별 개체) 목록.
	/// </summary>
	private readonly List<ModuleInstance> m_Modules = new List<ModuleInstance>();

	/// <summary>
	/// 다음 모듈 인스턴스에 부여할 식별자.
	/// </summary>
	private int m_NextModuleId = 1;

	/// <summary>
	/// 상태 값이 변경되면 발생한다.
	/// </summary>
	public event Action Changed;

	/// <summary>
	/// 레벨.
	/// </summary>
	public int Level
	{
		get { return m_Level; }
	}

	/// <summary>
	/// 경험치.
	/// </summary>
	public int Experience
	{
		get { return m_Experience; }
	}

	/// <summary>
	/// 활동력.
	/// </summary>
	public int Activity
	{
		get { return m_Activity; }
	}

	/// <summary>
	/// 최대 활동력.
	/// </summary>
	public int MaxActivity
	{
		get { return m_MaxActivity; }
	}

	/// <summary>
	/// 재화.
	/// </summary>
	public int Currency
	{
		get { return m_Currency; }
	}

	/// <summary>
	/// 보유 모듈 목록.
	/// </summary>
	public IReadOnlyList<ModuleInstance> Modules
	{
		get { return m_Modules; }
	}

	/// <summary>
	/// 생성됨. 기본 모듈을 지급한다.
	/// </summary>
	private void Awake()
	{
		for (int index = 0; index < 5; index++)
		{
			AddModule(ModuleType.Weapon);
		}

		for (int index = 0; index < 4; index++)
		{
			AddModule(ModuleType.Armor);
		}

		for (int index = 0; index < 4; index++)
		{
			AddModule(ModuleType.Engine);
		}
	}

	/// <summary>
	/// 모듈을 인벤토리에 추가한다.
	/// </summary>
	public void AddModule(ModuleType type)
	{
		var instance = new ModuleInstance();
		instance.Id = m_NextModuleId;
		instance.Type = type;
		m_NextModuleId += 1;
		m_Modules.Add(instance);
		RaiseChanged();
	}

	/// <summary>
	/// 해당 식별자의 모듈을 보유 중인지 확인한다.
	/// </summary>
	public bool ContainsModule(int instanceId)
	{
		for (int index = 0; index < m_Modules.Count; index++)
		{
			if (m_Modules[index].Id == instanceId)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// 해당 식별자의 모듈을 제거한다.
	/// </summary>
	public bool RemoveModule(int instanceId)
	{
		for (int index = 0; index < m_Modules.Count; index++)
		{
			if (m_Modules[index].Id == instanceId)
			{
				m_Modules.RemoveAt(index);
				RaiseChanged();
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// 재화를 소비한다. 잔액이 부족하면 실패한다.
	/// </summary>
	public bool TrySpendCurrency(int amount)
	{
		if (m_Currency < amount)
		{
			return false;
		}

		m_Currency -= amount;
		RaiseChanged();
		return true;
	}

	/// <summary>
	/// 활동력을 소비한다. 부족하면 실패한다.
	/// </summary>
	public bool TrySpendActivity(int amount)
	{
		if (m_Activity < amount)
		{
			return false;
		}

		m_Activity -= amount;
		RaiseChanged();
		return true;
	}

	/// <summary>
	/// 재화를 획득한다.
	/// </summary>
	public void AddCurrency(int amount)
	{
		m_Currency += amount;
		RaiseChanged();
	}

	/// <summary>
	/// 전투 승리를 기록한다. 현재 레벨만큼 승리하면 레벨업한다.
	/// (레벨 N 에서 N 회 승리 시 레벨 N+1)
	/// </summary>
	public void RegisterWin()
	{
		m_Experience += 1;
		if (m_Experience >= m_Level)
		{
			m_Experience -= m_Level;
			m_Level += 1;
		}

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
}

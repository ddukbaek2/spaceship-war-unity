using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 플레이어 상태. 레벨/경험치/활동력/재화를 보관한다.
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
	/// 보유 모듈 수량(종류별).
	/// </summary>
	private readonly Dictionary<ModuleType, int> m_Inventory = new Dictionary<ModuleType, int>();

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
	/// 보유 모듈 수량을 반환한다.
	/// </summary>
	public int GetModuleCount(ModuleType type)
	{
		int count;
		if (m_Inventory.TryGetValue(type, out count))
		{
			return count;
		}

		return 0;
	}

	/// <summary>
	/// 모듈을 인벤토리에 추가한다.
	/// </summary>
	public void AddModule(ModuleType type)
	{
		var count = GetModuleCount(type);
		m_Inventory[type] = count + 1;
		RaiseChanged();
	}

	/// <summary>
	/// 모듈을 인벤토리에서 하나 제거한다. 보유분이 없으면 실패한다.
	/// </summary>
	public bool TryRemoveModule(ModuleType type)
	{
		var count = GetModuleCount(type);
		if (count <= 0)
		{
			return false;
		}

		m_Inventory[type] = count - 1;
		RaiseChanged();
		return true;
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

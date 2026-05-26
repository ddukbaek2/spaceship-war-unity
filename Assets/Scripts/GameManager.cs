using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;



/// <summary>
/// 게임 매니저.
/// </summary>
public class GameManager : SharedComponent2<GameManager>
{
	#region INSPECTOR
	[SerializeField] private RectTransform m_HUDRectTransform;
	[SerializeField] private Character m_PlayerCharacter;
	#endregion

	private List<Pawn> m_Monsters;

	/// <summary>
	/// 생성됨.
	/// </summary>
	protected override void Awake()
	{
		base.Awake();

		if (m_HUDRectTransform == null)
		{
			//GameObject.FindFirstObjectByType<RectTransform>();
			var obj = GameObject.Find("UI/Canvas/HUD");
			m_HUDRectTransform = obj.GetComponent<RectTransform>();
		}

		if (m_PlayerCharacter == null)
		{
			var asset = Resources.Load<GameObject>("Units/PlayerCharacter");
			var obj = GameObject.Instantiate(asset);
			m_PlayerCharacter = asset.AddComponent<PlayerCharacter>();
		}


		m_Monsters = new List<Pawn>();
	}

	/// <summary>
	/// 초기화됨.
	/// </summary>
	protected override void Start()
	{
		base.Start();
	}

	/// <summary>
	/// 갱신됨.
	/// </summary>
	private void Update()
	{

	}

	private void SpawnMonster()
	{
	}
}
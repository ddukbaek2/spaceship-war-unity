using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


/// <summary>
/// 전투 컨트롤러. 전투 탭에 랜덤 적 우주선 목록을 구성하고, 전투하기 시
/// 활동력을 소모하여 별도 전투 씬(가산 로드)으로 전환한다. 결과를 받아 보상을 처리한다.
/// </summary>
public class BattleController : MonoBehaviour
{
	private const int ActivityCost = 10;
	private const int StageCount = 8;
	private const float RowHeight = 150f;
	private const float RowSpacing = 16f;
	private const string BattleSceneName = "Battle";

	/// <summary>
	/// 스테이지 적 우주선(번호/이름/배치/전투력).
	/// </summary>
	private struct EnemyShip
	{
		public int StageIndex;
		public string Name;
		public List<ModulePlacement> Layout;
		public int Power;
	}

	private static readonly Vector2Int s_CoreCell = new Vector2Int(0, 0);

	private static readonly Vector2Int[] s_Directions = new Vector2Int[]
	{
		new Vector2Int(0, 1),
		new Vector2Int(0, -1),
		new Vector2Int(-1, 0),
		new Vector2Int(1, 0),
	};

	private static readonly string[] s_EnemyNames = new string[]
	{
		"붉은 약탈자", "검은 유성", "강철 전갈", "녹빛 추적자", "푸른 망령",
		"황금 독수리", "심연의 포식자", "은빛 칼날", "혹한의 사냥꾼", "폭풍 전위",
	};

	private PlayerState m_PlayerState;
	private ShipBuilder m_ShipBuilder;
	private Canvas m_Canvas;

	private RectTransform m_ListContent;
	private TMP_Text m_MessageText;
	private readonly List<EnemyShip> m_Enemies = new List<EnemyShip>();
	private bool m_Busy;

	private GameObject m_MainCamera;
	private GameObject m_MainCanvas;

	/// <summary>
	/// 초기화됨.
	/// </summary>
	private void Start()
	{
		m_PlayerState = FindFirstObjectByType<PlayerState>();
		m_ShipBuilder = FindFirstObjectByType<ShipBuilder>(FindObjectsInactive.Include);

		var canvasObject = GameObject.Find("UI/Canvas");
		m_Canvas = canvasObject.GetComponent<Canvas>();
		m_MainCanvas = canvasObject;
		m_MainCamera = GameObject.Find("Main Camera");

		var battleScreen = canvasObject.transform.Find("Screens").Find("Screen_전투");
		BuildList(battleScreen);
		BuildStages();
		RefreshList();
	}

	/// <summary>
	/// 전투 목록 화면을 구성한다.
	/// </summary>
	private void BuildList(Transform battleScreen)
	{
		for (int index = battleScreen.childCount - 1; index >= 0; index--)
		{
			DestroyImmediate(battleScreen.GetChild(index).gameObject);
		}

		var banner = UiFactory.CreateImage("Banner", battleScreen, new Color(0.22f, 0.12f, 0.14f, 1f));
		var bannerRect = (RectTransform)banner.transform;
		bannerRect.anchorMin = new Vector2(0f, 1f);
		bannerRect.anchorMax = new Vector2(1f, 1f);
		bannerRect.pivot = new Vector2(0.5f, 1f);
		bannerRect.sizeDelta = new Vector2(0f, 110f);
		UiFactory.CreateText("Title", banner.transform, null, "⚔ 스테이지 ⚔", 52, new Color(1f, 0.6f, 0.55f, 1f), TextAnchor.MiddleCenter);

		m_MessageText = UiFactory.CreateText("Message", battleScreen, null, "스테이지를 선택하세요 (전투당 활동력 " + ActivityCost + ")", 26, new Color(0.7f, 0.72f, 0.78f, 1f), TextAnchor.MiddleCenter);
		var messageRect = m_MessageText.rectTransform;
		messageRect.anchorMin = new Vector2(0f, 1f);
		messageRect.anchorMax = new Vector2(1f, 1f);
		messageRect.pivot = new Vector2(0.5f, 1f);
		messageRect.sizeDelta = new Vector2(0f, 44f);
		messageRect.anchoredPosition = new Vector2(0f, -114f);

		var scrollViewObject = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
		scrollViewObject.layer = battleScreen.gameObject.layer;
		scrollViewObject.transform.SetParent(battleScreen, false);
		var scrollViewRect = (RectTransform)scrollViewObject.transform;
		scrollViewRect.anchorMin = new Vector2(0f, 0f);
		scrollViewRect.anchorMax = new Vector2(1f, 1f);
		scrollViewRect.offsetMin = new Vector2(12f, 12f);
		scrollViewRect.offsetMax = new Vector2(-12f, -168f);

		var viewport = UiFactory.CreateImage("Viewport", scrollViewRect, new Color(0f, 0f, 0f, 0.12f));
		var viewportRect = (RectTransform)viewport.transform;
		viewportRect.anchorMin = new Vector2(0f, 0f);
		viewportRect.anchorMax = new Vector2(1f, 1f);
		viewportRect.offsetMin = new Vector2(0f, 0f);
		viewportRect.offsetMax = new Vector2(0f, 0f);
		viewport.AddComponent<RectMask2D>();

		var contentObject = new GameObject("Content", typeof(RectTransform));
		contentObject.layer = battleScreen.gameObject.layer;
		contentObject.transform.SetParent(viewportRect, false);
		m_ListContent = (RectTransform)contentObject.transform;
		m_ListContent.anchorMin = new Vector2(0f, 1f);
		m_ListContent.anchorMax = new Vector2(1f, 1f);
		m_ListContent.pivot = new Vector2(0.5f, 1f);

		var scrollRect = scrollViewObject.GetComponent<ScrollRect>();
		scrollRect.content = m_ListContent;
		scrollRect.viewport = viewportRect;
		scrollRect.horizontal = false;
		scrollRect.vertical = true;
		scrollRect.movementType = ScrollRect.MovementType.Elastic;
		scrollRect.scrollSensitivity = 30f;
	}

	/// <summary>
	/// 고정 스테이지 목록을 구성한다(스테이지마다 결정적 배치).
	/// </summary>
	private void BuildStages()
	{
		var previousState = Random.state;
		m_Enemies.Clear();
		for (int index = 0; index < StageCount; index++)
		{
			m_Enemies.Add(CreateStage(index));
		}

		Random.state = previousState;
	}

	/// <summary>
	/// 스테이지 번호로 결정적 적 우주선(모듈 배치)을 만든다.
	/// </summary>
	private EnemyShip CreateStage(int stageIndex)
	{
		Random.InitState(7919 + stageIndex * 131);

		var layout = new List<ModulePlacement>();
		var occupied = new HashSet<Vector2Int>();
		occupied.Add(s_CoreCell);

		var count = Mathf.Min(2 + stageIndex, 8);
		for (int index = 0; index < count; index++)
		{
			var slots = new List<Vector2Int>();
			foreach (var cell in occupied)
			{
				foreach (var direction in s_Directions)
				{
					var neighbor = cell + direction;
					if (!occupied.Contains(neighbor) && !slots.Contains(neighbor))
					{
						slots.Add(neighbor);
					}
				}
			}

			if (slots.Count == 0)
			{
				break;
			}

			var coordinate = slots[Random.Range(0, slots.Count)];
			var placement = new ModulePlacement();
			placement.Coordinate = coordinate;
			placement.Type = (ModuleType)Random.Range(0, 9);
			layout.Add(placement);
			occupied.Add(coordinate);
		}

		var enemy = new EnemyShip();
		enemy.StageIndex = stageIndex;
		enemy.Name = "스테이지 " + (stageIndex + 1) + " · " + s_EnemyNames[stageIndex % s_EnemyNames.Length];
		enemy.Layout = layout;
		enemy.Power = ComputePower(layout);
		return enemy;
	}

	/// <summary>
	/// 배치로부터 전투력을 계산한다.
	/// </summary>
	private int ComputePower(List<ModulePlacement> layout)
	{
		var attack = 2;
		var health = 60;
		var speed = 1;
		for (int index = 0; index < layout.Count; index++)
		{
			var definition = ModuleCatalog.Get(layout[index].Type);
			attack += definition.Attack;
			health += definition.Health;
			speed += definition.Speed;
		}

		return attack * 3 + health + speed * 2;
	}

	/// <summary>
	/// 전투 목록을 다시 그린다.
	/// </summary>
	private void RefreshList()
	{
		if (m_ListContent == null)
		{
			return;
		}

		for (int index = m_ListContent.childCount - 1; index >= 0; index--)
		{
			Destroy(m_ListContent.GetChild(index).gameObject);
		}

		var topPad = 12f;
		var stride = RowHeight + RowSpacing;
		m_ListContent.sizeDelta = new Vector2(0f, topPad * 2f + m_Enemies.Count * stride - RowSpacing);

		for (int index = 0; index < m_Enemies.Count; index++)
		{
			CreateEnemyRow(m_Enemies[index], index, topPad, stride);
		}
	}

	/// <summary>
	/// 스테이지 한 줄을 생성한다.
	/// </summary>
	private void CreateEnemyRow(EnemyShip enemy, int index, float topPad, float stride)
	{
		var cleared = m_PlayerState != null && m_PlayerState.IsStageCleared(enemy.StageIndex);
		var rowColor = cleared ? new Color(0.13f, 0.19f, 0.16f, 1f) : new Color(0.14f, 0.15f, 0.19f, 1f);
		var row = UiFactory.CreateImage("Enemy", m_ListContent, rowColor);
		var rowRect = (RectTransform)row.transform;
		rowRect.anchorMin = new Vector2(0f, 1f);
		rowRect.anchorMax = new Vector2(1f, 1f);
		rowRect.pivot = new Vector2(0.5f, 1f);
		rowRect.sizeDelta = new Vector2(-24f, RowHeight);
		rowRect.anchoredPosition = new Vector2(0f, -(topPad + index * stride));

		if (cleared)
		{
			var clearBadge = UiFactory.CreateText("Clear", row.transform, null, "✔ 클리어", 26, new Color(0.5f, 1f, 0.6f, 1f), TextAnchor.UpperRight);
			clearBadge.raycastTarget = false;
			var clearRect = clearBadge.rectTransform;
			clearRect.anchorMin = new Vector2(0.4f, 1f);
			clearRect.anchorMax = new Vector2(1f, 1f);
			clearRect.pivot = new Vector2(1f, 1f);
			clearRect.sizeDelta = new Vector2(-260f, 40f);
			clearRect.anchoredPosition = new Vector2(-24f, -10f);
		}

		var name = UiFactory.CreateText("Name", row.transform, null, enemy.Name, 36, Color.white, TextAnchor.MiddleLeft);
		var nameRect = name.rectTransform;
		nameRect.anchorMin = new Vector2(0f, 0.5f);
		nameRect.anchorMax = new Vector2(0.6f, 1f);
		nameRect.offsetMin = new Vector2(28f, 0f);
		nameRect.offsetMax = new Vector2(0f, 0f);

		var power = UiFactory.CreateText("Power", row.transform, null, "전투력 " + enemy.Power + "   모듈 " + enemy.Layout.Count, 28, new Color(1f, 0.7f, 0.5f, 1f), TextAnchor.MiddleLeft);
		var powerRect = power.rectTransform;
		powerRect.anchorMin = new Vector2(0f, 0f);
		powerRect.anchorMax = new Vector2(0.6f, 0.5f);
		powerRect.offsetMin = new Vector2(28f, 0f);
		powerRect.offsetMax = new Vector2(0f, 0f);

		var fightObject = UiFactory.CreateImage("Fight", row.transform, new Color(0.75f, 0.25f, 0.3f, 1f));
		var fightRect = (RectTransform)fightObject.transform;
		fightRect.anchorMin = new Vector2(1f, 0.5f);
		fightRect.anchorMax = new Vector2(1f, 0.5f);
		fightRect.pivot = new Vector2(1f, 0.5f);
		fightRect.sizeDelta = new Vector2(220f, 100f);
		fightRect.anchoredPosition = new Vector2(-24f, 0f);
		var fightButton = fightObject.AddComponent<Button>();
		var fightLabel = UiFactory.CreateText("Label", fightObject.transform, null, "전투하기", 32, Color.white, TextAnchor.MiddleCenter);
		fightLabel.raycastTarget = false;

		var capturedEnemy = enemy;
		fightButton.onClick.AddListener(() => OnFight(capturedEnemy));
	}

	/// <summary>
	/// 전투하기. 활동력을 소모하고 전투 씬으로 전환한다.
	/// </summary>
	private void OnFight(EnemyShip enemy)
	{
		if (m_Busy)
		{
			return;
		}

		if (m_PlayerState == null)
		{
			return;
		}

		if (!m_PlayerState.TrySpendActivity(ActivityCost))
		{
			m_MessageText.text = "활동력이 부족합니다 (필요 " + ActivityCost + ")";
			return;
		}

		m_MessageText.text = "스테이지를 선택하세요 (전투당 활동력 " + ActivityCost + ")";

		BattleContext.PlayerLayout = m_ShipBuilder != null ? m_ShipBuilder.GetLayout() : new List<ModulePlacement>();
		BattleContext.EnemyLayout = enemy.Layout;
		BattleContext.EnemyName = enemy.Name;
		BattleContext.EnemyPower = enemy.Power;
		BattleContext.StageIndex = enemy.StageIndex;
		BattleContext.OnFinished = HandleFinished;

		m_Busy = true;
		SetMainSceneVisible(false);
		SceneManager.LoadScene(BattleSceneName, LoadSceneMode.Additive);
	}

	/// <summary>
	/// 전투 종료 처리. 보상 적용 후 전투 씬을 내리고 메인 화면을 복귀시킨다.
	/// </summary>
	private void HandleFinished()
	{
		BattleContext.OnFinished = null;

		if (BattleContext.ResultPlayerWon && m_PlayerState != null)
		{
			if (BattleContext.ResultCurrency > 0)
			{
				m_PlayerState.AddCurrency(BattleContext.ResultCurrency);
			}

			m_PlayerState.RegisterWin();
			m_PlayerState.MarkStageCleared(BattleContext.StageIndex);

			if (BattleContext.ResultHasItem)
			{
				m_PlayerState.AddModule(BattleContext.ResultItem);
			}
		}

		SceneManager.SetActiveScene(gameObject.scene);
		SceneManager.UnloadSceneAsync(BattleSceneName);
		SetMainSceneVisible(true);

		m_Busy = false;
		RefreshList();
	}

	/// <summary>
	/// 메인 씬의 카메라/UI 표시 여부를 설정한다.
	/// </summary>
	private void SetMainSceneVisible(bool visible)
	{
		if (m_MainCamera != null)
		{
			m_MainCamera.SetActive(visible);
		}

		if (m_MainCanvas != null)
		{
			m_MainCanvas.SetActive(visible);
		}
	}
}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 전투 컨트롤러. 전투 탭에 랜덤 적 우주선 목록을 구성하고,
/// 전투하기 시 활동력을 소모하여 자동 전투(연출)를 진행한 뒤 승패와 보상을 처리한다.
/// </summary>
public class BattleController : MonoBehaviour
{
	/// <summary>
	/// 전투 1회 활동력 소모량.
	/// </summary>
	private const int ActivityCost = 10;

	/// <summary>
	/// 적 우주선.
	/// </summary>
	private struct EnemyShip
	{
		public string Name;
		public ShipStats Stats;
	}

	private static readonly string[] s_EnemyNames = new string[]
	{
		"붉은 약탈자", "검은 유성", "강철 전갈", "녹빛 추적자", "푸른 망령",
		"황금 독수리", "심연의 포식자", "은빛 칼날", "혹한의 사냥꾼", "폭풍 전위",
	};

	private PlayerState m_PlayerState;
	private ShipBuilder m_ShipBuilder;
	private Canvas m_Canvas;
	private Font m_Font;

	private RectTransform m_ListContent;
	private TMP_Text m_MessageText;
	private readonly List<EnemyShip> m_Enemies = new List<EnemyShip>();

	private GameObject m_Overlay;
	private RectTransform m_PlayerHpFill;
	private RectTransform m_EnemyHpFill;
	private TMP_Text m_PlayerLabel;
	private TMP_Text m_EnemyLabel;
	private TMP_Text m_ResultText;
	private GameObject m_BeamObject;
	private GameObject m_ConfirmButton;
	private bool m_Busy;

	/// <summary>
	/// 초기화됨.
	/// </summary>
	private void Start()
	{
		m_PlayerState = FindFirstObjectByType<PlayerState>();
		m_ShipBuilder = FindFirstObjectByType<ShipBuilder>(FindObjectsInactive.Include);
		m_Font = UiFont.Default;

		var canvasObject = GameObject.Find("UI/Canvas");
		m_Canvas = canvasObject.GetComponent<Canvas>();
		var battleScreen = canvasObject.transform.Find("Screens").Find("Screen_전투");

		BuildList(battleScreen);
		BuildOverlay(canvasObject.transform);
		RegenerateEnemies();
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
		UiFactory.CreateText("Title", banner.transform, m_Font, "⚔ 전투 ⚔", 52, new Color(1f, 0.6f, 0.55f, 1f), TextAnchor.MiddleCenter);

		m_MessageText = UiFactory.CreateText("Message", battleScreen, m_Font, "대결할 적 우주선을 선택하세요 (전투당 활동력 " + ActivityCost + ")", 26, new Color(0.7f, 0.72f, 0.78f, 1f), TextAnchor.MiddleCenter);
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

		var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
		contentObject.layer = battleScreen.gameObject.layer;
		contentObject.transform.SetParent(viewportRect, false);
		m_ListContent = (RectTransform)contentObject.transform;
		m_ListContent.anchorMin = new Vector2(0f, 1f);
		m_ListContent.anchorMax = new Vector2(1f, 1f);
		m_ListContent.pivot = new Vector2(0.5f, 1f);

		var layout = contentObject.GetComponent<VerticalLayoutGroup>();
		layout.spacing = 16f;
		layout.padding = new RectOffset(12, 12, 12, 12);
		layout.childControlWidth = true;
		layout.childControlHeight = false;
		layout.childForceExpandWidth = true;
		layout.childForceExpandHeight = false;

		var sizeFitter = contentObject.GetComponent<ContentSizeFitter>();
		sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		var scrollRect = scrollViewObject.GetComponent<ScrollRect>();
		scrollRect.content = m_ListContent;
		scrollRect.viewport = viewportRect;
		scrollRect.horizontal = false;
		scrollRect.vertical = true;
		scrollRect.movementType = ScrollRect.MovementType.Elastic;
		scrollRect.scrollSensitivity = 30f;
	}

	/// <summary>
	/// 적 목록을 무작위로 생성한다.
	/// </summary>
	private void RegenerateEnemies()
	{
		m_Enemies.Clear();
		for (int index = 0; index < 6; index++)
		{
			m_Enemies.Add(CreateRandomEnemy());
		}
	}

	/// <summary>
	/// 무작위 적 우주선을 만든다.
	/// </summary>
	private EnemyShip CreateRandomEnemy()
	{
		var weaponCount = Random.Range(0, 4);
		var armorCount = Random.Range(0, 4);
		var engineCount = Random.Range(0, 3);

		var stats = new ShipStats();
		stats.Attack = 2 + weaponCount * ModuleCatalog.Get(ModuleType.Weapon).Attack;
		stats.Health = 60 + armorCount * ModuleCatalog.Get(ModuleType.Armor).Health + weaponCount * ModuleCatalog.Get(ModuleType.Weapon).Health;
		stats.Speed = 1 + engineCount * ModuleCatalog.Get(ModuleType.Engine).Speed;
		stats.Power = stats.Attack * 3 + stats.Health + stats.Speed * 2;

		var enemy = new EnemyShip();
		enemy.Name = s_EnemyNames[Random.Range(0, s_EnemyNames.Length)];
		enemy.Stats = stats;
		return enemy;
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

		for (int index = 0; index < m_Enemies.Count; index++)
		{
			CreateEnemyRow(m_Enemies[index]);
		}
	}

	/// <summary>
	/// 적 한 줄을 생성한다.
	/// </summary>
	private void CreateEnemyRow(EnemyShip enemy)
	{
		var row = UiFactory.CreateImage("Enemy", m_ListContent, new Color(0.14f, 0.15f, 0.19f, 1f));
		var layoutElement = row.AddComponent<LayoutElement>();
		layoutElement.preferredHeight = 150f;

		var name = UiFactory.CreateText("Name", row.transform, m_Font, enemy.Name, 36, Color.white, TextAnchor.MiddleLeft);
		var nameRect = name.rectTransform;
		nameRect.anchorMin = new Vector2(0f, 0.5f);
		nameRect.anchorMax = new Vector2(0.6f, 1f);
		nameRect.offsetMin = new Vector2(28f, 0f);
		nameRect.offsetMax = new Vector2(0f, 0f);

		var power = UiFactory.CreateText("Power", row.transform, m_Font, "전투력 " + enemy.Stats.Power, 30, new Color(1f, 0.7f, 0.5f, 1f), TextAnchor.MiddleLeft);
		var powerRect = power.rectTransform;
		powerRect.anchorMin = new Vector2(0f, 0f);
		powerRect.anchorMax = new Vector2(0.6f, 0.5f);
		powerRect.offsetMin = new Vector2(28f, 0f);
		powerRect.offsetMax = new Vector2(0f, 0f);

		var fightObject = new GameObject("Fight", typeof(RectTransform), typeof(Image), typeof(Button));
		fightObject.layer = m_ListContent.gameObject.layer;
		var fightRect = (RectTransform)fightObject.transform;
		fightRect.SetParent(row.transform, false);
		fightRect.anchorMin = new Vector2(1f, 0.5f);
		fightRect.anchorMax = new Vector2(1f, 0.5f);
		fightRect.pivot = new Vector2(1f, 0.5f);
		fightRect.sizeDelta = new Vector2(220f, 100f);
		fightRect.anchoredPosition = new Vector2(-24f, 0f);
		fightObject.GetComponent<Image>().color = new Color(0.75f, 0.25f, 0.3f, 1f);
		UiFactory.CreateText("Label", fightObject.transform, m_Font, "전투하기", 32, Color.white, TextAnchor.MiddleCenter);

		var capturedEnemy = enemy;
		fightObject.GetComponent<Button>().onClick.AddListener(() => OnFight(capturedEnemy));
	}

	/// <summary>
	/// 전투하기. 활동력을 소모하고 자동 전투를 시작한다.
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

		m_MessageText.text = "대결할 적 우주선을 선택하세요 (전투당 활동력 " + ActivityCost + ")";
		StartCoroutine(RunBattle(enemy));
	}

	/// <summary>
	/// 자동 전투를 연출하고 결과를 처리한다.
	/// </summary>
	private IEnumerator RunBattle(EnemyShip enemy)
	{
		m_Busy = true;
		m_Overlay.SetActive(true);
		m_ConfirmButton.SetActive(false);
		m_ResultText.gameObject.SetActive(false);

		var playerStats = m_ShipBuilder != null ? m_ShipBuilder.GetStats() : DefaultStats();
		var enemyStats = enemy.Stats;
		m_PlayerLabel.text = "내 함선  (전투력 " + playerStats.Power + ")";
		m_EnemyLabel.text = enemy.Name + "  (전투력 " + enemyStats.Power + ")";

		var playerAttack = Mathf.Max(1, playerStats.Attack) * Random.Range(0.85f, 1.15f);
		var enemyAttack = Mathf.Max(1, enemyStats.Attack) * Random.Range(0.85f, 1.15f);
		var playerHealth = Mathf.Max(1, playerStats.Health);
		var enemyHealth = Mathf.Max(1, enemyStats.Health);

		var timeToKillEnemy = enemyHealth / playerAttack;
		var timeToKillPlayer = playerHealth / enemyAttack;
		var playerWins = timeToKillEnemy <= timeToKillPlayer;
		var battleTime = Mathf.Min(timeToKillEnemy, timeToKillPlayer);

		SetHp(m_PlayerHpFill, 1f);
		SetHp(m_EnemyHpFill, 1f);

		var duration = 2.4f;
		var elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			var simulated = (elapsed / duration) * battleTime;
			var enemyRatio = Mathf.Clamp01(1f - (playerAttack * simulated) / enemyHealth);
			var playerRatio = Mathf.Clamp01(1f - (enemyAttack * simulated) / playerHealth);
			SetHp(m_PlayerHpFill, playerRatio);
			SetHp(m_EnemyHpFill, enemyRatio);
			m_BeamObject.GetComponent<Image>().color = new Color(1f, 0.85f, 0.4f, 0.3f + 0.5f * Mathf.Abs(Mathf.Sin(elapsed * 18f)));
			yield return null;
		}

		m_ResultText.gameObject.SetActive(true);
		if (playerWins)
		{
			SetHp(m_EnemyHpFill, 0f);
			var reward = Mathf.RoundToInt(enemyStats.Power * 0.4f) + Random.Range(10, 40);
			m_PlayerState.AddCurrency(reward);
			m_PlayerState.RegisterWin();
			m_ResultText.text = "승리!\n재화 +" + reward + "   경험치 +1";
			m_ResultText.color = new Color(0.6f, 1f, 0.6f, 1f);
		}
		else
		{
			SetHp(m_PlayerHpFill, 0f);
			m_ResultText.text = "패배...";
			m_ResultText.color = new Color(1f, 0.5f, 0.5f, 1f);
		}

		m_ConfirmButton.SetActive(true);
	}

	/// <summary>
	/// 전투 결과 확인. 오버레이를 닫고 적 목록을 갱신한다.
	/// </summary>
	private void OnConfirm()
	{
		m_Overlay.SetActive(false);
		m_Busy = false;
		RegenerateEnemies();
		RefreshList();
	}

	/// <summary>
	/// 전투 오버레이 화면을 구성한다(기본 숨김).
	/// </summary>
	private void BuildOverlay(Transform canvasTransform)
	{
		m_Overlay = UiFactory.CreateImage("BattleOverlay", canvasTransform, new Color(0.03f, 0.04f, 0.08f, 1f));
		var overlayRect = (RectTransform)m_Overlay.transform;
		overlayRect.anchorMin = new Vector2(0f, 0f);
		overlayRect.anchorMax = new Vector2(1f, 1f);
		overlayRect.offsetMin = new Vector2(0f, 0f);
		overlayRect.offsetMax = new Vector2(0f, 0f);
		m_Overlay.transform.SetAsLastSibling();

		UiFactory.CreateText("Title", m_Overlay.transform, m_Font, "전투", 60, new Color(1f, 0.9f, 0.6f, 1f), TextAnchor.UpperCenter).rectTransform.anchoredPosition = new Vector2(0f, -80f);

		m_EnemyLabel = CreateLabel("EnemyLabel", new Vector2(0f, 620f), new Color(1f, 0.7f, 0.7f, 1f));
		m_EnemyHpFill = CreateHpBar("EnemyHp", new Vector2(0f, 560f), new Color(0.85f, 0.3f, 0.3f, 1f));
		var enemyShip = UiFactory.CreateImage("EnemyShip", m_Overlay.transform, new Color(0.8f, 0.35f, 0.35f, 1f));
		PlaceCenter((RectTransform)enemyShip.transform, new Vector2(0f, 420f), new Vector2(180f, 180f));

		m_BeamObject = UiFactory.CreateImage("Beam", m_Overlay.transform, new Color(1f, 0.85f, 0.4f, 0.4f));
		PlaceCenter((RectTransform)m_BeamObject.transform, new Vector2(0f, 130f), new Vector2(24f, 420f));

		var playerShip = UiFactory.CreateImage("PlayerShip", m_Overlay.transform, new Color(0.3f, 0.8f, 0.8f, 1f));
		PlaceCenter((RectTransform)playerShip.transform, new Vector2(0f, -160f), new Vector2(180f, 180f));
		m_PlayerHpFill = CreateHpBar("PlayerHp", new Vector2(0f, -300f), new Color(0.3f, 0.8f, 0.85f, 1f));
		m_PlayerLabel = CreateLabel("PlayerLabel", new Vector2(0f, -360f), new Color(0.7f, 0.95f, 1f, 1f));

		m_ResultText = UiFactory.CreateText("Result", m_Overlay.transform, m_Font, "", 56, Color.white, TextAnchor.MiddleCenter);
		PlaceCenter(m_ResultText.rectTransform, new Vector2(0f, -560f), new Vector2(800f, 160f));
		m_ResultText.gameObject.SetActive(false);

		m_ConfirmButton = new GameObject("Confirm", typeof(RectTransform), typeof(Image), typeof(Button));
		m_ConfirmButton.layer = m_Overlay.layer;
		m_ConfirmButton.transform.SetParent(m_Overlay.transform, false);
		PlaceCenter((RectTransform)m_ConfirmButton.transform, new Vector2(0f, -740f), new Vector2(320f, 110f));
		m_ConfirmButton.GetComponent<Image>().color = new Color(0.2f, 0.55f, 0.6f, 1f);
		UiFactory.CreateText("Label", m_ConfirmButton.transform, m_Font, "확인", 40, Color.white, TextAnchor.MiddleCenter);
		m_ConfirmButton.GetComponent<Button>().onClick.AddListener(OnConfirm);

		m_Overlay.SetActive(false);
	}

	/// <summary>
	/// 오버레이 중앙 기준 라벨을 생성한다.
	/// </summary>
	private TMP_Text CreateLabel(string name, Vector2 anchoredPosition, Color color)
	{
		var label = UiFactory.CreateText(name, m_Overlay.transform, m_Font, "", 34, color, TextAnchor.MiddleCenter);
		PlaceCenter(label.rectTransform, anchoredPosition, new Vector2(900f, 50f));
		return label;
	}

	/// <summary>
	/// 오버레이 중앙 기준 HP 바를 생성하고 채움 RectTransform을 반환한다.
	/// </summary>
	private RectTransform CreateHpBar(string name, Vector2 anchoredPosition, Color color)
	{
		var background = UiFactory.CreateImage(name, m_Overlay.transform, new Color(0.1f, 0.1f, 0.13f, 1f));
		PlaceCenter((RectTransform)background.transform, anchoredPosition, new Vector2(600f, 40f));

		var fill = UiFactory.CreateImage("Fill", background.transform, color);
		var fillRect = (RectTransform)fill.transform;
		fillRect.anchorMin = new Vector2(0f, 0f);
		fillRect.anchorMax = new Vector2(1f, 1f);
		fillRect.offsetMin = new Vector2(0f, 0f);
		fillRect.offsetMax = new Vector2(0f, 0f);
		return fillRect;
	}

	/// <summary>
	/// HP 바의 채움 비율을 설정한다.
	/// </summary>
	private void SetHp(RectTransform fillRect, float ratio)
	{
		fillRect.anchorMin = new Vector2(0f, 0f);
		fillRect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
		fillRect.offsetMin = new Vector2(0f, 0f);
		fillRect.offsetMax = new Vector2(0f, 0f);
	}

	/// <summary>
	/// 오버레이 중앙(앵커 0.5, 0.5) 기준으로 배치한다.
	/// </summary>
	private void PlaceCenter(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
	{
		rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.sizeDelta = size;
		rectTransform.anchoredPosition = anchoredPosition;
	}

	/// <summary>
	/// 함선 빌더가 없을 때의 기본 스탯.
	/// </summary>
	private ShipStats DefaultStats()
	{
		var stats = new ShipStats();
		stats.Attack = 2;
		stats.Health = 60;
		stats.Speed = 1;
		stats.Power = stats.Attack * 3 + stats.Health + stats.Speed * 2;
		return stats;
	}
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


/// <summary>
/// 전투 매니저(전투 씬). 탑뷰 3D 공간에 플레이어/적 함선을 배치하고 교전시킨다.
/// 나가기(경고 후 패배 처리), 결과 팝업(승패/경험치/재화/아이템 보상)을 제공한다.
/// 드래그로 화면(공간)을 이동할 수 있다(BattleCameraController).
/// </summary>
public class BattleManager : MonoBehaviour
{
	private BattleShip m_Player;
	private BattleShip m_Enemy;
	private bool m_Finished;
	private float m_Elapsed;
	private Camera m_BattleCamera;
	private RectTransform m_HudCanvas;

	private GameObject m_ExitButton;
	private GameObject m_WarningPanel;
	private GameObject m_ResultPanel;
	private TMPro.TMP_Text m_ResultTitle;
	private TMPro.TMP_Text m_ExperienceLine;
	private TMPro.TMP_Text m_CurrencyLine;
	private TMPro.TMP_Text m_ItemLine;

	/// <summary>
	/// 초기화됨. 환경과 함선, UI를 구성한다.
	/// </summary>
	private void Start()
	{
		SceneManager.SetActiveScene(gameObject.scene);
		Time.timeScale = 1f;

		SetupEnvironment();
		BuildHealthHud();

		m_Player = SpawnShip(BattleContext.PlayerLayout, new Color(0.3f, 0.82f, 0.85f, 1f), false, new Vector3(0f, 0f, -4f));
		m_Enemy = SpawnShip(BattleContext.EnemyLayout, new Color(0.88f, 0.42f, 0.42f, 1f), true, new Vector3(Random.Range(-4f, 4f), 0f, 26f));
		m_Player.SetOpponent(m_Enemy);
		m_Enemy.SetOpponent(m_Player);

		BuildUi();
	}

	/// <summary>
	/// 매 프레임 승패를 확인한다.
	/// </summary>
	private void Update()
	{
		if (m_Finished)
		{
			return;
		}

		m_Elapsed += Time.deltaTime;

		if (!m_Enemy.IsAlive)
		{
			Finish(true);
			return;
		}

		if (!m_Player.IsAlive)
		{
			Finish(false);
			return;
		}

		if (m_Elapsed > 40f)
		{
			Finish(m_Player.Vitality >= m_Enemy.Vitality);
		}
	}

	/// <summary>
	/// 카메라/조명/맵 경계를 구성한다.
	/// </summary>
	private void SetupEnvironment()
	{
		var cameraObject = new GameObject("BattleCamera");
		cameraObject.transform.position = new Vector3(0f, 20f, -10f);
		cameraObject.transform.LookAt(new Vector3(0f, 0f, 3f));
		var camera = cameraObject.AddComponent<Camera>();
		camera.fieldOfView = 52f;
		camera.clearFlags = CameraClearFlags.SolidColor;
		camera.backgroundColor = new Color(0.02f, 0.03f, 0.06f, 1f);
		camera.farClipPlane = 200f;
		m_BattleCamera = camera;
		cameraObject.AddComponent<BattleCameraController>();

		var lightObject = new GameObject("BattleLight");
		lightObject.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
		var light = lightObject.AddComponent<Light>();
		light.type = LightType.Directional;
		light.intensity = 1.1f;
		light.color = new Color(0.9f, 0.93f, 1f, 1f);

		var skydomePrefab = Resources.Load<GameObject>("Modules/Skydome");
		if (skydomePrefab != null)
		{
			Instantiate(skydomePrefab);
		}

		var borderColor = new Color(0.2f, 0.5f, 0.7f, 1f);
		CreateBar(new Vector3(0f, 0f, 15f), new Vector3(30f, 0.1f, 0.25f), borderColor);
		CreateBar(new Vector3(0f, 0f, -15f), new Vector3(30f, 0.1f, 0.25f), borderColor);
		CreateBar(new Vector3(15f, 0f, 0f), new Vector3(0.25f, 0.1f, 30f), borderColor);
		CreateBar(new Vector3(-15f, 0f, 0f), new Vector3(0.25f, 0.1f, 30f), borderColor);
	}

	/// <summary>
	/// 맵 경계 막대를 생성한다.
	/// </summary>
	private void CreateBar(Vector3 position, Vector3 scale, Color color)
	{
		var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
		bar.name = "Border";
		Destroy(bar.GetComponent<Collider>());
		bar.transform.position = position;
		bar.transform.localScale = scale;
		bar.GetComponent<Renderer>().material = MaterialFactory.CreateLit(color, color * 1.2f, 0f, 0.5f, false);
	}

	/// <summary>
	/// 함선을 스폰한다.
	/// </summary>
	private BattleShip SpawnShip(System.Collections.Generic.List<ModulePlacement> layout, Color coreColor, bool isEnemy, Vector3 position)
	{
		var shipObject = new GameObject(isEnemy ? "EnemyShip" : "PlayerShip");
		shipObject.transform.position = position;
		var ship = shipObject.AddComponent<BattleShip>();
		ship.Build(layout, coreColor, isEnemy, m_BattleCamera, m_HudCanvas);
		return ship;
	}

	/// <summary>
	/// 체력바용 HUD 캔버스(오버레이)를 만든다.
	/// </summary>
	private void BuildHealthHud()
	{
		var canvasObject = new GameObject("HealthHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		canvasObject.layer = LayerMask.NameToLayer("UI");
		var canvas = canvasObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 50;
		var scaler = canvasObject.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1080f, 1920f);
		scaler.matchWidthOrHeight = 0.5f;
		m_HudCanvas = (RectTransform)canvasObject.transform;
	}

	/// <summary>
	/// 전투 UI(나가기 버튼/경고 팝업/결과 팝업)를 구성한다.
	/// </summary>
	private void BuildUi()
	{
		var canvasObject = new GameObject("BattleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		canvasObject.layer = LayerMask.NameToLayer("UI");
		var canvas = canvasObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 100;
		var scaler = canvasObject.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1080f, 1920f);
		scaler.matchWidthOrHeight = 0.5f;
		var canvasTransform = canvasObject.transform;

		BuildExitButton(canvasTransform);
		BuildWarningPopup(canvasTransform);
		BuildResultPopup(canvasTransform);
	}

	/// <summary>
	/// 나가기 버튼을 만든다(우상단).
	/// </summary>
	private void BuildExitButton(Transform parent)
	{
		m_ExitButton = UiFactory.CreateImage("ExitButton", parent, new Color(0.6f, 0.2f, 0.24f, 1f));
		var rect = (RectTransform)m_ExitButton.transform;
		rect.anchorMin = new Vector2(1f, 1f);
		rect.anchorMax = new Vector2(1f, 1f);
		rect.pivot = new Vector2(1f, 1f);
		rect.sizeDelta = new Vector2(190f, 92f);
		rect.anchoredPosition = new Vector2(-24f, -24f);
		var button = m_ExitButton.AddComponent<Button>();
		button.onClick.AddListener(OnExitClicked);
		var label = UiFactory.CreateText("Label", m_ExitButton.transform, null, "나가기", 36, Color.white, TextAnchor.MiddleCenter);
		label.raycastTarget = false;
	}

	/// <summary>
	/// 나가기 경고 팝업을 만든다(기본 숨김).
	/// </summary>
	private void BuildWarningPopup(Transform parent)
	{
		m_WarningPanel = UiFactory.CreateImage("WarningDim", parent, new Color(0f, 0f, 0f, 0.65f));
		var dimRect = (RectTransform)m_WarningPanel.transform;
		dimRect.anchorMin = new Vector2(0f, 0f);
		dimRect.anchorMax = new Vector2(1f, 1f);
		dimRect.offsetMin = new Vector2(0f, 0f);
		dimRect.offsetMax = new Vector2(0f, 0f);

		var panel = UiFactory.CreateImage("Panel", m_WarningPanel.transform, new Color(0.1f, 0.11f, 0.15f, 1f));
		var panelRect = (RectTransform)panel.transform;
		panelRect.anchorMin = new Vector2(0.5f, 0.5f);
		panelRect.anchorMax = new Vector2(0.5f, 0.5f);
		panelRect.pivot = new Vector2(0.5f, 0.5f);
		panelRect.sizeDelta = new Vector2(800f, 420f);

		var message = UiFactory.CreateText("Message", panel.transform, null, "전투에서 나가면 패배로 처리됩니다.\n나가시겠습니까?", 38, Color.white, TextAnchor.MiddleCenter);
		var messageRect = message.rectTransform;
		messageRect.anchorMin = new Vector2(0f, 0.35f);
		messageRect.anchorMax = new Vector2(1f, 1f);
		messageRect.offsetMin = new Vector2(20f, 0f);
		messageRect.offsetMax = new Vector2(-20f, 0f);

		var cancelButton = CreateButton(panel.transform, "Cancel", "취소", new Color(0.3f, 0.34f, 0.4f, 1f), new Vector2(0.28f, 0f), OnWarningCancel);
		var exitButton = CreateButton(panel.transform, "Exit", "나가기", new Color(0.75f, 0.25f, 0.3f, 1f), new Vector2(0.72f, 0f), OnWarningConfirm);

		m_WarningPanel.SetActive(false);
	}

	/// <summary>
	/// 결과 팝업을 만든다(기본 숨김).
	/// </summary>
	private void BuildResultPopup(Transform parent)
	{
		m_ResultPanel = UiFactory.CreateImage("ResultDim", parent, new Color(0f, 0f, 0f, 0.6f));
		var dimRect = (RectTransform)m_ResultPanel.transform;
		dimRect.anchorMin = new Vector2(0f, 0f);
		dimRect.anchorMax = new Vector2(1f, 1f);
		dimRect.offsetMin = new Vector2(0f, 0f);
		dimRect.offsetMax = new Vector2(0f, 0f);

		var panel = UiFactory.CreateImage("Panel", m_ResultPanel.transform, new Color(0.09f, 0.1f, 0.14f, 1f));
		var panelRect = (RectTransform)panel.transform;
		panelRect.anchorMin = new Vector2(0.5f, 0.5f);
		panelRect.anchorMax = new Vector2(0.5f, 0.5f);
		panelRect.pivot = new Vector2(0.5f, 0.5f);
		panelRect.sizeDelta = new Vector2(840f, 720f);

		m_ResultTitle = CreateCenteredText(panel.transform, "Title", 64, Color.white, 250f, new Vector2(760f, 110f));
		m_ExperienceLine = CreateCenteredText(panel.transform, "ExperienceLine", 40, Color.white, 90f, new Vector2(700f, 70f));
		m_CurrencyLine = CreateCenteredText(panel.transform, "CurrencyLine", 40, Color.white, 10f, new Vector2(700f, 70f));
		m_ItemLine = CreateCenteredText(panel.transform, "ItemLine", 40, Color.white, -70f, new Vector2(700f, 70f));

		CreateButton(panel.transform, "Confirm", "확인", new Color(0.2f, 0.55f, 0.6f, 1f), new Vector2(0.5f, 0f), OnConfirm);

		m_ResultPanel.SetActive(false);
	}

	/// <summary>
	/// 패널 하단(앵커 minX/maxX 비율) 위치에 버튼을 만든다.
	/// </summary>
	private GameObject CreateButton(Transform parent, string name, string content, Color color, Vector2 anchor, UnityEngine.Events.UnityAction onClick)
	{
		var buttonObject = UiFactory.CreateImage(name, parent, color);
		var rect = (RectTransform)buttonObject.transform;
		rect.anchorMin = new Vector2(anchor.x, 0f);
		rect.anchorMax = new Vector2(anchor.x, 0f);
		rect.pivot = new Vector2(0.5f, 0f);
		rect.sizeDelta = new Vector2(300f, 110f);
		rect.anchoredPosition = new Vector2(0f, 40f);
		var button = buttonObject.AddComponent<Button>();
		button.onClick.AddListener(onClick);
		var label = UiFactory.CreateText("Label", buttonObject.transform, null, content, 40, Color.white, TextAnchor.MiddleCenter);
		label.raycastTarget = false;
		return buttonObject;
	}

	/// <summary>
	/// 패널 중앙 기준 텍스트를 만든다.
	/// </summary>
	private TMPro.TMP_Text CreateCenteredText(Transform parent, string name, int fontSize, Color color, float anchoredY, Vector2 size)
	{
		var text = UiFactory.CreateText(name, parent, null, "", fontSize, color, TextAnchor.MiddleCenter);
		var rect = text.rectTransform;
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.sizeDelta = size;
		rect.anchoredPosition = new Vector2(0f, anchoredY);
		return text;
	}

	/// <summary>
	/// 나가기 클릭. 경고 팝업을 띄우고 전투를 일시정지한다.
	/// </summary>
	private void OnExitClicked()
	{
		m_WarningPanel.SetActive(true);
		Time.timeScale = 0f;
	}

	/// <summary>
	/// 경고 취소. 전투를 재개한다.
	/// </summary>
	private void OnWarningCancel()
	{
		m_WarningPanel.SetActive(false);
		Time.timeScale = 1f;
	}

	/// <summary>
	/// 경고 확인(나가기). 패배로 처리한다.
	/// </summary>
	private void OnWarningConfirm()
	{
		m_WarningPanel.SetActive(false);
		Finish(false);
	}

	/// <summary>
	/// 전투를 종료하고 보상을 계산해 결과를 표시한다.
	/// </summary>
	private void Finish(bool playerWon)
	{
		if (m_Finished)
		{
			return;
		}

		m_Finished = true;
		Time.timeScale = 1f;
		m_ExitButton.SetActive(false);

		BattleContext.ResultPlayerWon = playerWon;
		if (playerWon)
		{
			BattleContext.ResultCurrency = Mathf.RoundToInt(BattleContext.EnemyPower * 0.4f) + Random.Range(10, 40);
			BattleContext.ResultExperience = 1;
			BattleContext.ResultHasItem = Random.value < 0.5f;
			BattleContext.ResultItem = (ModuleType)Random.Range(0, 3);
		}
		else
		{
			BattleContext.ResultCurrency = 0;
			BattleContext.ResultExperience = 0;
			BattleContext.ResultHasItem = false;
		}

		ShowResult(playerWon);
	}

	/// <summary>
	/// 결과 팝업 내용을 채우고 표시한다.
	/// </summary>
	private void ShowResult(bool playerWon)
	{
		var gainColor = new Color(0.6f, 1f, 0.65f, 1f);
		var missColor = new Color(0.55f, 0.57f, 0.62f, 1f);

		m_ResultTitle.text = playerWon ? "승리!" : "패배...";
		m_ResultTitle.color = playerWon ? gainColor : new Color(1f, 0.5f, 0.5f, 1f);

		m_ExperienceLine.text = "경험치        " + (playerWon ? "+1" : "+0");
		m_ExperienceLine.color = playerWon ? gainColor : missColor;

		m_CurrencyLine.text = "재화           " + (playerWon ? "+" + BattleContext.ResultCurrency : "+0");
		m_CurrencyLine.color = playerWon ? gainColor : missColor;

		if (playerWon && BattleContext.ResultHasItem)
		{
			var definition = ModuleCatalog.Get(BattleContext.ResultItem);
			m_ItemLine.text = "아이템        " + definition.DisplayName + " 모듈 획득";
			m_ItemLine.color = gainColor;
		}
		else
		{
			m_ItemLine.text = "아이템        없음";
			m_ItemLine.color = missColor;
		}

		m_ResultPanel.SetActive(true);
	}

	/// <summary>
	/// 결과 확인. 메인 씬에 결과를 알린다.
	/// </summary>
	private void OnConfirm()
	{
		Time.timeScale = 1f;
		var callback = BattleContext.OnFinished;
		if (callback != null)
		{
			callback();
		}
	}
}

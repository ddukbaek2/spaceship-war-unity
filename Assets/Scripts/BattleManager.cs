using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 전투 매니저(전투 씬). 탑뷰 3D 공간에 플레이어/적 함선을 배치하고
/// 적이 시야 밖에서 접근하여 교전하게 한다. 승패가 결정되면 결과를 표시하고
/// 콜백으로 메인 씬에 알린다.
/// </summary>
public class BattleManager : MonoBehaviour
{
	private BattleShip m_Player;
	private BattleShip m_Enemy;
	private bool m_Finished;
	private float m_Elapsed;
	private bool m_PlayerWon;
	private int m_Reward;

	private GameObject m_ResultPanel;
	private TMPro.TMP_Text m_ResultText;
	private GameObject m_ConfirmButton;

	/// <summary>
	/// 초기화됨. 환경과 함선을 구성한다.
	/// </summary>
	private void Start()
	{
		SetupEnvironment();

		m_Player = SpawnShip(BattleContext.PlayerLayout, new Color(0.3f, 0.82f, 0.85f, 1f), false, new Vector3(0f, 0f, -4f));
		m_Enemy = SpawnShip(BattleContext.EnemyLayout, new Color(0.88f, 0.42f, 0.42f, 1f), true, new Vector3(Random.Range(-4f, 4f), 0f, 26f));
		m_Player.SetOpponent(m_Enemy);
		m_Enemy.SetOpponent(m_Player);

		BuildResultUi();
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

		var lightObject = new GameObject("BattleLight");
		lightObject.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
		var light = lightObject.AddComponent<Light>();
		light.type = LightType.Directional;
		light.intensity = 1.1f;
		light.color = new Color(0.9f, 0.93f, 1f, 1f);

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
		var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
		material.SetColor("_BaseColor", color);
		material.EnableKeyword("_EMISSION");
		material.SetColor("_EmissionColor", color * 1.2f);
		material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
		bar.GetComponent<Renderer>().material = material;
	}

	/// <summary>
	/// 함선을 스폰한다.
	/// </summary>
	private BattleShip SpawnShip(System.Collections.Generic.List<ModulePlacement> layout, Color coreColor, bool isEnemy, Vector3 position)
	{
		var shipObject = new GameObject(isEnemy ? "EnemyShip" : "PlayerShip");
		shipObject.transform.position = position;
		var ship = shipObject.AddComponent<BattleShip>();
		ship.Build(layout, coreColor, isEnemy);
		return ship;
	}

	/// <summary>
	/// 결과 UI(오버레이 캔버스)를 구성한다(기본 숨김).
	/// </summary>
	private void BuildResultUi()
	{
		var canvasObject = new GameObject("BattleResultCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		canvasObject.layer = LayerMask.NameToLayer("UI");
		var canvas = canvasObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 100;
		var scaler = canvasObject.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1080f, 1920f);
		scaler.matchWidthOrHeight = 0.5f;

		m_ResultPanel = UiFactory.CreateImage("Panel", canvasObject.transform, new Color(0.08f, 0.09f, 0.13f, 0.96f));
		var panelRect = (RectTransform)m_ResultPanel.transform;
		panelRect.anchorMin = new Vector2(0.5f, 0.5f);
		panelRect.anchorMax = new Vector2(0.5f, 0.5f);
		panelRect.pivot = new Vector2(0.5f, 0.5f);
		panelRect.sizeDelta = new Vector2(760f, 460f);
		panelRect.anchoredPosition = new Vector2(0f, 0f);

		m_ResultText = UiFactory.CreateText("Result", m_ResultPanel.transform, null, "", 56, Color.white, TextAnchor.MiddleCenter);
		var resultRect = m_ResultText.rectTransform;
		resultRect.anchorMin = new Vector2(0f, 0.35f);
		resultRect.anchorMax = new Vector2(1f, 1f);
		resultRect.offsetMin = new Vector2(0f, 0f);
		resultRect.offsetMax = new Vector2(0f, 0f);

		m_ConfirmButton = UiFactory.CreateImage("Confirm", m_ResultPanel.transform, new Color(0.2f, 0.55f, 0.6f, 1f));
		var confirmRect = (RectTransform)m_ConfirmButton.transform;
		confirmRect.anchorMin = new Vector2(0.5f, 0f);
		confirmRect.anchorMax = new Vector2(0.5f, 0f);
		confirmRect.pivot = new Vector2(0.5f, 0f);
		confirmRect.sizeDelta = new Vector2(320f, 110f);
		confirmRect.anchoredPosition = new Vector2(0f, 48f);
		var confirmButton = m_ConfirmButton.AddComponent<Button>();
		confirmButton.onClick.AddListener(OnConfirm);
		var confirmLabel = UiFactory.CreateText("Label", m_ConfirmButton.transform, null, "확인", 40, Color.white, TextAnchor.MiddleCenter);
		confirmLabel.raycastTarget = false;

		m_ResultPanel.SetActive(false);
	}

	/// <summary>
	/// 전투를 종료하고 결과를 표시한다.
	/// </summary>
	private void Finish(bool playerWon)
	{
		m_Finished = true;
		m_PlayerWon = playerWon;
		m_Reward = playerWon ? Mathf.RoundToInt(BattleContext.EnemyPower * 0.4f) + Random.Range(10, 40) : 0;

		if (playerWon)
		{
			m_ResultText.text = "승리!\n재화 +" + m_Reward + "   경험치 +1";
			m_ResultText.color = new Color(0.6f, 1f, 0.6f, 1f);
		}
		else
		{
			m_ResultText.text = "패배...";
			m_ResultText.color = new Color(1f, 0.5f, 0.5f, 1f);
		}

		m_ResultPanel.SetActive(true);
	}

	/// <summary>
	/// 결과 확인. 메인 씬에 결과를 알린다.
	/// </summary>
	private void OnConfirm()
	{
		var callback = BattleContext.OnFinished;
		if (callback != null)
		{
			callback(m_PlayerWon, m_Reward);
		}
	}
}

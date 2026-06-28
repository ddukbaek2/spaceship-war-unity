using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 전투 함선. 정면(로컬 +Z) 기준으로 추진(전진)하고 추진체 성능에 따라 회전하여 상대를 겨눈다.
/// 무기 모듈은 전방으로 빔을 발사한다. 각 모듈은 개별 체력을 가지며 파괴되면 떨어져 나가고,
/// 코어와 끊긴 모듈도 이탈한다. 모듈/코어 체력은 화면을 따라다니는 uGUI HUD로 표시한다
/// (최대 체력이면 숨김). 코어가 파괴되면 격침된다.
/// </summary>
public class BattleShip : MonoBehaviour
{
	private const float CellSize = 1f;
	private const float ModuleHeight = 0.34f;
	private const float CoreHeight = 0.42f;

	private static readonly Vector2Int[] s_Directions = new Vector2Int[]
	{
		new Vector2Int(0, 1),
		new Vector2Int(0, -1),
		new Vector2Int(-1, 0),
		new Vector2Int(1, 0),
	};

	private static readonly Vector2Int s_CoreCell = new Vector2Int(0, 0);

	/// <summary>
	/// 모듈 한 칸의 상태.
	/// </summary>
	private class ModuleData
	{
		public GameObject Object;
		public ModuleType Type;
		public float Health;
		public float MaxHealth;
		public RectTransform BarRoot;
		public RectTransform BarFill;
	}

	private readonly Dictionary<Vector2Int, ModuleData> m_Modules = new Dictionary<Vector2Int, ModuleData>();
	private GameObject m_CoreObject;
	private float m_CoreHealth;
	private float m_CoreMaxHealth;
	private RectTransform m_CoreBarRoot;
	private RectTransform m_CoreBarFill;

	private BattleShip m_Opponent;
	private Camera m_Camera;
	private RectTransform m_HudCanvas;

	private float m_MoveSpeed = 2.5f;
	private float m_TurnSpeed = 80f;
	private float m_WeaponRange = 9f;
	private float m_DetectionRange = 13f;
	private float m_FireInterval = 0.9f;
	private float m_FireCooldown;
	private float m_BobPhase;

	private bool m_ManualControl;
	private Vector2 m_MoveInput;
	private HashSet<Vector2Int> m_ActiveModules;

	/// <summary>
	/// 함선 생존 여부(코어 생존).
	/// </summary>
	public bool IsAlive
	{
		get { return m_CoreHealth > 0f; }
	}

	/// <summary>
	/// 남은 전력(코어 체력 + 모듈 수). 타임아웃 판정용.
	/// </summary>
	public float Vitality
	{
		get { return m_CoreHealth + m_Modules.Count * 12f; }
	}

	/// <summary>
	/// 함선을 구성한다.
	/// </summary>
	public void Build(List<ModulePlacement> layout, Color coreColor, bool isEnemy, Camera camera, RectTransform hudCanvas)
	{
		m_Camera = camera;
		m_HudCanvas = hudCanvas;
		m_CoreMaxHealth = 10f;
		m_CoreHealth = m_CoreMaxHealth;
		m_CoreObject = ModuleFactory.CreateCore(transform, new Vector3(0f, 0f, 0f));
		TintEmission(m_CoreObject, coreColor);
		CreateBar(out m_CoreBarRoot, out m_CoreBarFill, new Color(0.4f, 0.95f, 1f, 1f));

		m_BobPhase = isEnemy ? 1.7f : 0f;

		m_ActiveModules = isEnemy ? null : ModuleCatalog.ComputeActive(layout);

		var engineCount = 0;
		if (layout != null)
		{
			foreach (var placement in layout)
			{
				if (placement.Coordinate == s_CoreCell)
				{
					continue;
				}

				var definition = ModuleCatalog.Get(placement.Type);
				var moduleData = new ModuleData();
				moduleData.Object = ModuleFactory.CreateModule(placement.Type, transform, ToLocal(placement.Coordinate));
				moduleData.Type = placement.Type;
				moduleData.MaxHealth = Mathf.Max(12f, definition.Health);
				moduleData.Health = moduleData.MaxHealth;
				CreateBar(out moduleData.BarRoot, out moduleData.BarFill, new Color(1f, 0.7f, 0.4f, 1f));
				m_Modules[placement.Coordinate] = moduleData;

				if (!IsModuleActive(placement.Coordinate))
				{
					TintEmission(moduleData.Object, new Color(0.22f, 0.22f, 0.26f, 1f));
				}

				if (ModuleCatalog.GetCategory(placement.Type) == ModuleCategory.Engine && IsModuleActive(placement.Coordinate))
				{
					engineCount++;
				}
			}
		}

		m_MoveSpeed = 2.2f + engineCount * 0.7f;
		m_TurnSpeed = 70f + engineCount * 45f;
	}

	/// <summary>
	/// 모듈이 동력으로 작동 중인지 확인한다(적은 항상 작동).
	/// </summary>
	private bool IsModuleActive(Vector2Int coordinate)
	{
		return m_ActiveModules == null || m_ActiveModules.Contains(coordinate);
	}

	/// <summary>
	/// 상대 함선을 지정한다.
	/// </summary>
	public void SetOpponent(BattleShip opponent)
	{
		m_Opponent = opponent;
	}

	/// <summary>
	/// 수동 조작 여부를 설정한다(켜면 조이스틱 입력으로 이동).
	/// </summary>
	public void SetManualControl(bool manual)
	{
		m_ManualControl = manual;
	}

	/// <summary>
	/// 수동 이동 입력을 설정한다(x: 가로, y: 세로).
	/// </summary>
	public void SetMoveInput(Vector2 input)
	{
		m_MoveInput = input;
	}

	/// <summary>
	/// 매 프레임 이동/사격/체력바를 갱신한다.
	/// </summary>
	private void Update()
	{
		if (IsAlive && m_Opponent != null && m_Opponent.IsAlive)
		{
			var toOpponent = m_Opponent.transform.position - transform.position;
			toOpponent.y = 0f;
			var distance = toOpponent.magnitude;
			if (m_ManualControl)
			{
				ManualMove();
			}
			else
			{
				MoveAndFace(toOpponent, distance);
			}

			if (distance <= m_DetectionRange)
			{
				m_FireCooldown -= Time.deltaTime;
				if (m_FireCooldown <= 0f)
				{
					Fire();
					m_FireCooldown = m_FireInterval;
				}
			}
		}

		UpdateBars();
	}

	/// <summary>
	/// 정면(+Z)이 상대를 향하도록 회전하고 전진한다.
	/// </summary>
	private void MoveAndFace(Vector3 toOpponent, float distance)
	{
		var desired = distance > 0.001f ? new Vector3(toOpponent.x, 0f, toOpponent.z).normalized : transform.forward;
		if (desired.sqrMagnitude > 0.001f)
		{
			var targetRotation = Quaternion.LookRotation(desired, Vector3.up);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, m_TurnSpeed * Time.deltaTime);
		}

		var holdDistance = m_WeaponRange * 0.7f;
		var thrust = 0f;
		if (distance > holdDistance + 0.5f)
		{
			thrust = m_MoveSpeed;
		}
		else if (distance < holdDistance - 0.5f)
		{
			thrust = -m_MoveSpeed * 0.5f;
		}

		var bob = Mathf.Sin(Time.time * 1.4f + m_BobPhase) * 0.12f;
		var position = transform.position + transform.forward * thrust * Time.deltaTime;
		position.y = bob;
		transform.position = position;
	}

	/// <summary>
	/// 조이스틱 입력으로 이동/회전한다(입력 방향으로 정면을 돌리고 전진).
	/// </summary>
	private void ManualMove()
	{
		var input = new Vector3(m_MoveInput.x, 0f, m_MoveInput.y);
		var magnitude = Mathf.Clamp01(input.magnitude);
		if (magnitude > 0.05f)
		{
			var desired = input.normalized;
			var targetRotation = Quaternion.LookRotation(desired, Vector3.up);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, m_TurnSpeed * Time.deltaTime);
		}

		var bob = Mathf.Sin(Time.time * 1.4f + m_BobPhase) * 0.12f;
		var position = transform.position + transform.forward * m_MoveSpeed * magnitude * Time.deltaTime;
		position.y = bob;
		transform.position = position;
	}

	/// <summary>
	/// 무기 모듈에서 전방(빈 공간)으로 빔을 발사한다. 무기가 없으면 코어가 약하게 사격한다.
	/// </summary>
	private void Fire()
	{
		var fired = false;
		foreach (var moduleCell in m_Modules)
		{
			if (ModuleCatalog.GetCategory(moduleCell.Value.Type) != ModuleCategory.Weapon || moduleCell.Value.Object == null)
			{
				continue;
			}

			if (!IsModuleActive(moduleCell.Key))
			{
				continue;
			}

			var definition = ModuleCatalog.Get(moduleCell.Value.Type);
			var muzzle = moduleCell.Value.Object.transform.position + transform.forward * (CellSize * 0.6f);
			SpawnProjectile(muzzle, transform.forward, definition.Attack, 0.26f, new Color(1f, 0.5f, 0.4f, 1f));
			fired = true;
		}

		if (!fired && m_CoreObject != null)
		{
			var muzzle = m_CoreObject.transform.position + transform.forward * (CellSize * 0.6f);
			SpawnProjectile(muzzle, transform.forward, 1f, 0.07f, new Color(0.5f, 0.9f, 1f, 1f));
		}
	}

	/// <summary>
	/// 빔 투사체를 생성한다.
	/// </summary>
	private void SpawnProjectile(Vector3 start, Vector3 direction, float damage, float thickness, Color color)
	{
		var projectileObject = new GameObject("Laser");
		var projectile = projectileObject.AddComponent<BattleProjectile>();
		projectile.Launch(start, direction, m_Opponent, damage, 16f, thickness, color);
	}

	/// <summary>
	/// 현재 위치에서 가장 가까운 살아있는 칸(코어/모듈)을 반환한다.
	/// </summary>
	public bool TryGetNearestCell(Vector3 worldPosition, out Vector2Int coordinate, out Vector3 cellWorld)
	{
		coordinate = s_CoreCell;
		cellWorld = transform.position;
		var found = false;
		var best = float.MaxValue;

		if (m_CoreObject != null)
		{
			best = (m_CoreObject.transform.position - worldPosition).sqrMagnitude;
			cellWorld = m_CoreObject.transform.position;
			found = true;
		}

		foreach (var moduleCell in m_Modules)
		{
			if (moduleCell.Value.Object == null)
			{
				continue;
			}

			var sqr = (moduleCell.Value.Object.transform.position - worldPosition).sqrMagnitude;
			if (sqr < best)
			{
				best = sqr;
				coordinate = moduleCell.Key;
				cellWorld = moduleCell.Value.Object.transform.position;
				found = true;
			}
		}

		return found;
	}

	/// <summary>
	/// 대상 칸에 피해를 준다.
	/// </summary>
	public void TakeDamage(Vector2Int coordinate, float damage)
	{
		if (coordinate == s_CoreCell)
		{
			m_CoreHealth -= Mathf.Max(1f, damage - 1f);
			if (m_CoreHealth <= 0f && m_CoreObject != null)
			{
				SpawnDebris(m_CoreObject);
				m_CoreObject = null;
				DestroyBar(m_CoreBarRoot);
			}

			return;
		}

		ModuleData moduleData;
		if (!m_Modules.TryGetValue(coordinate, out moduleData))
		{
			return;
		}

		var armor = ModuleCatalog.Get(moduleData.Type).Armor;
		moduleData.Health -= Mathf.Max(1f, damage - armor);
		if (moduleData.Health > 0f)
		{
			return;
		}

		if (moduleData.Object != null)
		{
			SpawnDebris(moduleData.Object);
		}

		DestroyBar(moduleData.BarRoot);
		m_Modules.Remove(coordinate);
		PruneDisconnected();
	}

	/// <summary>
	/// 코어와 끊긴 모듈을 이탈시킨다.
	/// </summary>
	private void PruneDisconnected()
	{
		var connectedCells = new HashSet<Vector2Int>();
		var searchQueue = new Queue<Vector2Int>();
		connectedCells.Add(s_CoreCell);
		searchQueue.Enqueue(s_CoreCell);

		while (searchQueue.Count > 0)
		{
			var currentCell = searchQueue.Dequeue();
			foreach (var direction in s_Directions)
			{
				var neighborCell = currentCell + direction;
				if (m_Modules.ContainsKey(neighborCell) && !connectedCells.Contains(neighborCell))
				{
					connectedCells.Add(neighborCell);
					searchQueue.Enqueue(neighborCell);
				}
			}
		}

		var disconnectedCells = new List<Vector2Int>();
		foreach (var moduleCell in m_Modules)
		{
			if (!connectedCells.Contains(moduleCell.Key))
			{
				disconnectedCells.Add(moduleCell.Key);
			}
		}

		foreach (var disconnectedCell in disconnectedCells)
		{
			if (m_Modules[disconnectedCell].Object != null)
			{
				SpawnDebris(m_Modules[disconnectedCell].Object);
			}

			DestroyBar(m_Modules[disconnectedCell].BarRoot);
			m_Modules.Remove(disconnectedCell);
		}
	}

	/// <summary>
	/// 칸 오브젝트를 함선에서 분리해 파편으로 흘려보낸다.
	/// </summary>
	private void SpawnDebris(GameObject cellObject)
	{
		cellObject.transform.SetParent(null, true);
		var debris = cellObject.AddComponent<BattleDebris>();
		var outward = cellObject.transform.position - transform.position;
		outward.y = 0f;
		if (outward.sqrMagnitude < 0.01f)
		{
			outward = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
		}

		debris.Begin(outward.normalized * Random.Range(1.5f, 3f) + Vector3.up * Random.Range(0.3f, 1f));
	}

	/// <summary>
	/// 체력바를 갱신한다(만피면 숨김, 화면 추적).
	/// </summary>
	private void UpdateBars()
	{
		if (m_CoreObject != null)
		{
			PositionBar(m_CoreBarRoot, m_CoreBarFill, m_CoreObject.transform.position, m_CoreHealth, m_CoreMaxHealth);
		}

		foreach (var moduleCell in m_Modules)
		{
			if (moduleCell.Value.Object != null)
			{
				PositionBar(moduleCell.Value.BarRoot, moduleCell.Value.BarFill, moduleCell.Value.Object.transform.position, moduleCell.Value.Health, moduleCell.Value.MaxHealth);
			}
		}
	}

	/// <summary>
	/// 체력바 하나를 배치/표시한다.
	/// </summary>
	private void PositionBar(RectTransform barRoot, RectTransform barFill, Vector3 worldPosition, float health, float maxHealth)
	{
		if (barRoot == null || m_Camera == null || m_HudCanvas == null)
		{
			return;
		}

		if (health >= maxHealth - 0.01f)
		{
			barRoot.gameObject.SetActive(false);
			return;
		}

		var screen = m_Camera.WorldToScreenPoint(worldPosition);
		if (screen.z < 0f)
		{
			barRoot.gameObject.SetActive(false);
			return;
		}

		Vector2 localPoint;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(m_HudCanvas, screen, null, out localPoint);
		barRoot.anchoredPosition = localPoint + new Vector2(0f, 40f);
		barRoot.gameObject.SetActive(true);
		barFill.anchorMax = new Vector2(Mathf.Clamp01(health / maxHealth), 1f);
	}

	/// <summary>
	/// 체력바(배경+채움)를 생성한다.
	/// </summary>
	private void CreateBar(out RectTransform barRoot, out RectTransform barFill, Color fillColor)
	{
		var rootObject = new GameObject("HpBar", typeof(RectTransform), typeof(Image));
		rootObject.transform.SetParent(m_HudCanvas, false);
		barRoot = (RectTransform)rootObject.transform;
		barRoot.sizeDelta = new Vector2(86f, 12f);
		rootObject.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.07f, 0.85f);

		var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
		fillObject.transform.SetParent(barRoot, false);
		barFill = (RectTransform)fillObject.transform;
		barFill.anchorMin = new Vector2(0f, 0f);
		barFill.anchorMax = new Vector2(1f, 1f);
		barFill.offsetMin = new Vector2(2f, 2f);
		barFill.offsetMax = new Vector2(-2f, -2f);
		fillObject.GetComponent<Image>().color = fillColor;

		rootObject.SetActive(false);
	}

	/// <summary>
	/// 체력바를 제거한다.
	/// </summary>
	private void DestroyBar(RectTransform barRoot)
	{
		if (barRoot != null)
		{
			Destroy(barRoot.gameObject);
		}
	}

	/// <summary>
	/// 격자 좌표를 로컬 위치로 변환한다.
	/// </summary>
	private Vector3 ToLocal(Vector2Int coordinate)
	{
		return new Vector3(coordinate.x * CellSize, 0f, coordinate.y * CellSize);
	}

	/// <summary>
	/// 오브젝트의 모든 렌더러 발광색을 팀 색으로 덮어쓴다(아군/적 구분).
	/// </summary>
	private void TintEmission(GameObject target, Color color)
	{
		if (target == null)
		{
			return;
		}

		var renderers = target.GetComponentsInChildren<Renderer>();
		var block = new MaterialPropertyBlock();
		for (int index = 0; index < renderers.Length; index++)
		{
			renderers[index].GetPropertyBlock(block);
			block.SetColor("_EmissionColor", color);
			renderers[index].SetPropertyBlock(block);
		}
	}
}


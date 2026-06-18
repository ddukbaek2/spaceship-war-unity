using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 전투 함선. 탑뷰 3D 공간에서 상대에게 접근/감지하여 교전한다.
/// 각 모듈은 개별 체력을 가지며, 파괴되면 떨어져 나가고 코어와 끊긴 모듈도 이탈한다.
/// 코어가 파괴되면 함선이 격침된다.
/// </summary>
public class BattleShip : MonoBehaviour
{
	private const float CellSize = 1f;
	private const float ModuleHeight = 0.3f;
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
	private struct ModuleData
	{
		public GameObject Object;
		public ModuleType Type;
		public float Health;
	}

	private readonly Dictionary<Vector2Int, ModuleData> m_Modules = new Dictionary<Vector2Int, ModuleData>();
	private GameObject m_CoreObject;
	private float m_CoreHealth;
	private BattleShip m_Opponent;

	private float m_MoveSpeed = 3.5f;
	private float m_WeaponRange = 8f;
	private float m_DetectionRange = 12f;
	private float m_FireInterval = 0.9f;
	private float m_FireCooldown;
	private float m_BobPhase;
	private float m_StrafeSign = 1f;

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
	public void Build(List<ModulePlacement> layout, Color coreColor, bool isEnemy)
	{
		m_CoreHealth = 120f;
		m_CoreObject = CreateCube("Core", s_CoreCell, CoreHeight, coreColor, coreColor * 0.9f);
		m_BobPhase = isEnemy ? 1.7f : 0f;
		m_StrafeSign = isEnemy ? 1f : -1f;

		if (layout != null)
		{
			foreach (var placement in layout)
			{
				if (placement.Coordinate == s_CoreCell)
				{
					continue;
				}

				var definition = ModuleCatalog.Get(placement.Type);
				var moduleObject = CreateCube("Module", placement.Coordinate, ModuleHeight, definition.Color * 0.7f, definition.Color * 0.5f);

				var moduleData = new ModuleData();
				moduleData.Object = moduleObject;
				moduleData.Type = placement.Type;
				moduleData.Health = Mathf.Max(12f, definition.Health);
				m_Modules[placement.Coordinate] = moduleData;
			}
		}
	}

	/// <summary>
	/// 상대 함선을 지정한다.
	/// </summary>
	public void SetOpponent(BattleShip opponent)
	{
		m_Opponent = opponent;
	}

	/// <summary>
	/// 매 프레임 이동과 사격을 처리한다.
	/// </summary>
	private void Update()
	{
		if (!IsAlive || m_Opponent == null || !m_Opponent.IsAlive)
		{
			return;
		}

		var toOpponent = m_Opponent.transform.position - transform.position;
		toOpponent.y = 0f;
		var distance = toOpponent.magnitude;
		MoveAndFace(toOpponent, distance);

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

	/// <summary>
	/// 상대를 향해 이동·선회한다(사거리 유지 + 둥둥 부유 + 좌우 기동).
	/// </summary>
	private void MoveAndFace(Vector3 toOpponent, float distance)
	{
		var direction = distance > 0.001f ? toOpponent / distance : Vector3.forward;
		var holdDistance = m_WeaponRange * 0.7f;

		var move = Vector3.zero;
		if (distance > holdDistance + 0.4f)
		{
			move += direction * m_MoveSpeed * Time.deltaTime;
		}
		else if (distance < holdDistance - 0.4f)
		{
			move -= direction * m_MoveSpeed * 0.6f * Time.deltaTime;
		}

		var strafe = new Vector3(-direction.z, 0f, direction.x);
		move += strafe * m_StrafeSign * Mathf.Sin(Time.time * 0.7f + m_BobPhase) * m_MoveSpeed * 0.35f * Time.deltaTime;

		var bob = Mathf.Sin(Time.time * 1.4f + m_BobPhase) * 0.15f;
		var position = transform.position + move;
		position.y = bob;
		transform.position = position;

		var flatDirection = new Vector3(direction.x, 0f, direction.z);
		if (flatDirection.sqrMagnitude > 0.001f)
		{
			var targetRotation = Quaternion.LookRotation(flatDirection, Vector3.up);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 3f);
		}
	}

	/// <summary>
	/// 무기 모듈에서 상대 모듈로 사격한다. 무기가 없으면 코어가 약하게 사격한다.
	/// </summary>
	private void Fire()
	{
		var fired = false;
		foreach (var moduleCell in m_Modules)
		{
			if (moduleCell.Value.Type != ModuleType.Weapon)
			{
				continue;
			}

			var definition = ModuleCatalog.Get(ModuleType.Weapon);
			var start = moduleCell.Value.Object != null ? moduleCell.Value.Object.transform.position : transform.position;
			var targetCoordinate = m_Opponent.PickRandomTarget();
			SpawnProjectile(start, targetCoordinate, definition.Attack, new Color(1f, 0.5f, 0.4f, 1f));
			fired = true;
		}

		if (!fired && m_CoreObject != null)
		{
			var targetCoordinate = m_Opponent.PickRandomTarget();
			SpawnProjectile(m_CoreObject.transform.position, targetCoordinate, 4f, new Color(0.5f, 0.9f, 1f, 1f));
		}
	}

	/// <summary>
	/// 투사체를 생성한다.
	/// </summary>
	private void SpawnProjectile(Vector3 start, Vector2Int targetCoordinate, float damage, Color color)
	{
		var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
		projectileObject.name = "Projectile";
		var projectile = projectileObject.AddComponent<BattleProjectile>();
		projectile.Launch(start, m_Opponent, targetCoordinate, damage, 14f, color);
	}

	/// <summary>
	/// 상대가 사격할 대상 칸을 무작위로 고른다(모듈 우선, 없으면 코어).
	/// </summary>
	public Vector2Int PickRandomTarget()
	{
		if (m_Modules.Count > 0)
		{
			var index = Random.Range(0, m_Modules.Count);
			var current = 0;
			foreach (var moduleCell in m_Modules)
			{
				if (current == index)
				{
					return moduleCell.Key;
				}

				current++;
			}
		}

		return s_CoreCell;
	}

	/// <summary>
	/// 대상 칸의 월드 위치를 반환한다.
	/// </summary>
	public Vector3 GetTargetWorldPosition(Vector2Int coordinate)
	{
		if (coordinate == s_CoreCell && m_CoreObject != null)
		{
			return m_CoreObject.transform.position;
		}

		ModuleData moduleData;
		if (m_Modules.TryGetValue(coordinate, out moduleData) && moduleData.Object != null)
		{
			return moduleData.Object.transform.position;
		}

		if (m_CoreObject != null)
		{
			return m_CoreObject.transform.position;
		}

		return transform.position;
	}

	/// <summary>
	/// 대상 칸에 피해를 준다.
	/// </summary>
	public void TakeDamage(Vector2Int coordinate, float damage)
	{
		if (coordinate == s_CoreCell)
		{
			m_CoreHealth -= damage;
			if (m_CoreHealth <= 0f && m_CoreObject != null)
			{
				SpawnDebris(m_CoreObject);
				m_CoreObject = null;
			}

			return;
		}

		ModuleData moduleData;
		if (!m_Modules.TryGetValue(coordinate, out moduleData))
		{
			return;
		}

		moduleData.Health -= damage;
		if (moduleData.Health > 0f)
		{
			m_Modules[coordinate] = moduleData;
			return;
		}

		if (moduleData.Object != null)
		{
			SpawnDebris(moduleData.Object);
		}

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

			m_Modules.Remove(disconnectedCell);
		}
	}

	/// <summary>
	/// 큐브를 함선 자식으로 생성한다.
	/// </summary>
	private GameObject CreateCube(string name, Vector2Int coordinate, float height, Color baseColor, Color emission)
	{
		var cubeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
		cubeObject.name = name;
		cubeObject.transform.SetParent(transform, false);
		cubeObject.transform.localPosition = new Vector3(coordinate.x * CellSize, height * 0.5f, coordinate.y * CellSize);
		cubeObject.transform.localScale = new Vector3(CellSize * 0.96f, height, CellSize * 0.96f);

		var renderer = cubeObject.GetComponent<Renderer>();
		var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
		material.SetColor("_BaseColor", baseColor);
		material.SetFloat("_Metallic", 0.5f);
		material.SetFloat("_Smoothness", 0.7f);
		material.EnableKeyword("_EMISSION");
		material.SetColor("_EmissionColor", emission);
		material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
		renderer.material = material;
		return cubeObject;
	}

	/// <summary>
	/// 칸 오브젝트를 함선에서 분리해 파편으로 흘려보낸다.
	/// </summary>
	private void SpawnDebris(GameObject cellObject)
	{
		cellObject.transform.SetParent(null, true);
		var debris = cellObject.AddComponent<BattleDebris>();
		var outward = (cellObject.transform.position - transform.position);
		outward.y = 0f;
		if (outward.sqrMagnitude < 0.01f)
		{
			outward = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
		}

		debris.Begin(outward.normalized * Random.Range(1.5f, 3f) + Vector3.up * Random.Range(0.3f, 1f));
	}
}

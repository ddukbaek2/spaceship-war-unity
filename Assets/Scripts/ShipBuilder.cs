using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


/// <summary>
/// 함선 조립기(개조 화면 뷰). PlayerState의 장착 모듈을 읽어 탑뷰 2.5D로 렌더한다.
/// 인벤토리에서 모듈을 선택하면 빈 칸(슬롯)이 표시되고, 슬롯을 탭하면 장착되며
/// 장착된 모듈을 탭하면 장착 해제된다. 함선은 둥둥 떠다닌다.
/// </summary>
public class ShipBuilder : MonoBehaviour
{
	#region INSPECTOR
	[SerializeField] private float m_CellSize = 1f;
	[SerializeField] private float m_ModuleHeight = 0.18f;
	[SerializeField] private float m_SlotHeight = 0.07f;
	[SerializeField] private float m_BobAmplitude = 0.12f;
	[SerializeField] private float m_BobSpeed = 1.2f;
	[SerializeField] private bool m_AutoSpin = false;
	[SerializeField] private float m_SpinSpeed = 6f;
	[SerializeField] private Color m_CoreColor = new Color(0.16f, 0.7f, 0.72f, 1f);
	#endregion

	private static readonly Vector2Int[] s_Directions = new Vector2Int[]
	{
		new Vector2Int(0, 1),
		new Vector2Int(0, -1),
		new Vector2Int(-1, 0),
		new Vector2Int(1, 0),
	};

	private static readonly Vector2Int s_CoreCell = new Vector2Int(0, 0);

	private struct CellReference
	{
		public Vector2Int Coordinate;
		public bool IsSlot;
	}

	private readonly Dictionary<GameObject, CellReference> m_CellByObject = new Dictionary<GameObject, CellReference>();
	private readonly List<GameObject> m_SlotObjects = new List<GameObject>();

	private static readonly Color s_SlotBaseColor = new Color(0.25f, 1f, 0.4f, 1f);
	private static readonly Color s_SlotEmissionColor = new Color(0.15f, 0.85f, 0.3f, 1f);
	private static readonly Color s_FocusEmissionColor = new Color(1f, 0.95f, 0.4f, 1f);

	private GameObject m_FocusObject;
	private Renderer[] m_FocusRenderers;
	private MaterialPropertyBlock m_FocusBlock;

	private Vector3 m_BasePosition;
	private Camera m_Camera;
	private PlayerState m_PlayerState;

	/// <summary>
	/// 초기화됨.
	/// </summary>
	private void Start()
	{
		m_BasePosition = transform.localPosition;
		m_Camera = Camera.main;
		m_PlayerState = FindFirstObjectByType<PlayerState>();
		if (m_PlayerState != null)
		{
			m_PlayerState.Changed += Rebuild;
		}

		Rebuild();
	}

	/// <summary>
	/// 파괴됨.
	/// </summary>
	private void OnDestroy()
	{
		if (m_PlayerState != null)
		{
			m_PlayerState.Changed -= Rebuild;
		}
	}

	/// <summary>
	/// 매 프레임 갱신. 부유 모션과 포인터 입력을 처리한다.
	/// </summary>
	private void Update()
	{
		var bobOffset = Mathf.Sin(Time.time * m_BobSpeed) * m_BobAmplitude;
		transform.localPosition = m_BasePosition + new Vector3(0f, bobOffset, 0f);

		if (m_AutoSpin)
		{
			transform.Rotate(Vector3.up, m_SpinSpeed * Time.deltaTime, Space.World);
		}

		HandlePointer();
		PulseEffects();
	}

	/// <summary>
	/// 장착 가능한 슬롯과 선택된 장착 모듈을 깜빡이게 강조한다.
	/// </summary>
	private void PulseEffects()
	{
		var pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);

		for (int index = 0; index < m_SlotObjects.Count; index++)
		{
			var renderer = m_SlotObjects[index].GetComponent<Renderer>();
			if (!renderer.enabled)
			{
				continue;
			}

			var material = renderer.material;
			if (material.HasProperty("_BaseColor"))
			{
				var color = s_SlotBaseColor;
				color.a = Mathf.Lerp(0.2f, 0.55f, pulse);
				material.SetColor("_BaseColor", color);
			}

			if (material.HasProperty("_EmissionColor"))
			{
				material.SetColor("_EmissionColor", s_SlotEmissionColor * Mathf.Lerp(0.3f, 1.3f, pulse));
			}
		}

		if (m_FocusObject != null && m_FocusRenderers != null)
		{
			var focusPulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6.5f);
			var emission = s_FocusEmissionColor * Mathf.Lerp(0.1f, 1.4f, focusPulse);
			for (int index = 0; index < m_FocusRenderers.Length; index++)
			{
				if (m_FocusRenderers[index] == null)
				{
					continue;
				}

				m_FocusRenderers[index].GetPropertyBlock(m_FocusBlock);
				m_FocusBlock.SetColor("_EmissionColor", emission);
				m_FocusRenderers[index].SetPropertyBlock(m_FocusBlock);
			}

			var scale = 1f + 0.06f * focusPulse;
			m_FocusObject.transform.localScale = new Vector3(scale, scale, scale);
		}
	}

	/// <summary>
	/// 포인터 입력 처리. 선택 중이면 슬롯 탭으로 장착, 아니면 모듈 탭으로 해제.
	/// </summary>
	private void HandlePointer()
	{
		if (m_PlayerState == null)
		{
			return;
		}

		var pointer = Pointer.current;
		if (pointer == null || !pointer.press.wasPressedThisFrame)
		{
			return;
		}

		var picked = Pick(pointer.position.ReadValue());
		if (!picked.HasValue)
		{
			return;
		}

		if (picked.Value.IsSlot)
		{
			var selectedId = m_PlayerState.SelectedId;
			if (selectedId != -1)
			{
				var selected = m_PlayerState.GetModule(selectedId);
				if (selected != null && !selected.Equipped)
				{
					m_PlayerState.TryEquip(selectedId, picked.Value.Coordinate);
				}
			}

			return;
		}

		if (picked.Value.Coordinate == s_CoreCell)
		{
			m_PlayerState.ClearSelection();
			return;
		}

		var instanceId = m_PlayerState.GetEquippedInstanceAt(picked.Value.Coordinate);
		if (instanceId != -1)
		{
			m_PlayerState.SelectModule(instanceId);
		}
	}

	/// <summary>
	/// 현재 장착된 모듈 배치를 반환한다(전투 씬 전달용).
	/// </summary>
	public List<ModulePlacement> GetLayout()
	{
		if (m_PlayerState != null)
		{
			return m_PlayerState.GetEquipped();
		}

		return new List<ModulePlacement>();
	}

	/// <summary>
	/// 코어/모듈/슬롯을 모두 다시 생성한다.
	/// </summary>
	public void Rebuild()
	{
		ClearChildren();

		CreateCore();

		var focusCoordinate = s_CoreCell;
		var hasFocus = false;
		if (m_PlayerState != null && m_PlayerState.SelectedId != -1)
		{
			var selected = m_PlayerState.GetModule(m_PlayerState.SelectedId);
			if (selected != null && selected.Equipped)
			{
				focusCoordinate = selected.Coordinate;
				hasFocus = true;
			}
		}

		var equipped = m_PlayerState != null ? m_PlayerState.GetEquipped() : new List<ModulePlacement>();
		foreach (var placement in equipped)
		{
			var moduleRoot = CreateModule(placement.Coordinate, placement.Type);
			if (hasFocus && placement.Coordinate == focusCoordinate && moduleRoot != null)
			{
				m_FocusObject = moduleRoot;
				m_FocusRenderers = moduleRoot.GetComponentsInChildren<Renderer>();
				m_FocusBlock = new MaterialPropertyBlock();
			}
		}

		CreateConnectors(equipped);

		var slotCells = CollectSlots(equipped);
		foreach (var slotCell in slotCells)
		{
			CreateSlot(slotCell);
		}

		var showSlots = false;
		if (m_PlayerState != null && m_PlayerState.SelectedId != -1)
		{
			var selected = m_PlayerState.GetModule(m_PlayerState.SelectedId);
			showSlots = selected != null && !selected.Equipped;
		}

		SetSlotsVisible(showSlots);
	}

	/// <summary>
	/// 인접한 부착 칸 사이에 연결 블록을 붙인다(연결되는 느낌).
	/// </summary>
	private void CreateConnectors(List<ModulePlacement> equipped)
	{
		var occupied = new HashSet<Vector2Int>();
		occupied.Add(s_CoreCell);
		for (int index = 0; index < equipped.Count; index++)
		{
			occupied.Add(equipped[index].Coordinate);
		}

		var pairDirections = new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(0, 1) };
		foreach (var cell in occupied)
		{
			for (int d = 0; d < pairDirections.Length; d++)
			{
				var neighbor = cell + pairDirections[d];
				if (occupied.Contains(neighbor))
				{
					var midLocal = (ToLocal(cell) + ToLocal(neighbor)) * 0.5f;
					CreateConnector(midLocal, pairDirections[d]);
				}
			}
		}
	}

	/// <summary>
	/// 연결 블록 하나를 생성한다.
	/// </summary>
	private void CreateConnector(Vector3 localPosition, Vector2Int direction)
	{
		var connector = GameObject.CreatePrimitive(PrimitiveType.Cube);
		connector.name = "Connector";
		Destroy(connector.GetComponent<Collider>());
		connector.transform.SetParent(transform, false);
		var along = m_CellSize * 0.34f;
		var across = m_CellSize * 0.22f;
		var sizeX = direction.x != 0 ? along : across;
		var sizeZ = direction.y != 0 ? along : across;
		connector.transform.localPosition = new Vector3(localPosition.x, 0.22f, localPosition.z);
		connector.transform.localScale = new Vector3(sizeX, 0.34f, sizeZ);
		connector.GetComponent<Renderer>().material = MaterialFactory.CreateLit(new Color(0.7f, 0.74f, 0.8f, 1f), new Color(0.2f, 0.22f, 0.26f, 1f), 0.7f, 0.7f, false);
	}

	/// <summary>
	/// 점유된 칸(코어+장착 모듈)에 인접한 빈 칸을 슬롯으로 수집한다.
	/// </summary>
	private HashSet<Vector2Int> CollectSlots(List<ModulePlacement> equipped)
	{
		var occupiedCells = new HashSet<Vector2Int>();
		occupiedCells.Add(s_CoreCell);
		for (int index = 0; index < equipped.Count; index++)
		{
			occupiedCells.Add(equipped[index].Coordinate);
		}

		var slotCells = new HashSet<Vector2Int>();
		foreach (var occupiedCell in occupiedCells)
		{
			foreach (var direction in s_Directions)
			{
				var neighborCell = occupiedCell + direction;
				if (!occupiedCells.Contains(neighborCell))
				{
					slotCells.Add(neighborCell);
				}
			}
		}

		return slotCells;
	}

	/// <summary>
	/// 화면 좌표가 가리키는 칸 참조를 반환한다.
	/// </summary>
	private CellReference? Pick(Vector2 screenPosition)
	{
		if (m_Camera == null)
		{
			return null;
		}

		var ray = m_Camera.ScreenPointToRay(screenPosition);
		RaycastHit hit;
		if (!Physics.Raycast(ray, out hit))
		{
			return null;
		}

		CellReference cellReference;
		if (!m_CellByObject.TryGetValue(hit.collider.gameObject, out cellReference))
		{
			return null;
		}

		return cellReference;
	}

	/// <summary>
	/// 코어 모듈을 생성한다.
	/// </summary>
	private void CreateCore()
	{
		var root = ModuleFactory.CreateCore(transform, ToLocal(s_CoreCell));
		if (root == null)
		{
			return;
		}

		root.name = "CoreModule";
		var cellReference = new CellReference();
		cellReference.Coordinate = s_CoreCell;
		cellReference.IsSlot = false;
		m_CellByObject.Add(root, cellReference);
	}

	/// <summary>
	/// 격자 좌표를 로컬 위치로 변환한다.
	/// </summary>
	private Vector3 ToLocal(Vector2Int coordinate)
	{
		return new Vector3(coordinate.x * m_CellSize, 0f, coordinate.y * m_CellSize);
	}

	/// <summary>
	/// 모듈을 생성한다.
	/// </summary>
	private GameObject CreateModule(Vector2Int coordinate, ModuleType type)
	{
		var root = ModuleFactory.CreateModule(type, transform, ToLocal(coordinate));
		if (root == null)
		{
			return null;
		}

		var cellReference = new CellReference();
		cellReference.Coordinate = coordinate;
		cellReference.IsSlot = false;
		m_CellByObject.Add(root, cellReference);
		return root;
	}

	/// <summary>
	/// 슬롯 칸을 생성한다(반투명, 선택 중에만 표시).
	/// </summary>
	private void CreateSlot(Vector2Int coordinate)
	{
		var slotColor = new Color(0.25f, 1f, 0.4f, 0.42f);
		var slotEmission = new Color(0.15f, 0.85f, 0.3f, 1f);
		var slotMaterial = CreateLitMaterial(slotColor, slotEmission, 0f, 0.5f, true);
		var slotObject = CreateCell("Slot", coordinate, m_SlotHeight, 0.92f, slotMaterial, true);
		m_SlotObjects.Add(slotObject);
	}

	/// <summary>
	/// 격자 좌표에 정사각형 큐브 칸을 생성한다.
	/// </summary>
	private GameObject CreateCell(string name, Vector2Int coordinate, float height, float fill, Material material, bool isSlot)
	{
		var cellObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
		cellObject.name = name;
		cellObject.transform.SetParent(transform, false);
		cellObject.transform.localPosition = new Vector3(coordinate.x * m_CellSize, height * 0.5f, coordinate.y * m_CellSize);
		cellObject.transform.localScale = new Vector3(m_CellSize * fill, height, m_CellSize * fill);
		cellObject.GetComponent<Renderer>().material = material;

		var cellReference = new CellReference();
		cellReference.Coordinate = coordinate;
		cellReference.IsSlot = isSlot;
		m_CellByObject.Add(cellObject, cellReference);
		return cellObject;
	}

	/// <summary>
	/// 타일 위에 평평한 장식 큐브를 추가한다(충돌체 제거).
	/// </summary>
	private void CreateDecoration(Transform parentCell, Vector3 localOffset, Vector3 worldScale, Material material)
	{
		var decorationObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
		decorationObject.name = "Decoration";
		Destroy(decorationObject.GetComponent<Collider>());
		decorationObject.transform.SetParent(transform, false);
		decorationObject.transform.localPosition = parentCell.localPosition + localOffset;
		decorationObject.transform.localScale = worldScale;
		decorationObject.GetComponent<Renderer>().material = material;
	}

	/// <summary>
	/// URP/Lit 머티리얼을 생성한다.
	/// </summary>
	private Material CreateLitMaterial(Color baseColor, Color emission, float metallic, float smoothness, bool transparent)
	{
		return MaterialFactory.CreateLit(baseColor, emission, metallic, smoothness, transparent);
	}

	/// <summary>
	/// 슬롯의 표시 여부를 설정한다.
	/// </summary>
	private void SetSlotsVisible(bool visible)
	{
		for (int index = 0; index < m_SlotObjects.Count; index++)
		{
			m_SlotObjects[index].GetComponent<Renderer>().enabled = visible;
		}
	}

	/// <summary>
	/// 모든 자식 칸을 제거한다.
	/// </summary>
	private void ClearChildren()
	{
		for (int index = transform.childCount - 1; index >= 0; index--)
		{
			var childTransform = transform.GetChild(index);
			DestroyImmediate(childTransform.gameObject);
		}

		m_CellByObject.Clear();
		m_SlotObjects.Clear();
		m_FocusObject = null;
		m_FocusRenderers = null;
	}
}

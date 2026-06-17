using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


/// <summary>
/// 함선 조립기. 탑뷰 2.5D 월드 공간에서 코어 모듈을 중심으로 상하좌우에
/// 정사각형 모듈 타일을 동적으로 부착/탈착한다. 함선은 둥둥 떠다닌다.
/// 모듈은 평평하게 서로 붙어 연결되며, 타일 위에 꾸밈 장식이 올라간다.
/// 부착은 인벤토리에서 드래그하여 슬롯에 드롭하고, 모듈을 탭하면 탈착된다.
/// 슬롯은 드래그 중에만 반투명으로 표시되며, 드롭 위치에 부착 미리보기를 보여준다.
/// </summary>
public class ShipBuilder : MonoBehaviour
{
	#region INSPECTOR
	[SerializeField] private float m_CellSize = 1f;
	[SerializeField] private float m_ModuleHeight = 0.18f;
	[SerializeField] private float m_SlotHeight = 0.06f;
	[SerializeField] private float m_BobAmplitude = 0.12f;
	[SerializeField] private float m_BobSpeed = 1.2f;
	[SerializeField] private bool m_AutoSpin = false;
	[SerializeField] private float m_SpinSpeed = 6f;
	[SerializeField] private Color m_CoreColor = new Color(0.16f, 0.7f, 0.72f, 1f);
	[SerializeField] private Color m_SlotColor = new Color(0.6f, 0.7f, 0.9f, 0.3f);
	#endregion

	/// <summary>
	/// 인접 4방향(상/하/좌/우).
	/// </summary>
	private static readonly Vector2Int[] s_Directions = new Vector2Int[]
	{
		new Vector2Int(0, 1),
		new Vector2Int(0, -1),
		new Vector2Int(-1, 0),
		new Vector2Int(1, 0),
	};

	/// <summary>
	/// 코어 모듈의 격자 좌표.
	/// </summary>
	private static readonly Vector2Int s_CoreCell = new Vector2Int(0, 0);

	/// <summary>
	/// 칸 오브젝트가 가리키는 격자 좌표와 슬롯 여부.
	/// </summary>
	private struct CellReference
	{
		public Vector2Int Coordinate;
		public bool IsSlot;
	}

	/// <summary>
	/// 부착된 모듈의 격자 좌표별 종류(코어 제외).
	/// </summary>
	private readonly Dictionary<Vector2Int, ModuleType> m_ModuleCells = new Dictionary<Vector2Int, ModuleType>();

	/// <summary>
	/// 생성된 칸 오브젝트별 참조 정보.
	/// </summary>
	private readonly Dictionary<GameObject, CellReference> m_CellByObject = new Dictionary<GameObject, CellReference>();

	/// <summary>
	/// 생성된 슬롯 오브젝트 목록.
	/// </summary>
	private readonly List<GameObject> m_SlotObjects = new List<GameObject>();

	/// <summary>
	/// 부유 모션의 기준 위치.
	/// </summary>
	private Vector3 m_BasePosition;

	/// <summary>
	/// 클릭/드롭 판정에 사용할 카메라.
	/// </summary>
	private Camera m_Camera;

	/// <summary>
	/// 탈착 시 모듈을 환원할 플레이어 상태.
	/// </summary>
	private PlayerState m_PlayerState;

	/// <summary>
	/// 드래그(부착) 진행 중 여부.
	/// </summary>
	private bool m_DragActive;

	/// <summary>
	/// 초기화됨.
	/// </summary>
	private void Start()
	{
		m_BasePosition = transform.localPosition;
		m_Camera = Camera.main;
		m_PlayerState = FindFirstObjectByType<PlayerState>();
		Rebuild();
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
	}

	/// <summary>
	/// 모듈 탭을 받아 탈착한다. (슬롯 부착은 인벤토리 드래그로 처리)
	/// </summary>
	private void HandlePointer()
	{
		if (m_DragActive)
		{
			return;
		}

		var pointer = Pointer.current;
		if (pointer == null)
		{
			return;
		}

		if (!pointer.press.wasPressedThisFrame)
		{
			return;
		}

		var cellReference = Pick(pointer.position.ReadValue());
		if (cellReference.HasValue && !cellReference.Value.IsSlot)
		{
			DetachModule(cellReference.Value.Coordinate);
		}
	}

	/// <summary>
	/// 함선 종합 스탯을 계산한다(코어 기본값 + 모듈 합산).
	/// </summary>
	public ShipStats GetStats()
	{
		var stats = new ShipStats();
		stats.Attack = 2;
		stats.Health = 60;
		stats.Speed = 1;

		foreach (var moduleCell in m_ModuleCells)
		{
			var definition = ModuleCatalog.Get(moduleCell.Value);
			stats.Attack += definition.Attack;
			stats.Health += definition.Health;
			stats.Speed += definition.Speed;
		}

		stats.Power = stats.Attack * 3 + stats.Health + stats.Speed * 2;
		return stats;
	}

	/// <summary>
	/// 드래그 부착을 시작한다. 슬롯을 반투명으로 표시한다.
	/// </summary>
	public void BeginDragAttach()
	{
		m_DragActive = true;
		ResetSlotAppearance();
		SetSlotsVisible(true);
	}

	/// <summary>
	/// 드롭 위치의 슬롯에 부착 미리보기를 표시한다.
	/// </summary>
	public void UpdateAttachPreview(Vector2 screenPosition, ModuleType type)
	{
		if (!m_DragActive)
		{
			return;
		}

		ResetSlotAppearance();

		var slotObject = PickObject(screenPosition);
		if (slotObject == null || !m_CellByObject[slotObject].IsSlot)
		{
			return;
		}

		var localPosition = slotObject.transform.localPosition;
		slotObject.transform.localScale = new Vector3(m_CellSize * 0.99f, m_ModuleHeight, m_CellSize * 0.99f);
		slotObject.transform.localPosition = new Vector3(localPosition.x, m_ModuleHeight * 0.5f, localPosition.z);

		var definition = ModuleCatalog.Get(type);
		var previewColor = new Color(definition.Color.r, definition.Color.g, definition.Color.b, 0.85f);
		SetCellColor(slotObject, previewColor);
	}

	/// <summary>
	/// 드래그 부착을 종료한다. 슬롯을 숨긴다.
	/// </summary>
	public void EndDragAttach()
	{
		m_DragActive = false;
		ResetSlotAppearance();
		SetSlotsVisible(false);
	}

	/// <summary>
	/// 화면 좌표(드롭 위치)의 슬롯에 모듈을 부착한다. 성공하면 true.
	/// </summary>
	public bool TryAttachAtScreenPosition(Vector2 screenPosition, ModuleType type)
	{
		var slotObject = PickObject(screenPosition);
		if (slotObject == null || !m_CellByObject[slotObject].IsSlot)
		{
			return false;
		}

		AttachModule(m_CellByObject[slotObject].Coordinate, type);
		return true;
	}

	/// <summary>
	/// 모듈 부착.
	/// </summary>
	public void AttachModule(Vector2Int coordinate, ModuleType type)
	{
		if (coordinate == s_CoreCell)
		{
			return;
		}

		m_ModuleCells[coordinate] = type;
		Rebuild();
	}

	/// <summary>
	/// 모듈 탈착. 탈착된 모듈과, 그로 인해 코어와 끊긴 모듈을 인벤토리로 환원한다.
	/// </summary>
	public void DetachModule(Vector2Int coordinate)
	{
		ModuleType removedType;
		if (!m_ModuleCells.TryGetValue(coordinate, out removedType))
		{
			return;
		}

		m_ModuleCells.Remove(coordinate);
		RefundModule(removedType);
		PruneDisconnected();
		Rebuild();
	}

	/// <summary>
	/// 코어/모듈/슬롯을 모두 다시 생성한다.
	/// </summary>
	public void Rebuild()
	{
		ClearChildren();

		CreateCore();

		foreach (var moduleCell in m_ModuleCells)
		{
			CreateModule(moduleCell.Key, moduleCell.Value);
		}

		var slotCells = CollectSlots();
		foreach (var slotCell in slotCells)
		{
			CreateSlot(slotCell);
		}

		SetSlotsVisible(m_DragActive);
	}

	/// <summary>
	/// 점유된 칸(코어+모듈)에 인접한 빈 칸을 슬롯으로 수집한다.
	/// </summary>
	public HashSet<Vector2Int> CollectSlots()
	{
		var occupiedCells = new HashSet<Vector2Int>(m_ModuleCells.Keys);
		occupiedCells.Add(s_CoreCell);

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
	/// 코어와 연결되지 않은 모듈을 제거하고 인벤토리로 환원한다.
	/// </summary>
	public void PruneDisconnected()
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
				if (m_ModuleCells.ContainsKey(neighborCell) && !connectedCells.Contains(neighborCell))
				{
					connectedCells.Add(neighborCell);
					searchQueue.Enqueue(neighborCell);
				}
			}
		}

		var disconnectedCells = new List<Vector2Int>();
		foreach (var moduleCell in m_ModuleCells)
		{
			if (!connectedCells.Contains(moduleCell.Key))
			{
				disconnectedCells.Add(moduleCell.Key);
			}
		}

		foreach (var disconnectedCell in disconnectedCells)
		{
			RefundModule(m_ModuleCells[disconnectedCell]);
			m_ModuleCells.Remove(disconnectedCell);
		}
	}

	/// <summary>
	/// 모듈을 인벤토리로 환원한다.
	/// </summary>
	private void RefundModule(ModuleType type)
	{
		if (m_PlayerState != null)
		{
			m_PlayerState.AddModule(type);
		}
	}

	/// <summary>
	/// 화면 좌표가 가리키는 칸 참조를 반환한다.
	/// </summary>
	private CellReference? Pick(Vector2 screenPosition)
	{
		var pickedObject = PickObject(screenPosition);
		if (pickedObject == null)
		{
			return null;
		}

		return m_CellByObject[pickedObject];
	}

	/// <summary>
	/// 화면 좌표가 가리키는 칸 오브젝트를 반환한다.
	/// </summary>
	private GameObject PickObject(Vector2 screenPosition)
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

		var hitObject = hit.collider.gameObject;
		if (!m_CellByObject.ContainsKey(hitObject))
		{
			return null;
		}

		return hitObject;
	}

	/// <summary>
	/// 코어 모듈을 생성한다(평평한 타일 + 발광 장식).
	/// </summary>
	private void CreateCore()
	{
		var bodyMaterial = CreateLitMaterial(m_CoreColor, m_CoreColor * 0.8f, 0.45f, 0.7f, false);
		var coreObject = CreateCell("CoreModule", s_CoreCell, m_ModuleHeight, 0.99f, bodyMaterial, false);

		var insetMaterial = CreateLitMaterial(new Color(0.5f, 0.95f, 0.95f, 1f), new Color(0.4f, 1f, 1f, 1f), 0.3f, 0.85f, false);
		CreateDecoration(coreObject.transform, new Vector3(0f, m_ModuleHeight + 0.03f, 0f), new Vector3(m_CellSize * 0.6f, 0.06f, m_CellSize * 0.6f), insetMaterial);
		CreateDecoration(coreObject.transform, new Vector3(0f, m_ModuleHeight + 0.06f, 0f), new Vector3(m_CellSize * 0.26f, 0.08f, m_CellSize * 0.26f), insetMaterial);
	}

	/// <summary>
	/// 모듈을 생성한다(평평한 타일 + 종류별 장식).
	/// </summary>
	private void CreateModule(Vector2Int coordinate, ModuleType type)
	{
		var definition = ModuleCatalog.Get(type);
		var bodyColor = definition.Color * 0.55f;
		bodyColor.a = 1f;
		var bodyMaterial = CreateLitMaterial(bodyColor, definition.Color * 0.25f, 0.55f, 0.65f, false);
		var moduleObject = CreateCell("Module", coordinate, m_ModuleHeight, 0.99f, bodyMaterial, false);

		var insetMaterial = CreateLitMaterial(definition.Color, definition.Color * 0.7f, 0.5f, 0.75f, false);
		CreateDecoration(moduleObject.transform, new Vector3(0f, m_ModuleHeight + 0.03f, 0f), new Vector3(m_CellSize * 0.62f, 0.06f, m_CellSize * 0.62f), insetMaterial);

		var markMaterial = CreateLitMaterial(new Color(0.95f, 0.95f, 0.95f, 1f), definition.Color, 0.4f, 0.85f, false);
		var markOffset = GetMarkOffset(type);
		var markScale = GetMarkScale(type);
		CreateDecoration(moduleObject.transform, new Vector3(markOffset.x, m_ModuleHeight + 0.06f, markOffset.y), markScale, markMaterial);
	}

	/// <summary>
	/// 종류별 중심 장식의 평면 오프셋.
	/// </summary>
	private Vector2 GetMarkOffset(ModuleType type)
	{
		if (type == ModuleType.Engine)
		{
			return new Vector2(0f, -m_CellSize * 0.22f);
		}

		return new Vector2(0f, 0f);
	}

	/// <summary>
	/// 종류별 중심 장식의 크기.
	/// </summary>
	private Vector3 GetMarkScale(ModuleType type)
	{
		if (type == ModuleType.Weapon)
		{
			return new Vector3(m_CellSize * 0.16f, 0.08f, m_CellSize * 0.42f);
		}

		if (type == ModuleType.Engine)
		{
			return new Vector3(m_CellSize * 0.42f, 0.08f, m_CellSize * 0.2f);
		}

		return new Vector3(m_CellSize * 0.32f, 0.08f, m_CellSize * 0.32f);
	}

	/// <summary>
	/// 슬롯 칸을 생성한다(반투명, 기본 숨김).
	/// </summary>
	private void CreateSlot(Vector2Int coordinate)
	{
		var slotMaterial = CreateLitMaterial(m_SlotColor, Color.black, 0f, 0.3f, true);
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
	/// 칸 위에 평평한 장식 큐브를 추가한다(충돌체 제거).
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
	/// URP/Lit 머티리얼을 생성한다(불투명/투명, 이미션, 금속/매끄러움).
	/// </summary>
	private Material CreateLitMaterial(Color baseColor, Color emission, float metallic, float smoothness, bool transparent)
	{
		var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
		material.SetColor("_BaseColor", baseColor);
		material.SetFloat("_Metallic", metallic);
		material.SetFloat("_Smoothness", smoothness);

		if (emission.maxColorComponent > 0f)
		{
			material.EnableKeyword("_EMISSION");
			material.SetColor("_EmissionColor", emission);
			material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
		}

		if (transparent)
		{
			material.SetFloat("_Surface", 1f);
			material.SetFloat("_Blend", 0f);
			material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			material.SetFloat("_ZWrite", 0f);
			material.DisableKeyword("_ALPHATEST_ON");
			material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
		}

		return material;
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
	/// 모든 슬롯을 기본 모양(낮은 높이, 슬롯 색)으로 되돌린다.
	/// </summary>
	private void ResetSlotAppearance()
	{
		for (int index = 0; index < m_SlotObjects.Count; index++)
		{
			var slotObject = m_SlotObjects[index];
			var localPosition = slotObject.transform.localPosition;
			slotObject.transform.localScale = new Vector3(m_CellSize * 0.92f, m_SlotHeight, m_CellSize * 0.92f);
			slotObject.transform.localPosition = new Vector3(localPosition.x, m_SlotHeight * 0.5f, localPosition.z);
			SetCellColor(slotObject, m_SlotColor);
		}
	}

	/// <summary>
	/// 칸의 기본 색을 설정한다.
	/// </summary>
	private void SetCellColor(GameObject cellObject, Color color)
	{
		cellObject.GetComponent<Renderer>().material.SetColor("_BaseColor", color);
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
	}
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


/// <summary>
/// 함선 조립기. 탑뷰 2.5D 월드 공간에서 코어 모듈을 중심으로 상하좌우에
/// 정사각형 모듈 블록을 동적으로 부착/탈착한다. 함선은 둥둥 떠다닌다.
/// 빈 슬롯을 탭하면 모듈이 부착되고, 모듈을 탭하면 탈착된다.
/// </summary>
public class ShipBuilder : MonoBehaviour
{
	#region INSPECTOR
	[SerializeField] private float m_CellSize = 1f;
	[SerializeField] private float m_ModuleHeight = 0.4f;
	[SerializeField] private float m_SlotHeight = 0.08f;
	[SerializeField] private float m_BobAmplitude = 0.15f;
	[SerializeField] private float m_BobSpeed = 1.2f;
	[SerializeField] private bool m_AutoSpin = false;
	[SerializeField] private float m_SpinSpeed = 6f;
	[SerializeField] private Color m_CoreColor = new Color(0.2f, 0.8f, 0.8f, 1f);
	[SerializeField] private Color m_ModuleColor = new Color(0.45f, 0.65f, 0.9f, 1f);
	[SerializeField] private Color m_SlotColor = new Color(0.5f, 0.5f, 0.55f, 1f);
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
	/// 부착된 모듈의 격자 좌표 집합(코어 제외).
	/// </summary>
	private readonly HashSet<Vector2Int> m_ModuleCells = new HashSet<Vector2Int>();

	/// <summary>
	/// 생성된 칸 오브젝트별 참조 정보.
	/// </summary>
	private readonly Dictionary<GameObject, CellReference> m_CellByObject = new Dictionary<GameObject, CellReference>();

	/// <summary>
	/// 부유 모션의 기준 위치.
	/// </summary>
	private Vector3 m_BasePosition;

	/// <summary>
	/// 클릭 판정에 사용할 카메라.
	/// </summary>
	private Camera m_Camera;

	/// <summary>
	/// 초기화됨.
	/// </summary>
	private void Start()
	{
		m_BasePosition = transform.localPosition;
		m_Camera = Camera.main;
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
	/// 포인터(마우스/터치) 클릭을 받아 칸을 부착/탈착한다.
	/// </summary>
	private void HandlePointer()
	{
		var pointer = Pointer.current;
		if (pointer == null)
		{
			return;
		}

		if (!pointer.press.wasPressedThisFrame)
		{
			return;
		}

		if (m_Camera == null)
		{
			return;
		}

		var pointerPosition = pointer.position.ReadValue();
		var ray = m_Camera.ScreenPointToRay(pointerPosition);
		RaycastHit hit;
		if (!Physics.Raycast(ray, out hit))
		{
			return;
		}

		CellReference cellReference;
		if (!m_CellByObject.TryGetValue(hit.collider.gameObject, out cellReference))
		{
			return;
		}

		if (cellReference.IsSlot)
		{
			AttachModule(cellReference.Coordinate);
		}
		else
		{
			DetachModule(cellReference.Coordinate);
		}
	}

	/// <summary>
	/// 모듈 부착.
	/// </summary>
	public void AttachModule(Vector2Int coordinate)
	{
		if (coordinate == s_CoreCell)
		{
			return;
		}

		m_ModuleCells.Add(coordinate);
		Rebuild();
	}

	/// <summary>
	/// 모듈 탈착.
	/// </summary>
	public void DetachModule(Vector2Int coordinate)
	{
		if (!m_ModuleCells.Contains(coordinate))
		{
			return;
		}

		m_ModuleCells.Remove(coordinate);
		PruneDisconnected();
		Rebuild();
	}

	/// <summary>
	/// 코어/모듈/슬롯을 모두 다시 생성한다.
	/// </summary>
	public void Rebuild()
	{
		ClearChildren();

		CreateCell("CoreModule", s_CoreCell, m_CoreColor, m_ModuleHeight, false);

		foreach (var moduleCell in m_ModuleCells)
		{
			CreateCell("Module", moduleCell, m_ModuleColor, m_ModuleHeight, false);
		}

		var slotCells = CollectSlots();
		foreach (var slotCell in slotCells)
		{
			CreateCell("Slot", slotCell, m_SlotColor, m_SlotHeight, true);
		}
	}

	/// <summary>
	/// 점유된 칸(코어+모듈)에 인접한 빈 칸을 슬롯으로 수집한다.
	/// </summary>
	public HashSet<Vector2Int> CollectSlots()
	{
		var occupiedCells = new HashSet<Vector2Int>(m_ModuleCells);
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
	/// 코어와 연결되지 않은 모듈을 제거한다.
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
				if (m_ModuleCells.Contains(neighborCell) && !connectedCells.Contains(neighborCell))
				{
					connectedCells.Add(neighborCell);
					searchQueue.Enqueue(neighborCell);
				}
			}
		}

		m_ModuleCells.RemoveWhere(moduleCell => !connectedCells.Contains(moduleCell));
	}

	/// <summary>
	/// 격자 좌표에 정사각형 큐브 칸을 생성한다.
	/// </summary>
	private void CreateCell(string name, Vector2Int coordinate, Color color, float height, bool isSlot)
	{
		var cellObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
		cellObject.name = name;
		cellObject.transform.SetParent(transform, false);
		cellObject.transform.localPosition = new Vector3(coordinate.x * m_CellSize, height * 0.5f, coordinate.y * m_CellSize);
		cellObject.transform.localScale = new Vector3(m_CellSize * 0.9f, height, m_CellSize * 0.9f);

		var renderer = cellObject.GetComponent<Renderer>();
		var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
		material.SetColor("_BaseColor", color);
		renderer.material = material;

		var cellReference = new CellReference();
		cellReference.Coordinate = coordinate;
		cellReference.IsSlot = isSlot;
		m_CellByObject.Add(cellObject, cellReference);
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
	}
}

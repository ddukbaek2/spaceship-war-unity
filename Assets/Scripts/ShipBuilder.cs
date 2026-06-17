using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 함선 조립기. 코어 모듈을 중심으로 상하좌우에 정사각형 모듈을 부착/탈착한다.
/// 빈 슬롯을 탭하면 모듈이 부착되고, 모듈을 탭하면 탈착된다.
/// </summary>
public class ShipBuilder : MonoBehaviour
{
	#region INSPECTOR
	[SerializeField] private RectTransform m_ShipRoot;
	[SerializeField] private float m_CellSize = 240f;
	[SerializeField] private Color m_CoreColor = new Color(0.2f, 0.8f, 0.8f, 1f);
	[SerializeField] private Color m_ModuleColor = new Color(0.45f, 0.65f, 0.9f, 1f);
	[SerializeField] private Color m_SlotColor = new Color(1f, 1f, 1f, 0.12f);
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
	/// 부착된 모듈의 격자 좌표 집합(코어 제외).
	/// </summary>
	private readonly HashSet<Vector2Int> m_ModuleCells = new HashSet<Vector2Int>();

	/// <summary>
	/// 초기화됨.
	/// </summary>
	private void Start()
	{
		if (m_ShipRoot == null)
		{
			m_ShipRoot = (RectTransform)transform;
		}

		Rebuild();
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

		CreateImageObject("CoreModule", s_CoreCell, m_CoreColor);

		foreach (var moduleCell in m_ModuleCells)
		{
			CreateModule(moduleCell);
		}

		var slotCells = CollectSlots();
		foreach (var slotCell in slotCells)
		{
			CreateSlot(slotCell);
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
	/// 모듈 칸 생성(탭 시 탈착).
	/// </summary>
	private void CreateModule(Vector2Int coordinate)
	{
		var moduleObject = CreateImageObject("Module", coordinate, m_ModuleColor);
		var button = moduleObject.AddComponent<Button>();
		var capturedCoordinate = coordinate;
		button.onClick.AddListener(() => DetachModule(capturedCoordinate));
	}

	/// <summary>
	/// 슬롯 칸 생성(탭 시 부착).
	/// </summary>
	private void CreateSlot(Vector2Int coordinate)
	{
		var slotObject = CreateImageObject("Slot", coordinate, m_SlotColor);
		var button = slotObject.AddComponent<Button>();
		var capturedCoordinate = coordinate;
		button.onClick.AddListener(() => AttachModule(capturedCoordinate));
	}

	/// <summary>
	/// 격자 좌표에 정사각형 이미지 칸을 생성한다.
	/// </summary>
	private GameObject CreateImageObject(string name, Vector2Int coordinate, Color color)
	{
		var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
		imageObject.layer = m_ShipRoot.gameObject.layer;
		var rectTransform = (RectTransform)imageObject.transform;
		rectTransform.SetParent(m_ShipRoot, false);
		rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.sizeDelta = new Vector2(m_CellSize, m_CellSize);
		rectTransform.anchoredPosition = new Vector2(coordinate.x * m_CellSize, coordinate.y * m_CellSize);
		var image = imageObject.GetComponent<Image>();
		image.color = color;
		return imageObject;
	}

	/// <summary>
	/// 루트의 모든 자식 칸을 제거한다.
	/// </summary>
	private void ClearChildren()
	{
		for (int index = m_ShipRoot.childCount - 1; index >= 0; index--)
		{
			var childTransform = m_ShipRoot.GetChild(index);
			DestroyImmediate(childTransform.gameObject);
		}
	}
}

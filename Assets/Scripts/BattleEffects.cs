using UnityEngine;


/// <summary>
/// 전투 빔 투사체. 발사 방향(빈 공간 쪽)으로 나가며 대상 함선의 가장 가까운 모듈로
/// 약하게 유도되어 명중 시 피해를 준다. (1m 격자 기준 크기)
/// </summary>
public class BattleProjectile : MonoBehaviour
{
	private BattleShip m_TargetShip;
	private Vector3 m_Direction;
	private float m_Damage;
	private float m_Speed;
	private float m_Life;

	/// <summary>
	/// 투사체를 발사한다.
	/// </summary>
	public void Launch(Vector3 start, Vector3 direction, BattleShip targetShip, float damage, float speed, float thickness, Color color)
	{
		m_TargetShip = targetShip;
		m_Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
		m_Damage = damage;
		m_Speed = speed;
		m_Life = 2.5f;

		transform.position = start;
		transform.localScale = new Vector3(thickness, thickness, 0.7f);
		transform.rotation = Quaternion.LookRotation(m_Direction, Vector3.up);

		var collider = GetComponent<Collider>();
		if (collider != null)
		{
			Destroy(collider);
		}

		GetComponent<Renderer>().material = MaterialFactory.CreateLit(color, color * 2.2f, 0f, 0.5f, false);
	}

	/// <summary>
	/// 매 프레임 이동(약한 유도)과 명중을 처리한다.
	/// </summary>
	private void Update()
	{
		m_Life -= Time.deltaTime;
		if (m_Life <= 0f || m_TargetShip == null || !m_TargetShip.IsAlive)
		{
			Destroy(gameObject);
			return;
		}

		Vector2Int coordinate;
		Vector3 cellWorld;
		if (m_TargetShip.TryGetNearestCell(transform.position, out coordinate, out cellWorld))
		{
			var toCell = cellWorld - transform.position;
			var distance = toCell.magnitude;
			if (distance <= 0.6f)
			{
				m_TargetShip.TakeDamage(coordinate, m_Damage);
				Destroy(gameObject);
				return;
			}

			var desired = toCell / distance;
			m_Direction = Vector3.Slerp(m_Direction, desired, 2.2f * Time.deltaTime).normalized;
		}

		transform.position += m_Direction * m_Speed * Time.deltaTime;
		transform.rotation = Quaternion.LookRotation(m_Direction, Vector3.up);
	}
}


/// <summary>
/// 파괴되어 떨어져 나간 모듈 파편. 바깥으로 흘러가며 사라진다.
/// </summary>
public class BattleDebris : MonoBehaviour
{
	private Vector3 m_Velocity;
	private Vector3 m_Spin;
	private float m_Life;

	/// <summary>
	/// 파편을 시작시킨다.
	/// </summary>
	public void Begin(Vector3 velocity)
	{
		m_Velocity = velocity;
		m_Spin = new Vector3(120f, 80f, 60f);
		m_Life = 1.6f;
		var collider = GetComponent<Collider>();
		if (collider != null)
		{
			Destroy(collider);
		}
	}

	/// <summary>
	/// 매 프레임 흘러가며 축소·소멸한다.
	/// </summary>
	private void Update()
	{
		m_Life -= Time.deltaTime;
		if (m_Life <= 0f)
		{
			Destroy(gameObject);
			return;
		}

		transform.position += m_Velocity * Time.deltaTime;
		transform.Rotate(m_Spin * Time.deltaTime, Space.Self);
		transform.localScale = Vector3.Lerp(Vector3.zero, transform.localScale, 0.94f);
	}
}


/// <summary>
/// 추진체 분사 효과. 빈 공간 쪽으로 길게 뻗으며 깜빡인다.
/// </summary>
public class BattleThruster : MonoBehaviour
{
	private Vector3 m_BaseScale;
	private float m_Phase;

	/// <summary>
	/// 분사 효과를 시작한다.
	/// </summary>
	public void Begin(Vector3 baseScale, float phase)
	{
		m_BaseScale = baseScale;
		m_Phase = phase;
		var collider = GetComponent<Collider>();
		if (collider != null)
		{
			Destroy(collider);
		}
	}

	/// <summary>
	/// 매 프레임 길이를 출렁이게 한다.
	/// </summary>
	private void Update()
	{
		var pulse = 0.6f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 14f + m_Phase));
		transform.localScale = new Vector3(m_BaseScale.x, m_BaseScale.y, m_BaseScale.z * pulse);
	}
}

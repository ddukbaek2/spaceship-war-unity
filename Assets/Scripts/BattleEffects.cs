using UnityEngine;


/// <summary>
/// 전투 투사체. 발사 지점에서 대상 함선의 특정 모듈로 날아가 명중 시 피해를 준다.
/// </summary>
public class BattleProjectile : MonoBehaviour
{
	private BattleShip m_TargetShip;
	private Vector2Int m_TargetCoordinate;
	private float m_Damage;
	private float m_Speed;
	private float m_Life;

	/// <summary>
	/// 투사체를 발사한다.
	/// </summary>
	public void Launch(Vector3 start, BattleShip targetShip, Vector2Int targetCoordinate, float damage, float speed, Color color)
	{
		m_TargetShip = targetShip;
		m_TargetCoordinate = targetCoordinate;
		m_Damage = damage;
		m_Speed = speed;
		m_Life = 3f;

		transform.position = start;
		transform.localScale = new Vector3(0.18f, 0.18f, 0.55f);
		var renderer = GetComponent<Renderer>();
		var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
		material.SetColor("_BaseColor", color);
		material.EnableKeyword("_EMISSION");
		material.SetColor("_EmissionColor", color * 2f);
		material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
		renderer.material = material;

		var collider = GetComponent<Collider>();
		if (collider != null)
		{
			Destroy(collider);
		}
	}

	/// <summary>
	/// 매 프레임 대상으로 이동하고 명중을 처리한다.
	/// </summary>
	private void Update()
	{
		m_Life -= Time.deltaTime;
		if (m_Life <= 0f || m_TargetShip == null || !m_TargetShip.IsAlive)
		{
			Destroy(gameObject);
			return;
		}

		var targetPosition = m_TargetShip.GetTargetWorldPosition(m_TargetCoordinate);
		var toTarget = targetPosition - transform.position;
		var distance = toTarget.magnitude;
		if (distance <= 0.4f)
		{
			m_TargetShip.TakeDamage(m_TargetCoordinate, m_Damage);
			Destroy(gameObject);
			return;
		}

		var direction = toTarget / distance;
		transform.position += direction * m_Speed * Time.deltaTime;
		transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
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

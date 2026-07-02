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
	/// 무기 스타일별 투사체를 발사한다(0=총알, 1=레이저 빔, 2=에너지구).
	/// </summary>
	public void Launch(Vector3 start, Vector3 direction, BattleShip targetShip, float damage, int style, Color color)
	{
		m_TargetShip = targetShip;
		m_Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
		m_Damage = damage;
		m_Life = 2.5f;

		if (style == 2)
		{
			m_Speed = 12f;
		}
		else if (style == 1)
		{
			m_Speed = 26f;
		}
		else
		{
			m_Speed = 20f;
		}

		transform.position = start;
		transform.rotation = Quaternion.LookRotation(m_Direction, Vector3.up);

		BuildCore(style, color);
		BuildTrail(style, color);
		BuildSparks(style, color);
	}

	/// <summary>
	/// 스타일별 코어를 만든다(총알=작은 큐브, 빔=길쭉한 큐브, 에너지구=둥근 구).
	/// </summary>
	private void BuildCore(int style, Color color)
	{
		var primitive = style == 2 ? PrimitiveType.Sphere : PrimitiveType.Cube;
		var core = GameObject.CreatePrimitive(primitive);
		core.name = "Core";
		var collider = core.GetComponent<Collider>();
		if (collider != null)
		{
			Destroy(collider);
		}

		core.transform.SetParent(transform, false);
		core.transform.localPosition = Vector3.zero;

		Vector3 scale;
		float emissionMul;
		if (style == 1)
		{
			scale = new Vector3(0.11f, 0.11f, 2.4f);
			emissionMul = 6f;
		}
		else if (style == 2)
		{
			scale = new Vector3(0.55f, 0.55f, 0.55f);
			emissionMul = 4f;
		}
		else
		{
			scale = new Vector3(0.17f, 0.17f, 0.42f);
			emissionMul = 6f;
		}

		core.transform.localScale = scale;
		var emission = color * emissionMul;
		core.GetComponent<Renderer>().material = MaterialFactory.CreateLit(color, emission, 0f, 0.6f, false);
	}

	/// <summary>
	/// 스타일별 잔상 트레일을 만든다.
	/// </summary>
	private void BuildTrail(int style, Color color)
	{
		var trail = gameObject.AddComponent<TrailRenderer>();
		float width;
		float time;
		if (style == 1)
		{
			width = 0.18f;
			time = 0.16f;
		}
		else if (style == 2)
		{
			width = 0.55f;
			time = 0.12f;
		}
		else
		{
			width = 0.14f;
			time = 0.1f;
		}

		trail.time = time;
		trail.startWidth = width;
		trail.endWidth = 0f;
		trail.numCapVertices = 4;
		trail.minVertexDistance = 0.05f;
		trail.material = MaterialFactory.CreateLit(color, color * 4f, 0f, 0.5f, false);

		var gradient = new Gradient();
		var colorKeys = new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) };
		var alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) };
		gradient.SetKeys(colorKeys, alphaKeys);
		trail.colorGradient = gradient;
	}

	/// <summary>
	/// 궤적을 따라 흩어지는 파티클 스파크를 만든다(에너지구는 더 크고 짙게).
	/// </summary>
	private void BuildSparks(int style, Color color)
	{
		var sparkObject = new GameObject("Spark");
		sparkObject.transform.SetParent(transform, false);
		sparkObject.transform.localPosition = Vector3.zero;

		var particle = sparkObject.AddComponent<ParticleSystem>();
		var main = particle.main;
		main.startLifetime = 0.28f;
		main.startSpeed = 0.4f;
		main.startSize = style == 2 ? 0.55f : 0.2f;
		main.startColor = color;
		main.simulationSpace = ParticleSystemSimulationSpace.World;
		main.maxParticles = 90;

		var emission = particle.emission;
		emission.rateOverTime = style == 2 ? 100f : 60f;

		var shape = particle.shape;
		shape.shapeType = ParticleSystemShapeType.Sphere;
		shape.radius = style == 2 ? 0.3f : 0.1f;

		var particleRenderer = sparkObject.GetComponent<ParticleSystemRenderer>();
		particleRenderer.material = MaterialFactory.CreateLit(color, color * 5f, 0f, 0.5f, false);
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


/// <summary>
/// 개조 화면 자동 발사 미리보기용 간단 발사체. 지정 방향으로 잠시 날아가다 사라진다.
/// </summary>
public class PreviewShot : MonoBehaviour
{
	private Vector3 m_Direction;
	private float m_Life = 1.1f;
	private float m_Speed = 14f;

	/// <summary>
	/// 발사한다.
	/// </summary>
	public void Launch(Vector3 start, Vector3 direction, float size, Color color)
	{
		transform.position = start;
		transform.localScale = new Vector3(size, size, size * 3f);
		m_Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
		transform.rotation = Quaternion.LookRotation(m_Direction, Vector3.up);

		var collider = GetComponent<Collider>();
		if (collider != null)
		{
			Destroy(collider);
		}

		GetComponent<Renderer>().material = MaterialFactory.CreateLit(color, color * 5f, 0f, 0.6f, false);
	}

	/// <summary>
	/// 매 프레임 전진하고 수명이 끝나면 사라진다.
	/// </summary>
	private void Update()
	{
		m_Life -= Time.deltaTime;
		if (m_Life <= 0f)
		{
			Destroy(gameObject);
			return;
		}

		transform.position += m_Direction * m_Speed * Time.deltaTime;
	}
}

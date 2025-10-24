using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class ExplosionStunArea : MonoBehaviour
{
	[Header("Explosion Stun Settings")]
	public float stunRadius = 5f;
	public float stunDuration = 3f;
	public float lifetime = 0.2f;

	private CircleCollider2D circle;

	void Awake()
	{
		circle = GetComponent<CircleCollider2D>();
		circle.isTrigger = true;
		circle.radius = stunRadius;
	}

	void Start()
	{
		Destroy(gameObject, lifetime);
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		// Find any component on this object that implements IStunnable
		IStunnable stunnable = other.GetComponent<IStunnable>();
		if (stunnable != null)
		{
			stunnable.Stun(stunDuration);
		}
	}

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, stunRadius);
	}
}

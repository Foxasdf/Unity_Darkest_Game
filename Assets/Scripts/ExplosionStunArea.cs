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
		// Use OverlapCircle to detect what's in range at spawn
		CheckExplosionRadius();
		Destroy(gameObject, lifetime);
	}

	void CheckExplosionRadius()
	{
		// Get all colliders within the explosion radius
		Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, stunRadius);

		foreach (Collider2D hit in hitColliders)
		{
			// Skip self
			if (hit.gameObject == gameObject) continue;

			// Check if it's the player first - kill them
			if (hit.CompareTag("Player"))
			{
				PlayerDeathHandler pdh = hit.GetComponent<PlayerDeathHandler>();
				if (pdh != null)
				{
					pdh.TriggerDeath(DeathType.Mine);
				}
				continue; // Don't stun the player, they're dead
			}

			// Otherwise, check for stunnable enemies
			IStunnable stunnable = hit.GetComponent<IStunnable>();
			if (stunnable != null)
			{
				stunnable.Stun(stunDuration);
			}
		}
	}

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, stunRadius);
	}
}
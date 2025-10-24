using UnityEngine;

public class Mine : MonoBehaviour
{
	public GameObject explosionPrefab;  // Assign in Inspector
	public float delayBeforeDestroy = 0.2f;
	public GameObject stunAreaPrefab;


	private bool exploded = false;

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (exploded) return;

		// Example: check for player or enemy tags
		if (other.CompareTag("Player") || other.CompareTag("Enemy"))
		{
			Explode();
		}
	}

	void Explode()
	{
		exploded = true;

		// Spawn explosion animation
		Instantiate(explosionPrefab, transform.position, Quaternion.identity);

		// Spawn stun collider
		Instantiate(stunAreaPrefab, transform.position, Quaternion.identity);

		// Destroy the mine
		Destroy(gameObject, delayBeforeDestroy);
	}
}

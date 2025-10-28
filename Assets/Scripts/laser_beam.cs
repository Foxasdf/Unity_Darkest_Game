using UnityEngine;
using System.Collections;

public class laser_beam : MonoBehaviour
{
	BoxCollider2D laserCollider;
	SpriteRenderer laser_image;

	public float timeToStart = 2f;  // Delay before first activation
	public float laserOnTime = 1f;  // How long laser stays ON
	public float laserOffTime = 2f; // How long laser stays OFF

	void Start()
	{
		laserCollider = GetComponent<BoxCollider2D>();
		laser_image = GetComponent<SpriteRenderer>();

		if (laserCollider == null || laser_image == null)
		{
			Debug.LogError("Missing BoxCollider2D or SpriteRenderer on laser!");
			enabled = false; // disable script if setup is wrong
			return;
		}

		// Start the blinking cycle
		StartCoroutine(LaserCycle());
	}

	IEnumerator LaserCycle()
	{
		// Initial delay
		yield return new WaitForSeconds(timeToStart);

		while (true)
		{
			// Turn ON
			laserCollider.enabled = true;
			laser_image.enabled = true;
			yield return new WaitForSeconds(laserOnTime);

			// Turn OFF
			laserCollider.enabled = false;
			laser_image.enabled = false;
			yield return new WaitForSeconds(laserOffTime);
		}
	}
}
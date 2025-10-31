using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerDeathHandler : MonoBehaviour
{
	[Header("Death Settings")]
	public bool deathEnabled = true;
	[Range(0f, 1f)] public float jumpScareChance = 0.5f;
    
	[Header("UI References")]
	public GameObject blackBackgroundUI;
	public GameObject deathScreenUI;
	public GameObject jumpScareUI;
    
	[Header("Timing")]
	public float reloadDelay = 2.5f;
    
	[Header("Audio")]
	public AudioSource audioSource;
	public AudioClip deathSound;
	public AudioClip jumpScareSound;
    
	private bool isDead = false;

	// Called from collision with enemies
	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (!deathEnabled || isDead) return;
        
		if (collision.gameObject.CompareTag("Enemy"))
			TriggerDeath(DeathType.Enemy);
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (!deathEnabled || isDead) return;
        
		if (collision.gameObject.CompareTag("Enemy"))
			TriggerDeath(DeathType.Enemy);
		else if (collision.gameObject.CompareTag("KillZone")) // For falling/out of bounds
			TriggerDeath(DeathType.Fall);
	}

	// Public method that can be called from anywhere
	public void TriggerDeath(DeathType deathType)
	{
		if (isDead) return;
		StartCoroutine(HandleDeath(deathType));
	}

	private IEnumerator HandleDeath(DeathType deathType)
	{
		isDead = true;

		// Disable player control
		var movement = GetComponent<PlayerMovement2D>();
		if (movement != null) movement.enabled = false;

		var rb = GetComponent<Rigidbody2D>();
		if (rb != null) rb.linearVelocity = Vector2.zero;

		// Turn on black background to cover the game view
		if (blackBackgroundUI != null)
			blackBackgroundUI.SetActive(true);

		// Only show jumpscare if killed by enemy
		bool showJumpScare = (deathType == DeathType.Enemy) && (Random.value < jumpScareChance);

		if (showJumpScare && jumpScareUI != null)
		{
			// --- SHOW JUMPSCARE ONLY ---
			jumpScareUI.SetActive(true);
			var flicker = jumpScareUI.GetComponent<UIFlickerEffect>();
			if (flicker != null) flicker.StartFlicker();

			if (audioSource && jumpScareSound)
				audioSource.PlayOneShot(jumpScareSound);

			// Let the jumpscare fully play before reset
			yield return new WaitForSeconds(reloadDelay);
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
			yield break; // stop here so no death screen shows
		}

		// --- OTHERWISE, SHOW DEATH SCREEN ---
		if (audioSource && deathSound)
			audioSource.PlayOneShot(deathSound);

		yield return new WaitForSeconds(0.5f);

		if (deathScreenUI != null)
		{
			deathScreenUI.SetActive(true);
			var flicker = deathScreenUI.GetComponent<UIFlickerEffect>();
			if (flicker != null) flicker.StartFlicker();
		}

		yield return new WaitForSeconds(reloadDelay);
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}
}

// Enum to define different death types
public enum DeathType
{
	Enemy,
	Mine,
	Laser,
	Fall,
	Other
}
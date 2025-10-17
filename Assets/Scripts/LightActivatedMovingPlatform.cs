using UnityEngine;
using System.Collections.Generic;

public class LightActivatedMovingPlatform : MonoBehaviour
{
	[Header("Flashlight Reference")]
	[Tooltip("Assign the FlashlightController (usually on the player)")]
	public FlashlightController flashlight;

	[Header("Movement Settings")]
	public Transform pointA;
	public Transform pointB;
	public float moveSpeed = 3f;

	[Header("Platform Settings")]
	[SerializeField] private bool useSmoothing = true;
	[SerializeField] private float smoothTime = 0.1f;

	private Vector3 targetPosition;
	private Rigidbody2D rb;
	private Vector2 velocity;
	private Vector2 previousPosition;
	private HashSet<Rigidbody2D> playersOnPlatform = new HashSet<Rigidbody2D>();

	void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		rb.bodyType = RigidbodyType2D.Kinematic;
		targetPosition = pointB.position;
		previousPosition = rb.position;

		if (pointA == null || pointB == null)
		{
			Debug.LogError("LightActivatedMovingPlatform: pointA or pointB is not assigned!");
		}

		if (flashlight == null)
		{
			// Try to find flashlight on player
			GameObject player = GameObject.FindGameObjectWithTag("Player");
			if (player != null)
				flashlight = player.GetComponentInChildren<FlashlightController>();
			if (flashlight == null)
				Debug.LogError("LightActivatedMovingPlatform: No FlashlightController assigned!");
		}
	}

	void FixedUpdate()
	{
		previousPosition = rb.position;

		// Get the platform's own collider to exclude from raycast
		Collider2D platformCollider = GetComponent<Collider2D>();

		// Check if platform is lit, ignoring its own collider
		bool isLit = flashlight != null && flashlight.IsPositionLit(rb.position, platformCollider);

		if (isLit)
		{
			Vector2 newPosition = Vector2.MoveTowards(rb.position, targetPosition, moveSpeed * Time.fixedDeltaTime);
			rb.MovePosition(newPosition);
			velocity = (newPosition - previousPosition) / Time.fixedDeltaTime;

			if (Vector2.Distance(rb.position, targetPosition) < 0.01f)
			{
				targetPosition = (targetPosition == pointA.position) ? pointB.position : pointA.position;
			}
		}
		else
		{
			velocity = Vector2.zero;
		}

		// Apply velocity to players
		foreach (var playerRb in playersOnPlatform)
		{
			var playerMovement = playerRb.GetComponent<PlayerMovement2D>();
			if (playerMovement != null)
			{
				playerMovement.SetPlatformVelocity(velocity);
			}
		}
	}

	void OnCollisionEnter2D(Collision2D collision)
	{
		if (collision.gameObject.CompareTag("Player"))
		{
			Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
			PlayerMovement2D playerMovement = collision.gameObject.GetComponent<PlayerMovement2D>();

			if (playerRb != null && playerMovement != null && IsPlayerOnTop(collision))
			{
				playersOnPlatform.Add(playerRb);
				playerMovement.SetPlatformVelocity(velocity);
			}
		}
	}

	void OnCollisionStay2D(Collision2D collision)
	{
		if (collision.gameObject.CompareTag("Player"))
		{
			Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
			PlayerMovement2D playerMovement = collision.gameObject.GetComponent<PlayerMovement2D>();

			if (playerRb != null && playerMovement != null)
			{
				if (IsPlayerOnTop(collision))
				{
					if (!playersOnPlatform.Contains(playerRb))
					{
						playersOnPlatform.Add(playerRb);
					}
					playerMovement.SetPlatformVelocity(velocity);
				}
				else if (playersOnPlatform.Contains(playerRb))
				{
					playersOnPlatform.Remove(playerRb);
					playerMovement.ClearPlatformVelocity();
				}
			}
		}
	}

	void OnCollisionExit2D(Collision2D collision)
	{
		if (collision.gameObject.CompareTag("Player"))
		{
			Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
			PlayerMovement2D playerMovement = collision.gameObject.GetComponent<PlayerMovement2D>();

			if (playerRb != null && playerMovement != null)
			{
				playersOnPlatform.Remove(playerRb);
				playerMovement.ClearPlatformVelocity();
			}
		}
	}

	bool IsPlayerOnTop(Collision2D collision)
	{
		foreach (ContactPoint2D contact in collision.contacts)
		{
			if (Vector2.Dot(contact.normal, Vector2.down) > 0.7f)
			{
				return true;
			}
		}
		return false;
	}

	public Vector2 GetPlatformVelocity()
	{
		return velocity;
	}
}
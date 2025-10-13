using UnityEngine;
using System.Collections.Generic;

public class MovingPlatform2D : MonoBehaviour
{
	[Header("Movement Settings")]
	public Transform pointA;
	public Transform pointB;
	public float moveSpeed = 3f;

	[Header("Platform Settings")]
	[SerializeField] private bool useSmoothing = true;
	[SerializeField] private float smoothTime = 0.1f;

	private Vector3 nextPos;
	private Rigidbody2D rb;
	private Vector2 velocity;
	private Vector2 previousPosition;
	private HashSet<Rigidbody2D> playersOnPlatform = new HashSet<Rigidbody2D>();

	void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		rb.bodyType = RigidbodyType2D.Kinematic;
		nextPos = pointB.position;
		previousPosition = rb.position;

		// Validate points
		if (pointA == null || pointB == null)
		{
			Debug.LogError("MovingPlatform2D: pointA or pointB is not assigned!");
		}
	}

	void FixedUpdate()
	{
		// Store previous position to calculate velocity
		previousPosition = rb.position;

		// Move platform between points
		Vector2 newPosition = Vector2.MoveTowards(rb.position, nextPos, moveSpeed * Time.fixedDeltaTime);
		rb.MovePosition(newPosition);

		// Calculate platform velocity
		velocity = (newPosition - previousPosition) / Time.fixedDeltaTime;

		// Switch direction at endpoints
		if (Vector2.Distance(rb.position, nextPos) < 0.01f)
		{
			nextPos = (nextPos == pointA.position) ? pointB.position : pointA.position;
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
			// Check if the contact normal points upward (player is on top)
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
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class FlyingEnemy : MonoBehaviour
{
	[Header("Idle Settings")]
	[SerializeField] private float idleHoverSpeed = 1f;
	[SerializeField] private float idleHoverRange = 2f;
	[SerializeField] private bool hoverVertically = true;
	
	[Header("Chase Settings")]
	[SerializeField] private float chaseSpeed = 5f;
	[SerializeField] private float chaseAcceleration = 8f;
	[SerializeField] private float stoppingDistance = 0.5f; // How close to get to player
	[SerializeField] private float targetHeightOffset = 1f; // Aim higher than player's feet
	
	[Header("Player Detection")]
	[SerializeField] private float knockbackForce = 12f;
	
	private Rigidbody2D rb;
	private CircleCollider2D col;
	private Transform playerTransform;
	private FlashlightController flashlight;
	
	// States
	private bool isChasing = false;
	private bool hasBeenSpotted = false;
	
	// Idle hover
	private Vector3 startPosition;
	private float hoverOffset;
	
	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<CircleCollider2D>();
	}
	
	private void Start()
	{
		// Configure Rigidbody2D for flying
		rb.bodyType = RigidbodyType2D.Dynamic;
		rb.gravityScale = 0f; // No gravity - it flies!
		rb.linearDamping = 2f; // Some air resistance for smooth movement
		rb.freezeRotation = true;
		rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
		rb.interpolation = RigidbodyInterpolation2D.Interpolate;
		
		// Store starting position for idle hovering
		startPosition = transform.position;
		
		// Find player and flashlight
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		if (player != null)
		{
			playerTransform = player.transform;
			flashlight = player.GetComponentInChildren<FlashlightController>();
		}
		else
		{
			Debug.LogError("Flying enemy couldn't find player with tag 'Player'!");
		}
	}
	
	private void FixedUpdate()
	{
		// Check if flashlight is pointing at us
		if (!hasBeenSpotted && flashlight != null)
		{
			if (flashlight.IsPointingAt(transform))
			{
				hasBeenSpotted = true;
				isChasing = true;
			}
		}
		
		// Move based on state
		if (isChasing)
		{
			ChasePlayer();
		}
		else
		{
			IdleHover();
		}
	}
	
	private void IdleHover()
	{
		// Simple sine wave hovering
		hoverOffset += Time.fixedDeltaTime * idleHoverSpeed;
		
		Vector2 targetPosition;
		if (hoverVertically)
		{
			// Hover up and down
			float yOffset = Mathf.Sin(hoverOffset) * idleHoverRange;
			targetPosition = new Vector2(startPosition.x, startPosition.y + yOffset);
		}
		else
		{
			// Hover left and right
			float xOffset = Mathf.Sin(hoverOffset) * idleHoverRange;
			targetPosition = new Vector2(startPosition.x + xOffset, startPosition.y);
		}
		
		// Smoothly move toward hover position
		Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
		float distance = Vector2.Distance(transform.position, targetPosition);
		
		if (distance > 0.1f)
		{
			rb.AddForce(direction * chaseAcceleration * 0.5f);
		}
		
		// Limit speed
		if (rb.linearVelocity.magnitude > idleHoverSpeed)
		{
			rb.linearVelocity = rb.linearVelocity.normalized * idleHoverSpeed;
		}
	}
	
	private void ChasePlayer()
	{
		if (playerTransform == null)
		{
			isChasing = false;
			return;
		}
		
		// Calculate target position (player position + height offset)
		Vector2 targetPosition = new Vector2(playerTransform.position.x, 
			playerTransform.position.y + targetHeightOffset);
		
		// Calculate direction to target
		Vector2 directionToPlayer = (targetPosition - (Vector2)transform.position).normalized;
		float distanceToPlayer = Vector2.Distance(transform.position, targetPosition);
		
		// Only move if we're not too close
		if (distanceToPlayer > stoppingDistance)
		{
			// Apply force toward player
			rb.AddForce(directionToPlayer * chaseAcceleration);
			
			// Limit max speed
			if (rb.linearVelocity.magnitude > chaseSpeed)
			{
				rb.linearVelocity = rb.linearVelocity.normalized * chaseSpeed;
			}
		}
		else
		{
			// Slow down when close
			rb.linearVelocity *= 0.9f;
		}
	}
	
	private void OnCollisionEnter2D(Collision2D collision)
	{
		// Check if we hit the player
		if (collision.gameObject.CompareTag("Player"))
		{
			// Try to get player's Rigidbody2D for knockback
			Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
			
			if (playerRb != null)
			{
				// Calculate knockback direction (away from enemy)
				Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
				
				// Apply knockback
				playerRb.linearVelocity = new Vector2(knockbackDir.x * knockbackForce, knockbackForce * 0.5f);
			}
		}
	}
	
	private void OnDrawGizmosSelected()
	{
		// Draw idle hover range
		if (!Application.isPlaying)
		{
			Gizmos.color = Color.cyan;
			if (hoverVertically)
			{
				Gizmos.DrawLine(transform.position + Vector3.down * idleHoverRange, 
					transform.position + Vector3.up * idleHoverRange);
			}
			else
			{
				Gizmos.DrawLine(transform.position + Vector3.left * idleHoverRange, 
					transform.position + Vector3.right * idleHoverRange);
			}
		}
		else
		{
			// Draw chase state
			Gizmos.color = isChasing ? Color.red : Color.green;
			Gizmos.DrawWireSphere(transform.position, 0.5f);
			
			// Draw line to start position when idle
			if (!isChasing)
			{
				Gizmos.color = Color.cyan;
				Gizmos.DrawLine(transform.position, startPosition);
			}
		}
	}
}
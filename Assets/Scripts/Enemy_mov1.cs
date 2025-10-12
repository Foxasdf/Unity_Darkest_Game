using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class SimpleEnemyPatrol : MonoBehaviour
{
	[Header("Patrol Settings")]
	[SerializeField] private Transform leftPoint;
	[SerializeField] private Transform rightPoint;
	[SerializeField] private float moveSpeed = 3f;
	
	[Header("Chase Settings")]
	[SerializeField] private float chaseSpeed = 6f;
	[SerializeField] private float detectionRangeX = 15f; // Horizontal detection range
	[SerializeField] private LayerMask playerLayer;
	[SerializeField] private bool showDetectionRange = true;
    
	[Header("Player Detection")]
	[SerializeField] private float knockbackForce = 10f;
    
	private Rigidbody2D rb;
	private CapsuleCollider2D col;
	private bool movingRight = true;
	
	// Store world positions at start
	private float leftBoundary;
	private float rightBoundary;
	
	// Chase system
	private bool isChasing = false;
	private Transform playerTransform;
	private FlashlightController flashlight;
    
	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<CapsuleCollider2D>();
	}
    
	private void Start()
	{
		// Configure Rigidbody2D
		rb.bodyType = RigidbodyType2D.Dynamic; // Changed to Dynamic for gravity
		rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
		rb.freezeRotation = true;
		rb.gravityScale = 3f; // Match player's gravity
		rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Only freeze rotation
		rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smooth movement
        
		// Validate patrol points
		if (leftPoint == null || rightPoint == null)
		{
			Debug.LogError($"Patrol points not assigned on {gameObject.name}!");
			enabled = false;
			return;
		}
		
		// Store the world positions at start so child objects don't affect patrol
		leftBoundary = leftPoint.position.x;
		rightBoundary = rightPoint.position.x;
		
		// Make sure left is actually left of right
		if (leftBoundary > rightBoundary)
		{
			float temp = leftBoundary;
			leftBoundary = rightBoundary;
			rightBoundary = temp;
			Debug.LogWarning($"Left and Right points were swapped on {gameObject.name}");
		}
		
		// Find player
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		if (player != null)
		{
			playerTransform = player.transform;
			flashlight = player.GetComponentInChildren<FlashlightController>();
		}
	}
    
	private void FixedUpdate()
	{
		// Check if we should start chasing
		if (!isChasing)
		{
			CheckForFlashlight();
		}
		
		// Move based on state
		if (isChasing)
		{
			ChasePlayer();
		}
		else
		{
			Patrol();
		}
	}
	
	private void CheckForFlashlight()
	{
		// Don't chase if no player or flashlight found
		if (playerTransform == null || flashlight == null)
			return;
		
		// Check if flashlight is actually on (not just key pressed)
		if (!flashlight.IsFlashlightOn())
			return;
		
		// Calculate horizontal distance to player
		float horizontalDistance = Mathf.Abs(playerTransform.position.x - transform.position.x);
		
		// If player is within horizontal detection range, start chasing
		if (horizontalDistance <= detectionRangeX)
		{
			isChasing = true;
		}
	}
    
	private void Patrol()
	{
		// Calculate movement direction
		float direction = movingRight ? 1f : -1f;
        
		// Calculate target velocity
		float targetVelocityX = direction * moveSpeed;
		
		// Smoothly change velocity (keeps Y velocity for gravity)
		rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);
        
		// Check if we've reached patrol boundaries (using stored world positions)
		if (movingRight && transform.position.x >= rightBoundary)
		{
			movingRight = false;
		}
		else if (!movingRight && transform.position.x <= leftBoundary)
		{
			movingRight = true;
		}
	}
	
	private void ChasePlayer()
	{
		if (playerTransform == null)
		{
			// If player is lost, go back to patrol
			isChasing = false;
			return;
		}
		
		// Calculate direction to player (only on X axis)
		float directionToPlayer = Mathf.Sign(playerTransform.position.x - transform.position.x);
		
		// Calculate target velocity
		float targetVelocityX = directionToPlayer * chaseSpeed;
		
		// Smoothly change velocity (keeps Y velocity for gravity)
		rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);
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
    
	// Visualize patrol points and detection range in editor
	private void OnDrawGizmosSelected()
	{
		// Draw patrol path
		if (leftPoint != null && rightPoint != null)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(leftPoint.position, rightPoint.position);
            
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(leftPoint.position, 0.3f);
			Gizmos.DrawWireSphere(rightPoint.position, 0.3f);
			
			// Show direction
			Gizmos.color = Color.cyan;
			Vector3 center = (leftPoint.position + rightPoint.position) / 2f;
			Gizmos.DrawWireSphere(center, 0.2f);
		}
		
		// Draw detection range
		if (showDetectionRange)
		{
			Gizmos.color = isChasing ? Color.red : Color.blue;
			Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
			
			// Draw horizontal detection range
			Vector3 leftRange = transform.position + Vector3.left * detectionRangeX;
			Vector3 rightRange = transform.position + Vector3.right * detectionRangeX;
			
			// Draw vertical lines to show range
			Gizmos.DrawLine(leftRange + Vector3.up * 5f, leftRange + Vector3.down * 5f);
			Gizmos.DrawLine(rightRange + Vector3.up * 5f, rightRange + Vector3.down * 5f);
			
			// Draw horizontal line
			Gizmos.DrawLine(leftRange, rightRange);
		}
	}
}
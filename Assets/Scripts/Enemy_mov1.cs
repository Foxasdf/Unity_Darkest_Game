using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class SimpleEnemyPatrol : MonoBehaviour
{
	[Header("Patrol Settings")]
	[SerializeField] private Transform leftPoint;
	[SerializeField] private Transform rightPoint;
	[SerializeField] private float moveSpeed = 3f;
    
	[Header("Player Detection")]
	[SerializeField] private float knockbackForce = 10f;
    
	private Rigidbody2D rb;
	private CapsuleCollider2D col;
	private bool movingRight = true;
	
	// Store world positions at start
	private float leftBoundary;
	private float rightBoundary;
    
	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<CapsuleCollider2D>();
	}
    
	private void Start()
	{
		// Configure Rigidbody2D
		rb.bodyType = RigidbodyType2D.Kinematic;
		rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
		rb.freezeRotation = true;
        
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
	}
    
	private void FixedUpdate()
	{
		Patrol();
	}
    
	private void Patrol()
	{
		// Calculate movement direction
		float direction = movingRight ? 1f : -1f;
        
		// Move the enemy
		rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        
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
    
	// Visualize patrol points in editor
	private void OnDrawGizmosSelected()
	{
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
	}
}
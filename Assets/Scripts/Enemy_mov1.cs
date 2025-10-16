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
    
	// NEW: Reference to Animator
	[Header("Animation")]
	[SerializeField] private Animator animator;

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
    
	// Animation parameter name (optional: use string const for safety)
	private static readonly string MOVE_ANIM_PARAM = "move";

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<CapsuleCollider2D>();

		// NEW: Cache Animator (could be null if not assigned)
		animator = GetComponent<Animator>(); // Often on same object as SpriteRenderer
	}
    
	private void Start()
	{
		// Configure Rigidbody2D
		rb.bodyType = RigidbodyType2D.Dynamic;
		rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
		rb.freezeRotation = true;
		rb.gravityScale = 3f;
		rb.constraints = RigidbodyConstraints2D.FreezeRotation;
		rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        
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

		// NEW: Update animation based on movement
		UpdateAnimation();
	}
	
	private void CheckForFlashlight()
	{
		if (playerTransform == null || flashlight == null)
			return;
		
		if (!flashlight.IsFlashlightOn())
			return;
		
		float horizontalDistance = Mathf.Abs(playerTransform.position.x - transform.position.x);
		
		if (horizontalDistance <= detectionRangeX)
		{
			isChasing = true;
		}
	}
    
	private void Patrol()
	{
		float direction = movingRight ? 1f : -1f;
		float targetVelocityX = direction * moveSpeed;
		
		rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);
        
		if (movingRight && transform.position.x >= rightBoundary)
		{
			movingRight = false;
			FlipSprite(); // Optional: flip sprite when turning
		}
		else if (!movingRight && transform.position.x <= leftBoundary)
		{
			movingRight = true;
			FlipSprite(); // Optional: flip sprite when turning
		}
	}
	
	private void ChasePlayer()
	{
		if (playerTransform == null)
		{
			isChasing = false;
			return;
		}
		
		float directionToPlayer = Mathf.Sign(playerTransform.position.x - transform.position.x);
		float targetVelocityX = directionToPlayer * chaseSpeed;
		
		rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);
	}

	// NEW: Update animation parameter
	private void UpdateAnimation()
	{
		// If velocity is non-zero on X, then enemy is moving
		bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;

		// Set the 'move' parameter in Animator
		if (animator != null)
		{
			animator.SetBool(MOVE_ANIM_PARAM, isMoving);
		}
	}

	// Optional: Flip sprite when changing direction
	private void FlipSprite()
	{
		if (animator == null) return;

		// Scale x to -1 to flip, back to 1 to un-flip
		Vector3 scale = transform.localScale;
		scale.x *= -1; // Reverse X scale
		transform.localScale = scale;
	}
    
	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (collision.gameObject.CompareTag("Player"))
		{
			Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            
			if (playerRb != null)
			{
				Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
				playerRb.linearVelocity = new Vector2(knockbackDir.x * knockbackForce, knockbackForce * 0.5f);
			}
		}
	}
    
	private void OnDrawGizmosSelected()
	{
		if (leftPoint != null && rightPoint != null)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(leftPoint.position, rightPoint.position);
            
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(leftPoint.position, 0.3f);
			Gizmos.DrawWireSphere(rightPoint.position, 0.3f);
			
			Gizmos.color = Color.cyan;
			Vector3 center = (leftPoint.position + rightPoint.position) / 2f;
			Gizmos.DrawWireSphere(center, 0.2f);
		}
		
		if (showDetectionRange)
		{
			Gizmos.color = isChasing ? Color.red : Color.blue;
			Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
			
			Vector3 leftRange = transform.position + Vector3.left * detectionRangeX;
			Vector3 rightRange = transform.position + Vector3.right * detectionRangeX;
			
			Gizmos.DrawLine(leftRange + Vector3.up * 5f, leftRange + Vector3.down * 5f);
			Gizmos.DrawLine(rightRange + Vector3.up * 5f, rightRange + Vector3.down * 5f);
			Gizmos.DrawLine(leftRange, rightRange);
		}
	}
}
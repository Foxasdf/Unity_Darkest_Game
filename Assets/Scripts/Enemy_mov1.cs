using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class SimpleEnemyPatrol : MonoBehaviour, IStunnable
{
	[Header("Patrol Settings")]
	[SerializeField] private Transform leftPoint;
	[SerializeField] private Transform rightPoint;
	[SerializeField] private float moveSpeed = 3f;
	
	[Header("Chase Settings")]
	[SerializeField] private float chaseSpeed = 6f;
	[SerializeField] private float detectionRangeX = 15f;
	[SerializeField] private LayerMask playerLayer;
	[SerializeField] private bool showDetectionRange = true;
    
	[Header("Player Detection")]
	[SerializeField] private float knockbackForce = 10f;
    
	[Header("Animation")]
	[SerializeField] private Animator animator;

	private Rigidbody2D rb;
	private CapsuleCollider2D col;
	private bool movingRight = true;
	
	private float leftBoundary;
	private float rightBoundary;
	
	private bool isChasing = false;
	private Transform playerTransform;
	private FlashlightController flashlight;
    
	private static readonly string MOVE_ANIM_PARAM = "move";
	private bool isStunned = false;
	private float stunTimer = 0f;

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<CapsuleCollider2D>();
		animator = GetComponent<Animator>();
	}
    
	private void Start()
	{
		rb.bodyType = RigidbodyType2D.Dynamic;
		rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
		rb.freezeRotation = true;
		rb.gravityScale = 3f;
		rb.constraints = RigidbodyConstraints2D.FreezeRotation;
		rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        
		if (leftPoint == null || rightPoint == null)
		{
			Debug.LogError($"Patrol points not assigned on {gameObject.name}!");
			enabled = false;
			return;
		}
		
		leftBoundary = leftPoint.position.x;
		rightBoundary = rightPoint.position.x;
		
		if (leftBoundary > rightBoundary)
		{
			float temp = leftBoundary;
			leftBoundary = rightBoundary;
			rightBoundary = temp;
			Debug.LogWarning($"Left and Right points were swapped on {gameObject.name}");
		}
		
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		if (player != null)
		{
			playerTransform = player.transform;
			flashlight = player.GetComponentInChildren<FlashlightController>();
		}
	}
    
	private void FixedUpdate()
	{
		if (isStunned)
		{
			stunTimer -= Time.fixedDeltaTime;
			if (stunTimer <= 0f)
			{
				isStunned = false;
			}
			return;
		}

		if (!isChasing)
			CheckForFlashlight();

		if (isChasing)
			ChasePlayer();
		else
			Patrol();

		UpdateAnimation();
	}
	
	public void Stun(float duration)
	{
		if (isStunned) return;
		isStunned = true;
		stunTimer = duration;

		rb.linearVelocity = Vector2.zero;
		isChasing = false;

		if (animator != null)
			animator.SetBool("move", false);
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
			FlipSprite();
		}
		else if (!movingRight && transform.position.x <= leftBoundary)
		{
			movingRight = true;
			FlipSprite();
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

	private void UpdateAnimation()
	{
		bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;

		if (animator != null)
		{
			animator.SetBool(MOVE_ANIM_PARAM, isMoving);
		}
	}

	private void FlipSprite()
	{
		if (animator == null) return;

		Vector3 scale = transform.localScale;
		scale.x *= -1;
		transform.localScale = scale;
	}
    
	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (collision.gameObject.CompareTag("Player"))
		{
			// Trigger death through PlayerDeathHandler
			PlayerDeathHandler deathHandler = collision.gameObject.GetComponent<PlayerDeathHandler>();
			if (deathHandler != null)
			{
				deathHandler.TriggerDeath(DeathType.Enemy);
			}
			
			// Apply knockback
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
	
	private bool IsVisibleToPlayer()
	{
		if (playerTransform == null) return false;

		Vector2 direction = transform.position - playerTransform.position;
		float distance = direction.magnitude;

		RaycastHit2D hit = Physics2D.Raycast(
			playerTransform.position,
			direction.normalized,
			distance,
			~LayerMask.GetMask("Player", "Enemy")
		);

		if (hit.collider != null)
		{
			return false;
		}

		Camera cam = Camera.main;
		if (cam != null)
		{
			Vector3 viewportPos = cam.WorldToViewportPoint(transform.position);
			bool onScreen = 
				viewportPos.x >= 0 && viewportPos.x <= 1 &&
				viewportPos.y >= 0 && viewportPos.y <= 1 &&
				viewportPos.z > 0;
			if (!onScreen)
				return false;
		}

		return true;
	}
}
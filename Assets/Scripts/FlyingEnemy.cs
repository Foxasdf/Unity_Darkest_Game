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
	[SerializeField] private float stoppingDistance = 0.5f;
	[SerializeField] private float targetHeightOffset = 1f;
	
	[Header("Player Detection")]
	[SerializeField] private float knockbackForce = 12f;

	// NEW: Animation
	[Header("Animation")]
	[SerializeField] private Animator animator;
	[SerializeField] private string animationParamName = "isMoving"; // Use "Speed" or "Move" if using floats

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

		// NEW: Cache animator
		animator = GetComponent<Animator>(); // Assumes Animator is on same GameObject
	}
	
	private void Start()
	{
		// Configure Rigidbody2D for flying
		rb.bodyType = RigidbodyType2D.Dynamic;
		rb.gravityScale = 0f; // No gravity - it flies!
		rb.linearDamping = 2f; // Some air resistance
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

		// NEW: Update animation after physics update
		UpdateAnimation();
	}
	
	private void IdleHover()
	{
		hoverOffset += Time.fixedDeltaTime * idleHoverSpeed;
		
		Vector2 targetPosition;
		if (hoverVertically)
		{
			float yOffset = Mathf.Sin(hoverOffset) * idleHoverRange;
			targetPosition = new Vector2(startPosition.x, startPosition.y + yOffset);
		}
		else
		{
			float xOffset = Mathf.Sin(hoverOffset) * idleHoverRange;
			targetPosition = new Vector2(startPosition.x + xOffset, startPosition.y);
		}
		
		Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
		float distance = Vector2.Distance(transform.position, targetPosition);
		
		if (distance > 0.1f)
		{
			rb.AddForce(direction * chaseAcceleration * 0.5f);
		}
		
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
		
		Vector2 targetPosition = new Vector2(
			playerTransform.position.x, 
			playerTransform.position.y + targetHeightOffset);
		
		Vector2 directionToPlayer = (targetPosition - (Vector2)transform.position).normalized;
		float distanceToPlayer = Vector2.Distance(transform.position, targetPosition);
		
		if (distanceToPlayer > stoppingDistance)
		{
			rb.AddForce(directionToPlayer * chaseAcceleration);
			
			if (rb.linearVelocity.magnitude > chaseSpeed)
			{
				rb.linearVelocity = rb.linearVelocity.normalized * chaseSpeed;
			}
		}
		else
		{
			rb.linearVelocity *= 0.9f;
		}
	}

	// NEW: Control animation based on motion/state
	private void UpdateAnimation()
	{
		// Option 1: Use a boolean — play animation only when chasing
		bool shouldPlayAnimation = isChasing;

		// OR Option 2: Use speed — more realistic (recommended)
		float currentSpeed = rb.linearVelocity.magnitude;
        
		if (animator != null)
		{
			// If you're using a FLOAT parameter like "Speed"
			if (animator.parameters.Length > 0 && animator.GetParameter(0).type == AnimatorControllerParameterType.Float)
			{
				animator.SetFloat("Speed", currentSpeed); // Adjust in Animator to scale motion
			}
			// If using BOOL like "isMoving"
			else
			{
				animator.SetBool(animationParamName, shouldPlayAnimation);
			}
		}
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
			Gizmos.color = isChasing ? Color.red : Color.green;
			Gizmos.DrawWireSphere(transform.position, 0.5f);
			
			if (!isChasing)
			{
				Gizmos.color = Color.cyan;
				Gizmos.DrawLine(transform.position, startPosition);
			}
		}
	}
}
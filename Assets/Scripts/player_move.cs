using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerMovement2D : MonoBehaviour
{
	[Header("Movement Settings")]
	[SerializeField] private float moveSpeed = 8f;
	[SerializeField] private float acceleration = 10f;
	[SerializeField] private float deceleration = 10f;
	[SerializeField] private float velPower = 0.9f; // Makes movement feel more responsive

	[Header("Jump Settings")]
	[SerializeField] private float jumpForce = 15f;
	[SerializeField] private float jumpCutMultiplier = 0.5f; // For variable jump height
	[SerializeField] private float coyoteTime = 0.2f; // Grace period for jumping after leaving platform
	[SerializeField] private float jumpBufferTime = 0.2f; // Jump input buffer

	[Header("Gravity Settings")]
	[SerializeField] private float gravityScale = 3f;
	[SerializeField] private float fallGravityMultiplier = 1.5f; // Faster falling
	[SerializeField] private float maxFallSpeed = 20f;

	[Header("Ground Check")]
	[SerializeField] private Transform groundCheckPoint;
	[SerializeField] private Vector2 groundCheckSize = new Vector2(0.49f, 0.03f);
	[SerializeField] private LayerMask groundLayer;

	[Header("Air Movement")]
	[SerializeField] private float airMultiplier = 0.8f; // Slightly reduced air control
	[SerializeField] private int maxAirJumps = 1; // Double jump capability

	// Components
	private Rigidbody2D rb;
	private CapsuleCollider2D col;

	// Movement variables
	private float horizontalInput;
	private bool isGrounded;
	private bool wasGrounded;
	private int airJumpsRemaining;

	// Jump variables
	private float coyoteTimeCounter;
	private float jumpBufferCounter;
	private bool isJumping;
	private bool jumpInputReleased;

	// Platform velocity (for moving platforms)
	private Vector2 platformVelocity = Vector2.zero;

	// Optional: For animations
	public bool IsFacingRight { get; private set; } = true;
	public bool IsMoving => Mathf.Abs(rb.linearVelocity.x) > 0.01f;
	public bool IsGrounded => isGrounded;
	public float VerticalVelocity => rb.linearVelocity.y;

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<CapsuleCollider2D>();
	}

	private void Start()
	{
		rb.gravityScale = gravityScale;
		rb.freezeRotation = true;
		rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

		// Create ground check point if not assigned
		if (groundCheckPoint == null)
		{
			GameObject groundCheck = new GameObject("GroundCheck");
			groundCheck.transform.parent = transform;
			groundCheck.transform.localPosition = new Vector3(0, -col.bounds.extents.y, 0);
			groundCheckPoint = groundCheck.transform;
		}
	}

	private void Update()
	{
		// Get input
		horizontalInput = Input.GetAxisRaw("Horizontal");

		// Ground check
		CheckGrounded();

		// Handle coyote time
		if (isGrounded)
		{
			coyoteTimeCounter = coyoteTime;
			airJumpsRemaining = maxAirJumps;
		}
		else
		{
			coyoteTimeCounter -= Time.deltaTime;
		}

		// Handle jump buffer
		if (Input.GetButtonDown("Jump"))
		{
			jumpBufferCounter = jumpBufferTime;
		}
		else
		{
			jumpBufferCounter -= Time.deltaTime;
		}

		// Handle jump
		HandleJump();

		// Handle sprite flipping
		HandleSpriteFlip();

		// Track jump input release for variable jump height
		if (Input.GetButtonUp("Jump"))
		{
			jumpInputReleased = true;
		}
	}

	private void FixedUpdate()
	{
		// Handle horizontal movement
		HandleMovement();

		// Apply gravity modifications
		ApplyGravityModifiers();

		// Clamp fall speed
		if (rb.linearVelocity.y < -maxFallSpeed)
		{
			rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
		}
	}

	private void HandleMovement()
	{
		// Base target speed from input
		float targetSpeed = horizontalInput * moveSpeed;

		// Add platform's horizontal velocity to maintain sync
		targetSpeed += platformVelocity.x;

		// Calculate speed difference
		float speedDiff = targetSpeed - rb.linearVelocity.x;

		// Determine acceleration rate
		float accelRate = (Mathf.Abs(horizontalInput) > 0.01f) ? acceleration : deceleration;

		// Apply air multiplier if in air
		if (!isGrounded)
		{
			accelRate *= airMultiplier;
		}

		// Calculate movement with velPower for better feel
		float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, velPower) * Mathf.Sign(speedDiff);

		// Apply force
		rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
	}

	private void HandleJump()
	{
		// Check if we should jump (with buffer and coyote time)
		bool canJump = (coyoteTimeCounter > 0f || airJumpsRemaining > 0) && jumpBufferCounter > 0f;

		if (canJump)
		{
			Jump();
		}

		// Variable jump height - reduce velocity when jump button is released
		if (jumpInputReleased && rb.linearVelocity.y > 0 && isJumping)
		{
			rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
			isJumping = false;
		}
	}

	private void Jump()
	{
		// If we're using an air jump
		if (coyoteTimeCounter <= 0f && !isGrounded)
		{
			airJumpsRemaining--;
		}

		// Apply jump velocity
		rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

		// Reset counters
		jumpBufferCounter = 0f;
		coyoteTimeCounter = 0f;
		isJumping = true;
		jumpInputReleased = false;
	}

	private void ApplyGravityModifiers()
	{
		// Apply different gravity when falling for better game feel
		if (rb.linearVelocity.y < 0)
		{
			rb.gravityScale = gravityScale * fallGravityMultiplier;
			isJumping = false;
		}
		else if (rb.linearVelocity.y > 0 && jumpInputReleased)
		{
			rb.gravityScale = gravityScale * fallGravityMultiplier;
		}
		else
		{
			rb.gravityScale = gravityScale;
		}
	}

	private void CheckGrounded()
	{
		wasGrounded = isGrounded;

		// Box cast for ground detection
		isGrounded = Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0f, groundLayer);

		// Landing
		if (!wasGrounded && isGrounded)
		{
			OnLand();
		}
	}

	private void OnLand()
	{
		isJumping = false;
		jumpInputReleased = false;
	}

	private void HandleSpriteFlip()
	{
		if (horizontalInput > 0 && !IsFacingRight)
		{
			Flip();
		}
		else if (horizontalInput < 0 && IsFacingRight)
		{
			Flip();
		}
	}

	private void Flip()
	{
		IsFacingRight = !IsFacingRight;
		Vector3 scale = transform.localScale;
		scale.x *= -1;
		transform.localScale = scale;
	}

	// Called by moving platform when player is on it
	public void SetPlatformVelocity(Vector2 vel)
	{
		platformVelocity = vel;
	}

	// Called by moving platform when player leaves it
	public void ClearPlatformVelocity()
	{
		platformVelocity = Vector2.zero;
	}

	// Optional: For one-way platforms
	public void DisableCollisionForPlatform(float duration = 0.5f)
	{
		StartCoroutine(DisableCollision(duration));
	}

	private System.Collections.IEnumerator DisableCollision(float duration)
	{
		int platformLayer = LayerMask.NameToLayer("OneWayPlatform");
		if (platformLayer == -1) yield break; // Layer doesn't exist

		Physics2D.IgnoreLayerCollision(gameObject.layer, platformLayer, true);
		yield return new WaitForSeconds(duration);
		Physics2D.IgnoreLayerCollision(gameObject.layer, platformLayer, false);
	}

	// Debug visualization
	private void OnDrawGizmosSelected()
	{
		if (groundCheckPoint != null)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
		}
	}
}
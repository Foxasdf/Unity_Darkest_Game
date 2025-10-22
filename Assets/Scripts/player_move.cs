using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerMovement2D : MonoBehaviour
{
	[Header("Movement Settings")]
	[SerializeField] private float moveSpeed = 8f;
	[SerializeField] private float acceleration = 10f;
	[SerializeField] private float deceleration = 10f;
	[SerializeField] private float velPower = 0.9f;

	[Header("Jump Settings")]
	[SerializeField] private float jumpForce = 15f;
	[SerializeField] private float jumpCutMultiplier = 0.5f;
	[SerializeField] private float coyoteTime = 0.2f;
	[SerializeField] private float jumpBufferTime = 0.2f;

	[Header("Gravity Settings")]
	[SerializeField] private float gravityScale = 3f;
	[SerializeField] private float fallGravityMultiplier = 1.5f;
	[SerializeField] private float maxFallSpeed = 20f;

	[Header("Ground Check")]
	[SerializeField] private Transform groundCheckPoint;
	[SerializeField] private Vector2 groundCheckSize = new Vector2(0.49f, 0.03f);
	[SerializeField] private LayerMask groundLayer;

	[Header("Air Movement")]
	[SerializeField] private float airMultiplier = 0.8f;
	[SerializeField] private int maxAirJumps = 1;

	[Header("Drop Through Platform")]
	[SerializeField] private float dropThroughDuration = 0.5f;
	[SerializeField] private KeyCode dropKey = KeyCode.S;
	[SerializeField] private float downPush = -2f;

	// Components
	private Rigidbody2D rb;
	private CapsuleCollider2D col;
	private PhysicsMaterial2D noFrictionMaterial;

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

	// Platform velocity
	private Vector2 platformVelocity = Vector2.zero;

	// Drop through - store ALL colliders we're standing on
	private Collider2D standingOnCollider;
	private PlatformEffector2D standingOnEffector;
	private bool isDropping;

	// Optional: For animations
	public bool IsFacingRight { get; private set; } = true;
	public bool IsMoving => Mathf.Abs(rb.linearVelocity.x) > 0.01f;
	public bool IsGrounded => isGrounded;
	public float VerticalVelocity => rb.linearVelocity.y;

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<CapsuleCollider2D>();
		
		// Create no-friction material to prevent sliding
		noFrictionMaterial = new PhysicsMaterial2D("NoFriction");
		noFrictionMaterial.friction = 0f;
		noFrictionMaterial.bounciness = 0f;
		
		col.sharedMaterial = noFrictionMaterial;
	}

	private void Start()
	{
		rb.gravityScale = gravityScale;
		rb.freezeRotation = true;
		rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

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
		horizontalInput = Input.GetAxisRaw("Horizontal");

		// Check for drop-through input
		if (Input.GetKeyDown(dropKey) && isGrounded)
		{
			// Check what we're standing on
			CheckPlatformBelow();
			
			if (standingOnEffector != null)
			{
				Debug.Log($"Dropping through platform with effector!");
				StartCoroutine(DropThroughPlatform());
			}
			else
			{
				Debug.Log("No platform effector found below");
			}
		}

		CheckGrounded();

		if (isGrounded)
		{
			coyoteTimeCounter = coyoteTime;
			airJumpsRemaining = maxAirJumps;
		}
		else
		{
			coyoteTimeCounter -= Time.deltaTime;
		}

		if (Input.GetButtonDown("Jump"))
		{
			jumpBufferCounter = jumpBufferTime;
		}
		else
		{
			jumpBufferCounter -= Time.deltaTime;
		}

		HandleJump();
		HandleSpriteFlip();

		if (Input.GetButtonUp("Jump"))
		{
			jumpInputReleased = true;
		}
	}

	private void FixedUpdate()
	{
		HandleMovement();
		ApplyGravityModifiers();

		if (rb.linearVelocity.y < -maxFallSpeed)
		{
			rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
		}
	}

	private void CheckPlatformBelow()
	{
		// Check what's directly below the player
		RaycastHit2D[] hits = Physics2D.BoxCastAll(
			groundCheckPoint.position, 
			groundCheckSize, 
			0f, 
			Vector2.down, 
			0.1f, 
			groundLayer
		);

		standingOnCollider = null;
		standingOnEffector = null;

		foreach (RaycastHit2D hit in hits)
		{
			if (hit.collider != null && hit.collider != col)
			{
				// Check for platform effector on the hit object
				PlatformEffector2D effector = hit.collider.GetComponent<PlatformEffector2D>();
				
				// If not found, check in parent
				if (effector == null)
				{
					effector = hit.collider.GetComponentInParent<PlatformEffector2D>();
				}
				
				// If not found, check in children
				if (effector == null)
				{
					effector = hit.collider.GetComponentInChildren<PlatformEffector2D>();
				}

				if (effector != null)
				{
					standingOnCollider = hit.collider;
					standingOnEffector = effector;
					Debug.Log($"Found platform effector on: {effector.gameObject.name}, collider on: {hit.collider.gameObject.name}");
					break;
				}
			}
		}
	}

	private IEnumerator DropThroughPlatform()
	{
		if (standingOnCollider == null)
		{
			Debug.LogWarning("No collider to drop through!");
			yield break;
		}

		isDropping = true;
		
		// Find ALL colliders associated with the platform (including composite colliders)
		Collider2D[] platformColliders = standingOnEffector.gameObject.GetComponents<Collider2D>();
		
		// Also check parent and children
		if (platformColliders.Length == 0)
		{
			platformColliders = standingOnEffector.GetComponentsInChildren<Collider2D>();
		}
		
		if (platformColliders.Length == 0)
		{
			platformColliders = standingOnEffector.GetComponentsInParent<Collider2D>();
		}

		Debug.Log($"Disabling collision with {platformColliders.Length} colliders");

		// Disable collision with ALL platform colliders
		foreach (Collider2D platformCol in platformColliders)
		{
			if (platformCol != null)
			{
				Physics2D.IgnoreCollision(col, platformCol, true);
				Debug.Log($"Ignoring collision with: {platformCol.gameObject.name}");
			}
		}

		// Also ignore the specific collider we detected
		if (standingOnCollider != null)
		{
			Physics2D.IgnoreCollision(col, standingOnCollider, true);
		}

		// Clear platform velocity
		ClearPlatformVelocity();
		
		// Push player down
		rb.linearVelocity = new Vector2(rb.linearVelocity.x, -downPush);

		// Wait for drop duration
		yield return new WaitForSeconds(dropThroughDuration);

		// Re-enable collision with ALL platform colliders
		foreach (Collider2D platformCol in platformColliders)
		{
			if (platformCol != null)
			{
				Physics2D.IgnoreCollision(col, platformCol, false);
			}
		}

		if (standingOnCollider != null)
		{
			Physics2D.IgnoreCollision(col, standingOnCollider, false);
		}

		isDropping = false;
		standingOnCollider = null;
		standingOnEffector = null;
		
		Debug.Log("Drop through complete - collisions re-enabled");
	}

	private void HandleMovement()
	{
		float targetSpeed = horizontalInput * moveSpeed;
		targetSpeed += platformVelocity.x;
		float speedDiff = targetSpeed - rb.linearVelocity.x;
		float accelRate = (Mathf.Abs(horizontalInput) > 0.01f) ? acceleration : deceleration;

		if (!isGrounded)
		{
			accelRate *= airMultiplier;
		}

		float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, velPower) * Mathf.Sign(speedDiff);
		rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
	}

	private void HandleJump()
	{
		bool canJump = (coyoteTimeCounter > 0f || airJumpsRemaining > 0) && jumpBufferCounter > 0f;

		if (canJump)
		{
			Jump();
		}

		if (jumpInputReleased && rb.linearVelocity.y > 0 && isJumping)
		{
			rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
			isJumping = false;
		}
	}

	private void Jump()
	{
		if (coyoteTimeCounter <= 0f && !isGrounded)
		{
			airJumpsRemaining--;
		}

		rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
		jumpBufferCounter = 0f;
		coyoteTimeCounter = 0f;
		isJumping = true;
		jumpInputReleased = false;
	}

	private void ApplyGravityModifiers()
	{
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
		isGrounded = Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0f, groundLayer);

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

	public void SetPlatformVelocity(Vector2 vel)
	{
		platformVelocity = vel;
	}

	public void ClearPlatformVelocity()
	{
		platformVelocity = Vector2.zero;
	}

	public void DisableCollisionForPlatform(float duration = 0.5f)
	{
		StartCoroutine(DisableCollision(duration));
	}

	private IEnumerator DisableCollision(float duration)
	{
		int platformLayerIndex = LayerMask.NameToLayer("OneWayPlatform");
		if (platformLayerIndex == -1) yield break;

		Physics2D.IgnoreLayerCollision(gameObject.layer, platformLayerIndex, true);
		yield return new WaitForSeconds(duration);
		Physics2D.IgnoreLayerCollision(gameObject.layer, platformLayerIndex, false);
	}

	private void OnDrawGizmosSelected()
	{
		if (groundCheckPoint != null)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
		}
	}
}
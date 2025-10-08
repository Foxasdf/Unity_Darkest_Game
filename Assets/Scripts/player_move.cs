using UnityEngine;

public class player_move : MonoBehaviour
{
	[Header("Movement")]
	public float moveSpeed = 7f;
	public float runMultiplier = 1.5f;

	[Header("Jumping")]
	public float jumpForce = 10f;
	public float jumpHoldGravityMultiplier = 2.5f;
	public float jumpLowGravityMultiplier = 0.5f;
	public float coyoteTimeDuration = 0.15f; // Time after leaving ground you can still jump

	[Header("Ground Check")]
	public Transform groundCheck;
	public LayerMask groundLayer;
	private readonly float groundCheckRadius = 0.1f;

	private float moveInput;
	private bool isGrounded;
	private bool isJumpHeld;
	private bool isJumpQueued;
	private float lastGroundedTime;
	private Rigidbody2D rb;

	void Start()
	{
		rb = GetComponent<Rigidbody2D>();
	}

	void Update()
	{
		moveInput = Input.GetAxisRaw("Horizontal");

		// Ground check
		isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

		// Track last time we were grounded
		if (isGrounded)
		{
			lastGroundedTime = Time.time;
		}

		// Track jump hold
		isJumpHeld = Input.GetButton("Jump");

		// Jump buffering: press jump slightly before landing

		if (Input.GetButtonDown("Jump"))
		{
			isJumpQueued = true;
			Invoke(nameof(ClearJumpQueue), 0.1f); // Auto-clear after 0.1s
		}
	}

	void FixedUpdate()
	{
		// Apply horizontal movement
		float currentSpeed = moveInput * moveSpeed;
		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
		{
			currentSpeed *= runMultiplier;
		}
		rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);

		// Coyote Time Check: Can jump if recently on ground
		bool canJump = (Time.time - lastGroundedTime) <= coyoteTimeDuration;

		if (isJumpQueued && canJump)
		{
			Jump();
			isJumpQueued = false;
		}

		ApplyVariableGravity();
	}

	void Jump()
	{
		rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
		lastGroundedTime = -coyoteTimeDuration; // Prevent immediate re-jump
	}

	void ApplyVariableGravity()
	{
		if (rb.linearVelocity.y > 0 && isJumpHeld)
		{
			rb.gravityScale = 1f * jumpLowGravityMultiplier;
		}
		else if (rb.linearVelocity.y > 0 && !isJumpHeld)
		{
			rb.gravityScale = jumpHoldGravityMultiplier;
		}
		else
		{
			rb.gravityScale = 3f;
		}
	}

	void ClearJumpQueue()
	{
		isJumpQueued = false;
	}
}
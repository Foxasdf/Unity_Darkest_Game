using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private PlayerMovement2D playerMovement;
	[SerializeField] private FlashlightController flashlightController;
    
	private Animator animator;
	private SpriteRenderer spriteRenderer;
    
	// Animation state hashes for performance
	private static readonly int IdleHash = Animator.StringToHash("Idle");
	private static readonly int RunHash = Animator.StringToHash("Run");
	private static readonly int JumpHash = Animator.StringToHash("Jump");
	private static readonly int IdleFlashlightHash = Animator.StringToHash("Idle_Flashlight");
	private static readonly int RunFlashlightHash = Animator.StringToHash("Run_Flashlight");
	private static readonly int JumpFlashlightHash = Animator.StringToHash("Jump_Flashlight");
    
	// Current animation state tracking
	private int currentStateHash = 0;
	private bool hasFlashlight = false;
	private bool wasGrounded = true;
	private bool jumpAnimationComplete = false;
    
	void Awake()
	{
		animator = GetComponent<Animator>();
		spriteRenderer = GetComponent<SpriteRenderer>();
        
		// Auto-find components if not assigned
		if (playerMovement == null)
			playerMovement = GetComponent<PlayerMovement2D>();
            
		if (flashlightController == null)
			flashlightController = GetComponentInChildren<FlashlightController>();
	}
    
	void Update()
	{
		UpdateAnimation();
	}
    
	void UpdateAnimation()
	{
		if (animator == null || playerMovement == null)
			return;
        
		// Check flashlight state
		hasFlashlight = flashlightController != null && flashlightController.IsFlashlightOn();
        
		// Detect landing
		if (!wasGrounded && playerMovement.IsGrounded)
		{
			// Player just landed
			jumpAnimationComplete = false;
		}
        
		// Determine which animation to play
		int targetStateHash = DetermineAnimationState();
        
		// Only change if state is different
		if (targetStateHash != currentStateHash)
		{
			PlayAnimation(targetStateHash);
			currentStateHash = targetStateHash;
			
			// Reset jump animation flag when starting a jump
			if (targetStateHash == JumpHash || targetStateHash == JumpFlashlightHash)
			{
				jumpAnimationComplete = false;
			}
		}
		// If we're in a jump state, check if animation is complete
		else if ((currentStateHash == JumpHash || currentStateHash == JumpFlashlightHash) 
			&& !jumpAnimationComplete)
		{
			AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
			
			// Check if animation has reached near the end (95% complete)
			if (stateInfo.normalizedTime >= 0.95f)
			{
				jumpAnimationComplete = true;
				// Pause the animator to hold on last frame
				animator.speed = 0f;
			}
		}
		// Resume animator speed when we're not in jump or animation is complete
		else if (animator.speed == 0f && (currentStateHash != JumpHash && currentStateHash != JumpFlashlightHash))
		{
			animator.speed = 1f;
		}
        
		wasGrounded = playerMovement.IsGrounded;
	}
    
	int DetermineAnimationState()
	{
		// Priority: Jump > Run > Idle
        
		// Check if jumping or falling
		if (!playerMovement.IsGrounded)
		{
			return hasFlashlight ? JumpFlashlightHash : JumpHash;
		}
        
		// When landing, resume normal animation speed
		if (animator.speed == 0f)
		{
			animator.speed = 1f;
		}
        
		// Check if player is providing movement input (not just moving from platform)
		bool hasMovementInput = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f;
		
		if (hasMovementInput)
		{
			return hasFlashlight ? RunFlashlightHash : RunHash;
		}
        
		// Default to idle
		return hasFlashlight ? IdleFlashlightHash : IdleHash;
	}
    
	void PlayAnimation(int stateHash)
	{
		// Ensure animator speed is normal when starting new animation
		if (stateHash != JumpHash && stateHash != JumpFlashlightHash)
		{
			animator.speed = 1f;
		}
		
		// Play the animation with crossfade for smooth transitions
		animator.CrossFade(stateHash, 0.1f, 0);
	}
    
	// Optional: Public methods for external control
	public void ForceIdleAnimation()
	{
		animator.speed = 1f;
		int stateHash = hasFlashlight ? IdleFlashlightHash : IdleHash;
		PlayAnimation(stateHash);
		currentStateHash = stateHash;
	}
}
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
        
		// Determine which animation to play
		int targetStateHash = DetermineAnimationState();
        
		// Only change if state is different
		if (targetStateHash != currentStateHash)
		{
			PlayAnimation(targetStateHash);
			currentStateHash = targetStateHash;
		}
	}
    
	int DetermineAnimationState()
	{
		// Priority: Jump > Run > Idle
        
		// Check if jumping or falling
		if (!playerMovement.IsGrounded)
		{
			return hasFlashlight ? JumpFlashlightHash : JumpHash;
		}
        
		// Check if running
		if (playerMovement.IsMoving)
		{
			return hasFlashlight ? RunFlashlightHash : RunHash;
		}
        
		// Default to idle
		return hasFlashlight ? IdleFlashlightHash : IdleHash;
	}
    
	void PlayAnimation(int stateHash)
	{
		// Play the animation with crossfade for smooth transitions
		animator.CrossFade(stateHash, 0.1f, 0);
	}
    
	// Optional: Public methods for external control
	public void ForceIdleAnimation()
	{
		int stateHash = hasFlashlight ? IdleFlashlightHash : IdleHash;
		PlayAnimation(stateHash);
		currentStateHash = stateHash;
	}
}
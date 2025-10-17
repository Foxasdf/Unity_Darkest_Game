using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FlashlightController : MonoBehaviour
{
	[Header("Flashlight Settings")]
	[SerializeField] private Light2D flashlight;
	[SerializeField] private KeyCode toggleKey = KeyCode.F;
	[SerializeField] private float maxDistance = 15f;
	
	[Header("Rotation Offset")]
	[Tooltip("Spot lights point up by default, so we need -90 offset. Point lights use 0.")]
	[SerializeField] private float rotationOffset = -90f;
    
	[Header("Smooth Motion")]
	[SerializeField] private float smoothSpeed = 15f;
	[SerializeField] private bool useSmoothing = true;
    
	[Header("Visual Feedback")]
	[SerializeField] private SpriteRenderer flashlightSprite;
	[SerializeField] private bool showDebugRay = true;
	
	[Header("Detection Settings")]
	[SerializeField] private LayerMask detectionLayers;
	[SerializeField] private LayerMask blockingLayers; // NEW: What blocks the light
	[SerializeField] private float coneAngle = 30f; // Cone angle for spot light detection
    
	private Camera mainCam;
	private bool isFlashlightOn = false;
	private Transform playerTransform;
	private float targetAngle;
	private float currentAngle;
	
	void Start()
	{
		mainCam = Camera.main;
		playerTransform = transform.parent != null ? transform.parent : transform;
        
		if (flashlight != null)
			flashlight.enabled = false;
            
		// Initialize angles to point at mouse immediately
		Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
		mousePos.z = 0f;
		Vector2 direction = (mousePos - transform.position).normalized;
		currentAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffset;
		targetAngle = currentAngle;
		transform.rotation = Quaternion.Euler(0, 0, currentAngle);
	}
	
	void Update()
	{
		HandleFlashlightToggle();
        
		if (isFlashlightOn)
		{
			PointAtMouse();
		}
	}
	
	void HandleFlashlightToggle()
	{
		if (Input.GetKeyDown(toggleKey))
		{
			isFlashlightOn = true;
			if (flashlight != null)
				flashlight.enabled = true;
			if (flashlightSprite != null)
				flashlightSprite.enabled = true;
		}
		else if (Input.GetKeyUp(toggleKey))
		{
			isFlashlightOn = false;
			if (flashlight != null)
				flashlight.enabled = false;
			if (flashlightSprite != null)
				flashlightSprite.enabled = false;
		}
	}
	
	void PointAtMouse()
	{
		// Get mouse position in world space
		Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
		mousePos.z = 0f;
        
		// Calculate direction from flashlight to mouse
		Vector2 direction = (mousePos - transform.position).normalized;
        
		// Calculate target angle with offset
		targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffset;
        
		// Smooth or instant rotation
		if (useSmoothing)
		{
			currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, smoothSpeed * Time.deltaTime);
		}
		else
		{
			currentAngle = targetAngle;
		}
        
		// Apply rotation
		transform.rotation = Quaternion.Euler(0, 0, currentAngle);
	}
	
	// Check if flashlight is pointing at a specific target
	public bool IsPointingAt(Transform target)
	{
		if (!isFlashlightOn || target == null)
			return false;

		// Calculate direction to target
		Vector2 directionToTarget = (target.position - transform.position).normalized;

		// Get flashlight direction (transform.up for spot lights)
		Vector2 flashlightDir = GetFlashlightDirection();

		// Calculate angle between flashlight and target
		float angleToTarget = Vector2.Angle(flashlightDir, directionToTarget);

		// Check if target is within cone angle
		if (angleToTarget > coneAngle / 2f)
			return false;

		// Check distance
		float distanceToTarget = Vector2.Distance(transform.position, target.position);
		if (distanceToTarget > maxDistance)
			return false;

		// Raycast to check if there's a direct line of sight
		// Combine detection and blocking layers so we can see what's in between
		LayerMask combinedMask = detectionLayers | blockingLayers;
		RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToTarget, maxDistance, combinedMask);

		// Only return true if the raycast hits the target directly (no blocking object in between)
		if (hit.collider != null && hit.transform == target)
		{
			return true;
		}

		return false;
	}
	
	void OnDrawGizmosSelected()
	{
		// Visual debug in editor - draws a line showing where the flashlight is pointing
		if (Application.isPlaying && mainCam != null)
		{
			Gizmos.color = isFlashlightOn ? Color.yellow : Color.gray;
			// Use transform.up for spot lights since they point upward
			Gizmos.DrawRay(transform.position, transform.up * maxDistance);
			
			// Draw cone edges
			if (isFlashlightOn)
			{
				Vector2 dir = transform.up;
				Vector2 leftEdge = Quaternion.Euler(0, 0, -coneAngle / 2f) * dir;
				Vector2 rightEdge = Quaternion.Euler(0, 0, coneAngle / 2f) * dir;
				
				Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
				Gizmos.DrawRay(transform.position, leftEdge * maxDistance);
				Gizmos.DrawRay(transform.position, rightEdge * maxDistance);
			}
			
			// Draw line to mouse for debugging
			Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
			mousePos.z = 0f;
			Gizmos.color = Color.red;
			Gizmos.DrawLine(transform.position, mousePos);
		}
	}
	// Check if a world position is lit, optionally ignoring a specific collider (e.g., the platform itself)
	public bool IsPositionLit(Vector2 worldPosition, Collider2D ignoreCollider = null)
	{
		if (!isFlashlightOn)
			return false;

		Vector2 directionToPos = (worldPosition - (Vector2)transform.position).normalized;
		float distance = Vector2.Distance(transform.position, worldPosition);

		if (distance > maxDistance)
			return false;

		Vector2 flashlightDir = GetFlashlightDirection();
		float angleToPos = Vector2.Angle(flashlightDir, directionToPos);
		if (angleToPos > coneAngle / 2f)
			return false;

		// Perform raycast, ignoring the specified collider (e.g., the platform's own collider)
		RaycastHit2D hit = Physics2D.Raycast(
			transform.position,
			directionToPos,
			distance,
			blockingLayers
		);

		// If nothing hit, it's lit
		if (hit.collider == null)
			return true;

		// If the only thing hit is the one we're allowed to ignore, it's still lit
		if (ignoreCollider != null && hit.collider == ignoreCollider)
			return true;

		// Otherwise, something else is blocking
		return false;
	}
	public bool IsFlashlightOn()
	{
		return isFlashlightOn;
	}
	
	public Vector2 GetFlashlightDirection()
	{
		// For spot lights, use transform.up since they point upward
		return transform.up;
	}
    
	public Vector2 GetMouseWorldPosition()
	{
		if (mainCam != null)
		{
			Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
			mousePos.z = 0f;
			return mousePos;
		}
		return Vector2.zero;
	}
}
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
	
	void OnDrawGizmosSelected()
	{
		// Visual debug in editor - draws a line showing where the flashlight is pointing
		if (Application.isPlaying && mainCam != null)
		{
			Gizmos.color = isFlashlightOn ? Color.yellow : Color.gray;
			// Use transform.up for spot lights since they point upward
			Gizmos.DrawRay(transform.position, transform.up * maxDistance);
			
			// Draw line to mouse for debugging
			Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
			mousePos.z = 0f;
			Gizmos.color = Color.red;
			Gizmos.DrawLine(transform.position, mousePos);
		}
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
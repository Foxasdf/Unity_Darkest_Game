using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FlashlightController : MonoBehaviour
{
	[Header("Flashlight Settings")]
	[SerializeField] private Light2D flashlight;
	[SerializeField] private KeyCode toggleKey = KeyCode.F;
	[SerializeField] private float maxDistance = 15f;
    
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
            
		// Initialize angles
		currentAngle = transform.eulerAngles.z;
		targetAngle = currentAngle;
	}

	void Update()
	{
		HandleFlashlightToggle();
        
		if (isFlashlightOn)
		{
			PointAtMouse();
			//CheckFlashlightInteractions();
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
        
		// Calculate target angle
		// Note: Unity's right vector is (1,0), so 0 degrees points right
		targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
		// Smooth or instant rotation
		if (useSmoothing)
		{
			// Smooth angle interpolation with proper wrapping
			currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, smoothSpeed * Time.deltaTime);
		}
		else
		{
			currentAngle = targetAngle;
		}
        
		// Apply rotation
		transform.rotation = Quaternion.Euler(0, 0, currentAngle);
	}

	//void CheckFlashlightInteractions()
	//{
	//	Vector2 origin = transform.position;
	//	Vector2 direction = transform.right; // transform.right is the direction the object is facing
        
	//	RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, maxDistance);
        
	//	if (showDebugRay)
	//	{
	//		Debug.DrawRay(origin, direction * maxDistance, Color.yellow);
	//	}
        
	//	// Track which objects are currently being hit
	//	foreach (RaycastHit2D hit in hits)
	//	{
	//		if (hit.collider != null)
	//		{
	//			ILightInteractable interactable = hit.collider.GetComponent<ILightInteractable>();
	//			if (interactable != null)
	//			{
	//				interactable.OnLightHit(hit.point, direction);
	//			}
	//		}
	//	}
	//}

	void OnDrawGizmosSelected()
	{
		// Visual debug in editor
		if (isFlashlightOn && Application.isPlaying)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawRay(transform.position, transform.right * maxDistance);
		}
	}

	public bool IsFlashlightOn()
	{
		return isFlashlightOn;
	}

	public Vector2 GetFlashlightDirection()
	{
		return transform.right;
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
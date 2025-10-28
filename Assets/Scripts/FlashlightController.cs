using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FlashlightController : MonoBehaviour
{
	[Header("Flashlight Settings")]
	[SerializeField] private Light2D flashlight;
	[SerializeField] private KeyCode toggleKey = KeyCode.Mouse0;
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
	[SerializeField] private LayerMask blockingLayers;
	[SerializeField] private float coneAngle = 30f;

	// NEW: Separate sounds for on/off + audio source
	[Header("Audio")]
	[Tooltip("Sound played when flashlight turns ON")]
	[SerializeField] private AudioClip turnOnSound;
    
	[Tooltip("Sound played when flashlight turns OFF")]
	[SerializeField] private AudioClip turnOffSound;
    
	[SerializeField] private AudioSource audioSource;

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
            
		// Initialize angle to face mouse
		Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
		mousePos.z = 0f;
		Vector2 direction = (mousePos - transform.position).normalized;
		currentAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffset;
		targetAngle = currentAngle;
		transform.rotation = Quaternion.Euler(0, 0, currentAngle);

		// Setup audio source
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
			if (audioSource == null)
			{
				audioSource = gameObject.AddComponent<AudioSource>();
			}
		}

		// Audio settings
		audioSource.playOnAwake = false;
		audioSource.spatialBlend = 1f;         // 3D sound
		audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
		audioSource.minDistance = 5f;
		audioSource.maxDistance = 20f;
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
			sound_manager.play_sound(soundType.flashlight_on);
			//PlaySound(turnOnSound); // Only play "on" sound
		}
		else if (Input.GetKeyUp(toggleKey))
		{
			isFlashlightOn = false;
			if (flashlight != null)
				flashlight.enabled = false;
			if (flashlightSprite != null)
				flashlightSprite.enabled = false;
			
			sound_manager.play_sound(soundType.flashlight_off);
			//PlaySound(turnOffSound); // Only play "off" sound
		}
	}

	// Helper to play any sound safely
	private void PlaySound(AudioClip clip)
	{
		if (clip != null && audioSource != null)
		{
			audioSource.PlayOneShot(clip);
		}
	}
	
	void PointAtMouse()
	{
		Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
		mousePos.z = 0f;
		Vector2 direction = (mousePos - transform.position).normalized;
		targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffset;
		
		if (useSmoothing)
		{
			currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, smoothSpeed * Time.deltaTime);
		}
		else
		{
			currentAngle = targetAngle;
		}
		
		transform.rotation = Quaternion.Euler(0, 0, currentAngle);
	}
	
	public bool IsPointingAt(Transform target)
	{
		if (!isFlashlightOn || target == null)
			return false;

		Vector2 directionToTarget = (target.position - transform.position).normalized;
		Vector2 flashlightDir = GetFlashlightDirection();
		float angleToTarget = Vector2.Angle(flashlightDir, directionToTarget);

		if (angleToTarget > coneAngle / 2f)
			return false;

		float distanceToTarget = Vector2.Distance(transform.position, target.position);
		if (distanceToTarget > maxDistance)
			return false;

		LayerMask combinedMask = detectionLayers | blockingLayers;
		RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToTarget, maxDistance, combinedMask);

		if (hit.collider != null && hit.transform == target)
		{
			return true;
		}

		return false;
	}
	
	void OnDrawGizmosSelected()
	{
		if (Application.isPlaying && mainCam != null)
		{
			Gizmos.color = isFlashlightOn ? Color.yellow : Color.gray;
			Gizmos.DrawRay(transform.position, transform.up * maxDistance);
			
			if (isFlashlightOn)
			{
				Vector2 dir = transform.up;
				Vector2 leftEdge = Quaternion.Euler(0, 0, -coneAngle / 2f) * dir;
				Vector2 rightEdge = Quaternion.Euler(0, 0, coneAngle / 2f) * dir;
				
				Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
				Gizmos.DrawRay(transform.position, leftEdge * maxDistance);
				Gizmos.DrawRay(transform.position, rightEdge * maxDistance);
			}
			
			Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
			mousePos.z = 0f;
			Gizmos.color = Color.red;
			Gizmos.DrawLine(transform.position, mousePos);
		}
	}

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

		RaycastHit2D hit = Physics2D.Raycast(
			transform.position,
			directionToPos,
			distance,
			blockingLayers
		);

		if (hit.collider == null)
			return true;

		if (ignoreCollider != null && hit.collider == ignoreCollider)
			return true;

		return false;
	}
	
	public bool IsFlashlightOn()
	{
		return isFlashlightOn;
	}
	
	public Vector2 GetFlashlightDirection()
	{
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
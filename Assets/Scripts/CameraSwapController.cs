using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;
using System.Collections;

public class CinemachineSwapOnHold : MonoBehaviour
{
	[Header("Cameras (assign in inspector)")]
	public CinemachineCamera normalCam;
	public CinemachineCamera zoomOutCam;
	public CinemachineCamera zoomInCam;

	[Header("Input")]
	public KeyCode holdKey = KeyCode.F;

	[Header("Priority settings")]
	public int highPriority = 20;
	public int lowPriority = 10;

	[Header("Fade Settings")]
	public CanvasGroup fadeCanvasGroup;
	public float fadeDuration = 0.5f;

	private bool isInteracting = false;
	private bool isFading = false;

	void Start()
	{
		if (normalCam == null || zoomOutCam == null || zoomInCam == null)
		{
			Debug.LogError("Assign all three cameras (Normal, ZoomOut, ZoomIn) in the inspector.");
			enabled = false;
			return;
		}

		if (fadeCanvasGroup != null)
			fadeCanvasGroup.alpha = 0f;

		SetActiveCamera(normalCam);
	}

	void Update()
	{
		if (isFading) return; // prevent switching during fade

		if (isInteracting)
		{
			SetActiveCamera(zoomInCam);
		}
		else if (Input.GetKey(holdKey))
		{
			SetActiveCamera(zoomOutCam);
		}
		else
		{
			SetActiveCamera(normalCam);
		}
	}

	void SetActiveCamera(CinemachineCamera active)
	{
		normalCam.Priority = (active == normalCam) ? highPriority : lowPriority;
		zoomOutCam.Priority = (active == zoomOutCam) ? highPriority : lowPriority;
		zoomInCam.Priority = (active == zoomInCam) ? highPriority : lowPriority;
	}

	// Called from interaction scripts
	public void OnInteractionStart()
	{
		if (!isFading)
			StartCoroutine(FadeAndSwitch(zoomInCam, true));
	}

	public void OnInteractionEnd()
	{
		if (!isFading)
			StartCoroutine(FadeAndSwitch(normalCam, false));
	}

	IEnumerator FadeAndSwitch(CinemachineCamera targetCam, bool interacting)
	{
		isFading = true;

		// Fade out
		yield return StartCoroutine(Fade(1));

		// Switch camera
		SetActiveCamera(targetCam);
		isInteracting = interacting;

		// Fade in
		yield return StartCoroutine(Fade(0));

		isFading = false;
	}

	IEnumerator Fade(float targetAlpha)
	{
		if (fadeCanvasGroup == null)
			yield break;

		float startAlpha = fadeCanvasGroup.alpha;
		float elapsed = 0f;

		while (elapsed < fadeDuration)
		{
			elapsed += Time.deltaTime;
			fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
			yield return null;
		}

		fadeCanvasGroup.alpha = targetAlpha;
	}
}

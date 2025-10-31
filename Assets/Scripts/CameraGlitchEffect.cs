using UnityEngine;
using System.Collections;

public class CameraGlitchEffect : MonoBehaviour
{
	[Header("Shake Settings")]
	[Tooltip("How strong the shake is.")]
	public float shakeIntensity = 0.2f;

	[Tooltip("How long the shake lasts.")]
	public float shakeDuration = 0.5f;

	[Header("Glitch Overlay (optional)")]
	[Tooltip("Assign a GameObject or UI Image (e.g., a static/glitch overlay).")]
	public GameObject glitchOverlay;

	private Vector3 originalPos;
	private bool isShaking = false;

	public void TriggerGlitch()
	{
		if (!isShaking)
			StartCoroutine(ShakeAndGlitchRoutine());
	}

	private IEnumerator ShakeAndGlitchRoutine()
	{
		isShaking = true;
		originalPos = transform.localPosition;

		if (glitchOverlay != null)
			glitchOverlay.SetActive(true);

		float elapsed = 0f;

		while (elapsed < shakeDuration)
		{
			float x = Random.Range(-1f, 1f) * shakeIntensity;
			float y = Random.Range(-1f, 1f) * shakeIntensity;
			transform.localPosition = originalPos + new Vector3(x, y, 0);
			elapsed += Time.deltaTime;
			yield return null;
		}

		transform.localPosition = originalPos;

		if (glitchOverlay != null)
			glitchOverlay.SetActive(false);

		isShaking = false;
	}
}

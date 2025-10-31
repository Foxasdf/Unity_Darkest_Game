using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIFlickerEffect : MonoBehaviour
{
	private CameraGlitchEffect camGlitch;

	[Header("Flicker Settings")]
	[Tooltip("Delay before the flicker *starts* (UI will already be visible).")]
	public float flickerStartDelay = 0.3f;

	[Tooltip("Time between flicker flashes (seconds).")]
	public float flickerSpeed = 0.1f;

	[Tooltip("How long the flicker lasts (seconds).")]
	public float flickerDuration = 1.5f;

	[Tooltip("Minimum alpha (0 = invisible, 1 = fully visible).")]
	[Range(0f, 1f)] public float minAlpha = 0.1f;

	[Tooltip("Maximum alpha (0 = invisible, 1 = fully visible).")]
	[Range(0f, 1f)] public float maxAlpha = 1f;

	private Graphic[] graphics;
	private bool isFlickering = false;

	private void Awake()
	{
		graphics = GetComponentsInChildren<Graphic>(true);
	}

	public void StartFlicker()
	{
		if (!isFlickering)
		{
			if (camGlitch == null)
				camGlitch = Camera.main?.GetComponent<CameraGlitchEffect>();

			StartCoroutine(FlickerRoutine());
		}
	}

	private IEnumerator FlickerRoutine()
	{
		isFlickering = true;

		// Wait before flickering begins
		yield return new WaitForSeconds(flickerStartDelay);

		// 💥 Trigger camera shake + glitch
		if (camGlitch != null)
			camGlitch.TriggerGlitch();

		float timer = 0f;
		while (timer < flickerDuration)
		{
			float randomAlpha = Random.Range(minAlpha, maxAlpha);
			SetAlpha(randomAlpha);
			timer += flickerSpeed;
			yield return new WaitForSeconds(flickerSpeed);
		}

		SetAlpha(maxAlpha);
		isFlickering = false;
	}

	private void SetAlpha(float alpha)
	{
		foreach (var g in graphics)
		{
			if (g == null) continue;
			Color c = g.color;
			c.a = alpha;
			g.color = c;
		}
	}
}

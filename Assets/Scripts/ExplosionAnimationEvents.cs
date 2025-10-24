using UnityEngine;

public class ExplosionAnimationEvents : MonoBehaviour
{
	public void ShakeCamera()
	{
		// Make sure CameraShakeTrigger.Instance exists before calling
		if (CameraShakeTrigger.Instance != null)
		{
			CameraShakeTrigger.Instance.ShakeCamera(2f); // You can adjust intensity here
		}
		else
		{
			Debug.LogWarning("No CameraShakeTrigger found in scene!");
		}
	}
}

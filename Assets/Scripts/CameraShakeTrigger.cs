using UnityEngine;
using Unity.Cinemachine;

public class CameraShakeTrigger : MonoBehaviour
{
	public static CameraShakeTrigger Instance;
	private CinemachineImpulseSource impulseSource;

	void Awake()
	{
		Instance = this;
		impulseSource = GetComponent<CinemachineImpulseSource>();
	}

	// Called from Animation Event
	public void ShakeCamera(float intensity = 1f)
	{
		if (impulseSource == null) return;
		impulseSource.GenerateImpulseWithForce(intensity);
	}
}

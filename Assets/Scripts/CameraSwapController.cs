using UnityEngine;
using Unity.Cinemachine;

public class CinemachineSwapOnHold : MonoBehaviour
{
	[Header("Cameras (assign in inspector)")]
	public CinemachineCamera normalCam;
	public CinemachineCamera zoomOutCam;

	[Header("Input")]
	public KeyCode holdKey = KeyCode.F;

	[Header("Priority settings")]
	public int highPriority = 20;
	public int lowPriority = 10;

	void Start()
	{
		// safety: ensure both cams assigned
		if (normalCam == null || zoomOutCam == null)
		{
			Debug.LogError("Assign both Normal and ZoomOut virtual cameras in the inspector.");
			enabled = false;
			return;
		}

		// make sure normalCam is active by default
		SetActiveCamera(normalCam, zoomOutCam);
	}

	void Update()
	{
		// While key is held -> zoom out cam becomes active; otherwise normal
		if (Input.GetKey(holdKey))
			SetActiveCamera(zoomOutCam, normalCam);
		else
			SetActiveCamera(normalCam, zoomOutCam);
	}

	void SetActiveCamera(CinemachineCamera active, CinemachineCamera inactive)
	{
		if (active == null || inactive == null) return;

		// only change if necessary
		if (active.Priority == highPriority && inactive.Priority == lowPriority) return;

		active.Priority = highPriority;
		inactive.Priority = lowPriority;
	}
}

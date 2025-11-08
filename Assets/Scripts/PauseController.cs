using UnityEngine;

public class PauseController : MonoBehaviour
{
	public static bool isGamePaused { get; private set; } = false;
	
	// Reset pause state when scene loads
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	static void Init()
	{
		isGamePaused = false;
	}
	
	void Awake()
	{
		// Also reset on Awake as a safety measure
		isGamePaused = false;
	}
	
	public static void setPaused(bool pause)
	{
		isGamePaused = pause;
		Debug.Log($"Game pause state set to: {pause}");
	}
}
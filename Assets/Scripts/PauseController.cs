using UnityEngine;

public class PauseController : MonoBehaviour
{
	public static bool isGamePaused {get; private set;} =false;
	public static void setPaused(bool pause){
		isGamePaused=pause;
	}
	
}

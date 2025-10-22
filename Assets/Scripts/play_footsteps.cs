using UnityEngine;

public class play_footsteps : MonoBehaviour
{
	public void playsound(){
		sound_manager.play_sound(soundType.run);
	}
}

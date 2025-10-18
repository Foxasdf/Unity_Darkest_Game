using System;
using UnityEngine;




public enum soundType{
	flashlight_on,
	flashlight_off,
	walk,
	run,
	growl
}




[RequireComponent(typeof(AudioSource)),ExecuteInEditMode]

public class sound_manager : MonoBehaviour
{
	[SerializeField] private AudioClip[] soundlist;//this works but we want to add a way to make it so we can have multiple sound clips for 1 action
	//[SerializeField] private SoundList[] soundlist;
	private static sound_manager instance;
	private AudioSource audiosource; 
	
	
	
	private void Awake(){
		instance = this;
	}

	// Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
	    audiosource = GetComponent<AudioSource>();         
    }
	void Update()
	{
        
	}
    
	
	public static void play_sound(soundType sound,float volume = 1){
		instance.audiosource.PlayOneShot(instance.soundlist[(int)sound],volume);
		
	}   
	/* #if UNITY_EDITOR    
	private void on_enable(){
		string[] names= Enum.GetNames(typeof(soundType));
		Array.Resize(ref soundlist,names.Length);
		for (int i = 0; i < soundlist.Length; i++) {
			soundlist[i].name = names[i];
		}
	}
	#endif*/
    // Update is called once per frame
    
}
/*[Serializable]
public struct SoundList{
	[HideInInspector] public String name;
	[SerializeField] private AudioClip[] sounds;
}
*/
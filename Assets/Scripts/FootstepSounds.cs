using UnityEngine;

public class FootstepSounds : MonoBehaviour
{
	[SerializeField] private AudioSource audioSource;
	[SerializeField] private AudioClip[] footstepSounds; // Drag your step sounds here
    
	private int currentStepIndex = 0;

	// This function is called by Animation Events
	public void PlayFootstep()
	{
		if (footstepSounds.Length == 0) return;
        
		// Randomize pitch for variation
		audioSource.pitch = Random.Range(0.09f, 0.15f);
		
		// Play a random footstep sound
		AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
		audioSource.PlayOneShot(clip);
	}
    
	// Or play them in sequence
	public void PlayFootstepSequential()
	{
		if (footstepSounds.Length == 0) return;
		
		// Randomize pitch for variation
		audioSource.pitch = Random.Range(0.09f, 0.15f);
        
		audioSource.PlayOneShot(footstepSounds[currentStepIndex]);
		currentStepIndex = (currentStepIndex + 1) % footstepSounds.Length;
	}
}
using System;
using UnityEngine;

public enum soundType {
    flashlight_on,
    flashlight_off,
    walk,
    run,
    growl
}

[RequireComponent(typeof(AudioSource)), ExecuteInEditMode]
public class sound_manager : MonoBehaviour
{
    [SerializeField] private AudioClip[] soundlist; // this works but we want to add a way to make it so we can have multiple sound clips for 1 action
    private static sound_manager instance;
    private AudioSource audiosource; 

    private void Awake() {
        instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        audiosource = GetComponent<AudioSource>();         
    }

    void Update() {
        
    }

    // public static void play_sound(soundType sound, float volume = 1) {
    //     instance.audiosource.PlayOneShot(instance.soundlist[(int)sound], volume);
    // }       
}

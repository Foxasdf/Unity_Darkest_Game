using UnityEngine;
using System.Collections;
public class InteractionDetector : MonoBehaviour
{
	private IInteractable interactableInRange=null;
	public GameObject interactionIcon;
	private bool nearInteractable=false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
	    interactionIcon.SetActive(false);
    }
	public void OnInteract(){
		if(Input.GetKeyDown(KeyCode.E)){
			Debug.Log("E is pressed");
			interactableInRange?.Interact();
			
		}
		
	}
	private void Update(){
		if(nearInteractable){
		OnInteract();
	}
		
	}
    // Update is called once per frame
	// Sent when another object enters a trigger collider attached to this object (2D physics only).
	private void OnTriggerEnter2D(Collider2D other)
	{
		if(other.TryGetComponent(out IInteractable interactable)&& interactable.CanInteract()){
			interactableInRange=interactable;
			interactionIcon.SetActive(true);
			nearInteractable=true;
		
		}
		
	}
	// Sent when another object leaves a trigger collider attached to this object (2D physics only).
	private void OnTriggerExit2D(Collider2D other)
	{
		if(other.TryGetComponent(out IInteractable interactable)&&interactable ==interactableInRange){
			interactableInRange=null;
			interactionIcon.SetActive(false);
			nearInteractable=false;
		}
	}
}

using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
public class NPC1 : MonoBehaviour,IInteractable
{
	public NPCDialogue dialogueData;
	public GameObject dialoguePanel;
	public TMP_Text dialogueText, nameText;
	public Image  portraitImage;
	private int dialogueIndex;
	private bool isTyping,isDialogueActive;


	public bool CanInteract(){
		return !isDialogueActive;
	}
	
	public void Interact(){
		if(dialogueData ==null || (PauseController.isGamePaused &&!isDialogueActive)){
			return;
		}
		if (isDialogueActive){
			NextLine();
		}
		else{
			//Start Dialogue
			StartDialogue();
		}
		
	}
	
	void NextLine(){
		if(isTyping){
			StopAllCoroutines();
			dialogueText.SetText(dialogueData.dialogueLines[dialogueIndex]);
			isTyping=false;
		}
		else if(++dialogueIndex <dialogueData.dialogueLines.Length){
			StartCoroutine(TypeLine());
		}
		else{
			//end dialogue
			EndDialogue();
		}
	}
	
	void StartDialogue(){
		isDialogueActive=true;
		dialogueIndex=0;
		nameText.SetText(dialogueData.npcName);
		portraitImage.sprite=dialogueData.npcPotrait;
		
		dialoguePanel.SetActive(true);
		PauseController.setPaused(true);
		
		//TypeLine
		StartCoroutine (TypeLine());
		
		
	}
	IEnumerator TypeLine(){
		isTyping =true;
		
		dialogueText.SetText("");
		
		foreach(char letter in dialogueData.dialogueLines[dialogueIndex]){
			dialogueText.text +=letter;
			yield return new WaitForSeconds(dialogueData.typingSpeed);
			
			
		}
		isTyping=false;
		
		if(dialogueData.autoProgressLines.Length>dialogueIndex && dialogueData.autoProgressLines[dialogueIndex]){
			yield return  new WaitForSeconds(dialogueData.autoProgressDelay);
			NextLine();
		}
		
	}
	public void EndDialogue(){
		StopAllCoroutines();
		isDialogueActive=false;
		dialogueText.SetText("");
		dialoguePanel.SetActive(false);
		PauseController.setPaused(false);
		
		
	}

}

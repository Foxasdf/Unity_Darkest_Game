using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class NPC1 : MonoBehaviour, IInteractable
{
	[Header("Dialogue Settings")]
	public NPCDialogue dialogueData;
	public GameObject dialoguePanel;
	public TMP_Text dialogueText, nameText;
	public Image portraitImage;

	[Header("NPC Sprites")]
	public Sprite idleSprite;
	public Sprite talkingSprite;
	public SpriteRenderer npcRenderer; // assign your NPC's SpriteRenderer here

	private int dialogueIndex;
	private bool isTyping, isDialogueActive;
	public GameObject interactionIcon;

	private CinemachineSwapOnHold camSystem;

	void Start()
	{
		camSystem = FindObjectOfType<CinemachineSwapOnHold>();

		if (dialoguePanel != null)
			dialoguePanel.SetActive(false);

		// Ensure the NPC starts with the idle sprite
		if (npcRenderer != null && idleSprite != null)
			npcRenderer.sprite = idleSprite;
	}

	public bool CanInteract() => !isDialogueActive;

	public void Interact()
	{
		if (dialogueData == null || (PauseController.isGamePaused && !isDialogueActive))
			return;

		if (isDialogueActive)
		{
			NextLine();
		}
		else
		{interactionIcon.SetActive(false);
			StartDialogue();
		}
	}

	void StartDialogue()
	{
		isDialogueActive = true;
		dialogueIndex = 0;
		nameText.SetText(dialogueData.npcName);
		portraitImage.sprite = dialogueData.npcPotrait;

		PauseController.setPaused(true);

		// ✅ Trigger camera zoom-in (which fades)
		camSystem?.OnInteractionStart();

		// ✅ Switch NPC sprite to talking
		if (npcRenderer != null && talkingSprite != null)
			npcRenderer.sprite = talkingSprite;

		// ✅ Wait until fade finishes before showing dialogue
		StartCoroutine(ShowDialogueAfterFade());
	}

	IEnumerator ShowDialogueAfterFade()
	{
		yield return new WaitForSeconds(camSystem != null ? camSystem.fadeDuration : 0.5f);
		dialoguePanel.SetActive(true);
		StartCoroutine(TypeLine());
	}

	void NextLine()
	{
		if (isTyping)
		{
			StopAllCoroutines();
			dialogueText.SetText(dialogueData.dialogueLines[dialogueIndex]);
			isTyping = false;
		}
		else if (++dialogueIndex < dialogueData.dialogueLines.Length)
		{
			StartCoroutine(TypeLine());
		}
		else
		{
			EndDialogue();
		}
	}

	IEnumerator TypeLine()
	{
		isTyping = true;
		dialogueText.SetText("");

		foreach (char letter in dialogueData.dialogueLines[dialogueIndex])
		{
			dialogueText.text += letter;
			yield return new WaitForSeconds(dialogueData.typingSpeed);
		}

		isTyping = false;

		if (dialogueData.autoProgressLines.Length > dialogueIndex &&
			dialogueData.autoProgressLines[dialogueIndex])
		{
			yield return new WaitForSeconds(dialogueData.autoProgressDelay);
			NextLine();
		}
	}

	public void EndDialogue()
	{
		StopAllCoroutines();
		isDialogueActive = false;
		dialogueText.SetText("");
		dialoguePanel.SetActive(false);
		PauseController.setPaused(false);

		// ✅ Switch NPC sprite back to idle
		if (npcRenderer != null && idleSprite != null)
			npcRenderer.sprite = idleSprite;

		interactionIcon.SetActive(false);
		// ✅ Return camera to normal (with fade)
		camSystem?.OnInteractionEnd();
	}
}

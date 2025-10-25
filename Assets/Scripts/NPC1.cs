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

	private int dialogueIndex;
	private bool isTyping, isDialogueActive;

	private CinemachineSwapOnHold camSystem; // reference to camera system

	void Start()
	{
		// Find the camera system in the scene
		camSystem = FindObjectOfType<CinemachineSwapOnHold>();

		// Ensure dialogue panel is hidden at start
		if (dialoguePanel != null)
			dialoguePanel.SetActive(false);
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
		{
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

		// ✅ Wait until fade finishes before showing panel
		StartCoroutine(ShowDialogueAfterFade());
	}

	IEnumerator ShowDialogueAfterFade()
	{
		// Wait slightly longer than the fade duration (for safety)
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

		// ✅ Return camera to normal (with fade)
		camSystem?.OnInteractionEnd();
	}
}

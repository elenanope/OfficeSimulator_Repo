using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using JetBrains.Annotations;

public class DialogueManager : MonoBehaviour
{

    [Header("Text References")]
    [SerializeField] GameObject dialoguePanel;
    [SerializeField] TMPro.TMP_Text dialogueText;

    float typingTime;
    bool playerInRange;
    bool didDialogueStart;
    bool dialogueOver;
    int lineIndex;

    [Header("Dialogo Arrays")]
    [SerializeField, TextArea(2, 4)] string[] dialogueLines;

    public void StartDialogue()
    {
        if (!didDialogueStart)
        {
            didDialogueStart = true;
            dialoguePanel.SetActive(true);
            StartCoroutine(ShowLine());
        }
    }

    void NextDialogueLine()
    {
        didDialogueStart = false;
        dialogueOver = true;
        dialoguePanel.SetActive(false);
    }

    private IEnumerator ShowLine()
    {
        dialogueText.text = string.Empty;

        foreach (char ch  in dialogueLines[lineIndex])
        {
            dialogueText.text+= ch;
            yield return new WaitForSecondsRealtime(typingTime);
        }
    }

    public void Dialogue(int lineToRead)
    {
        lineIndex = lineToRead;
        if (playerInRange&&!dialogueOver)
        {
            if (!didDialogueStart) StartDialogue();
            else if (dialogueText.text == dialogueLines[lineIndex]) NextDialogueLine();
            else
            {
                StopAllCoroutines();
                dialogueText.text = dialogueLines[lineIndex];
            }
        }

        if (playerInRange && dialogueOver)
        {
            if (!didDialogueStart)
            {
                didDialogueStart = true;
                dialoguePanel.SetActive(true);
                StartCoroutine(ShowLine());
            }
            else if (dialogueText.text == dialogueLines[lineIndex]) NextDialogueLine();
            else
            {
                StopAllCoroutines();
                dialogueText.text = dialogueLines[lineIndex];
            }
        }
    }
}

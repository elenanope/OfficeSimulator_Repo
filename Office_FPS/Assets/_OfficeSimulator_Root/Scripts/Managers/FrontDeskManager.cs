using UnityEngine;
using System.Collections;

public class FrontDeskManager : MonoBehaviour
{
    [Header("Objects Management")]
    int activityChosen;
    public bool objectsSet;
    [SerializeField] Material[] sheetsMats; //materiales de folios: 0 + 1 básicos, 2+3 confidential, 4 blue, 5 green, 6 yellow
    public GameObject visitorCard;
    [SerializeField] GameObject paper1;
    [SerializeField] GameObject paper2;//público para impresora
    [SerializeField] MeshRenderer meshRenderer1;
    [SerializeField] MeshRenderer meshRenderer2;
    [SerializeField] Collider col1;
    [SerializeField] Collider col2;
    [SerializeField] Rigidbody rb1;
    [SerializeField] Rigidbody rb2;
    [SerializeField] TipoObjeto infoPaper1; //solo modificar al resetearlos
    [SerializeField] TipoObjeto infoPaper2;
    public Transform spawnPoint1;
    [SerializeField] Transform spawnPoint2;
    [SerializeField] Transform printingPoint;
    [SerializeField] int eleccion1 = 0;

    [Header("Dialogue Manager")]
    [SerializeField] GameObject dialoguePanel;
    [SerializeField] TMPro.TMP_Text dialogueText;

    float typingTime;
    bool didDialogueStart;
    [SerializeField]bool dialogueOver;
    int lineIndex;

    [SerializeField, TextArea(2, 4)] string[] dialogueLines;// 0 grapar, 1 fotocopiar, 2 acreditaciones, 3 deshacerte, 4 clasificar documentos, 5 Bienn gracias, 6 noo, pero va que tengo prisa
                                                            // 7 paciencia acabada: lo siento pero no has hecho muy buen trabajo, 8 yo no queria eso, adios, 9 mal, inadmisible (a la primera)

    [SerializeField] GameObject pointsText;
    [SerializeField] GameObject strikesText;

    private void Awake()
    {
        paper1.transform.position = spawnPoint1.position;
        paper2.transform.position = spawnPoint2.position;
        paper1.SetActive(false);
        paper2.SetActive(false);
        didDialogueStart = false;
    }
    #region Office Tasks
    public void Printing()//solo llamar si hay folio encima
    {
        paper2.SetActive(true);
        paper2.transform.position = printingPoint.position;
        infoPaper2.paperType = eleccion1;
        //meshRenderer2.material = sheetsMats[materialChosen];
    }
    public void StartActivity(int favourAsked)
    {
        activityChosen = favourAsked;
        Dialogue(favourAsked); 
        if (!objectsSet)
        {
            PrepareObjects(activityChosen);
        }
    }
    public void PrepareObjects(int favourAsked)
    {
        objectsSet = true;
        activityChosen = favourAsked;
        int eleccion2 = 0;
        if (favourAsked == 0)
        {
            paper1.SetActive(true);
            paper2.SetActive(true);
            eleccion1 = Random.Range(0, 2);
            eleccion2 = Random.Range(0, 2);
            meshRenderer1.material = sheetsMats[eleccion1];
            meshRenderer2.material = sheetsMats[eleccion2];
            infoPaper1.paperType = eleccion1;
            infoPaper2.paperType = eleccion2;
        }
        else if(favourAsked == 1)
        {
            paper1.SetActive(true);
            eleccion1 = Random.Range(0, 2);//ajustar numeros
            meshRenderer1.material = sheetsMats[eleccion1];
            infoPaper1.paperType = eleccion1;
        }
        else if(favourAsked == 2)
        {
            //visitCards las gestionas tu, así que aqui creo que no irá nada
        }
        else if(favourAsked == 3)
        {
            paper1.SetActive(true);
            //paper2.SetActive(true);
            eleccion1 = Random.Range(0, 4);
            //eleccion2 = Random.Range(0, 4); //ajustar
            meshRenderer1.material = sheetsMats[eleccion1];
            //meshRenderer2.material = sheetsMats[eleccion2];
            infoPaper1.paperType = eleccion1;
            //infoPaper2.paperType = eleccion2;
            //luego se comprueba en el interact que si tiras uno a la basura no tenga confidencial activado o viceversa, si lo haces mal, strike
        }
        else if(favourAsked == 4)
        {
            paper1.SetActive(true);
            //paper2.SetActive(true);
            eleccion1 = Random.Range(0, sheetsMats.Length);
            //eleccion2 = Random.Range(0, sheetsMats.Length);
            meshRenderer1.material = sheetsMats[eleccion1];
            //meshRenderer2.material = sheetsMats[eleccion2];
            infoPaper1.paperType = eleccion1;
            //infoPaper2.paperType = eleccion2;
            //luego se comprueba en el interact que si tiras uno a la basura no tenga confidencial activado o viceversa, si lo haces mal, strike
        }
    }
    public void ResetObjects()
    {
        objectsSet = false;
        paper1.transform.position = spawnPoint1.position;
        paper2.transform.position = spawnPoint2.position;
        paper1.transform.parent = null;
        paper2.transform.parent = null;
        infoPaper1.activityDone = -1; // no sé si hace falta algo más
        infoPaper2.activityDone = -1;
        col1.enabled = true;
        col2.enabled = true;
        rb1.useGravity = true;
        rb2.useGravity = true;
        rb1.isKinematic = false;
        rb2.isKinematic = false;
        paper1.SetActive(false);
        paper2.SetActive(false);
    }
    #endregion
    #region Dialogue Management
    void StartDialogue()
    {
            didDialogueStart = true;
            dialoguePanel.SetActive(true);
            StartCoroutine(ShowLine());
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

        foreach (char ch in dialogueLines[lineIndex])
        {
            dialogueText.text += ch;
            yield return new WaitForSecondsRealtime(typingTime);
        }
        if(lineIndex <5 || lineIndex == 6) yield return new WaitForSecondsRealtime(2);
        else yield return new WaitForSecondsRealtime(1);
        NextDialogueLine();
    }

    public void Dialogue(int lineToRead)
    {
        lineIndex = lineToRead;
        if (lineIndex < 5 || lineIndex == 6)
        {
            if (!didDialogueStart)
            {
                StopAllCoroutines();
                StartDialogue();
            }
        }
        else
        {
            if (!didDialogueStart)
            {
                StopAllCoroutines();
            }

            StartDialogue();
        }
            
    }

    #endregion
    public IEnumerator ShowNotification(bool isPositive)
    {
        if(isPositive) pointsText.SetActive(true);
        else strikesText.SetActive(true);
        yield return new WaitForSeconds(2);
        if (isPositive) pointsText.SetActive(false);
        else strikesText.SetActive(false);
    }
}

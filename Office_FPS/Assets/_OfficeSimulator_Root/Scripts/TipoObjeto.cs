using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TipoObjeto : MonoBehaviour
{
    public int objectType = 0; //0 black, 1 yellow, 2 pink, 3 cyan, 4 Grapadora, 5 Acreditaciones - (npc), 6 trituradora, 7 papelera - bolsa, 8 2/3 bandejas para documentos - papeles
                               //9 taza, 10 boton impresora imprimir
    
    public bool mainPart;
    public bool canBeUsedAlone;//para botones random
    public bool isFull;
    public GameObject objectOnTop = null;
    public bool pickable;
    public int activityDone = -1;//0 Grapados, 1 papeles impresos, 2 Acreditaciones, 
    public int partsLeft; //grapas, etc
    [SerializeField] bool unlimited; //grapas, etc
    [SerializeField] bool justAButton;
    [SerializeField] int[] coloursLeft; //0 black, 1 yellow, 2 pink, 3 cyan
    int maxParts;
    public bool workProp;//grapadora, etc. para que al cogerlo esté en una posición distinta
    //public bool sticky;
    public Transform initialPoint;
    private void Awake()
    {
        maxParts = partsLeft;
        //if(heldObject == 4 && mainPart)afecta a objectType==8 si no es mainpart
        //if(heldObject == 8 && !mainPart)afecta a objectType== 6, 7, 8  si es mainpart
    }
    public void UseObject()
    {
        if(isFull)
        {
            if(!unlimited)partsLeft--;
            if (partsLeft == 0) isFull = false;
            Debug.Log("Has usado el objeto que llevas");
            if(objectType == 4)
            {
                Debug.Log("Usas la grapadora");
            }
        }
        else if(justAButton)
        {
            if(objectType == 10)
            {
                if (partsLeft > 0 && objectOnTop != null) isFull = true;//el maximo de folios son 10
                else isFull = false;
                if (coloursLeft[0] == 10 && coloursLeft[1] == 10 && coloursLeft[2] == 10 && coloursLeft[3] == 10 && isFull)
                {
                    Debug.Log("Button pressed");
                    coloursLeft[0] -= Random.Range(0, 4);
                    coloursLeft[1] -= Random.Range(0, 3);
                    coloursLeft[2] -= Random.Range(0, 3);
                    coloursLeft[3] -= Random.Range(0, 3);
                    partsLeft--;
                }
                else
                {
                    if (coloursLeft[0] != 10) Debug.Log("Te falta negro");
                    else if (coloursLeft[1] != 10) Debug.Log("Te falta amarillo");
                    else if (coloursLeft[2] != 10) Debug.Log("Te falta rosa");
                    else if (coloursLeft[3] != 10) Debug.Log("Te falta cyan");
                    else if (!isFull) Debug.Log("Te falta papel");
                }
            }
            else if(objectType == 15 && !mainPart)
            {
                SceneManager.LoadScene(0);
            }
            //Hacer lo que haga el botón
            //reproducir sonido de boton
        }
        else if(objectType == 7 && !mainPart)
        {
            //recoger la bolsa y cambia una condición (pickable?) si le vuelves a dar podrás cogerlo
        }
    }
    public void Replenish(int objectType)
    {
        
        if (!unlimited && objectType!= 10)
        {
            isFull = true;
            transform.GetChild(0).gameObject.SetActive(true);
            partsLeft = maxParts;
        }
        else if (!unlimited && objectType == 10)
        {
            if(objectType >= 0 && objectType < 4)
            {
                if (objectType == 0) coloursLeft[0] = 10;
                else if (objectType == 1) coloursLeft[1] = 10;
                else if (objectType == 2) coloursLeft[2] = 10;
                else if (objectType == 3) coloursLeft[3] = 10;
            }
            else
            {
                if(objectType == 8)
                {
                    isFull = true;
                    partsLeft = maxParts;
                }
            }
        }
    }
}

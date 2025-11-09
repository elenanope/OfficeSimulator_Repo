using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractingSystem : MonoBehaviour
{
    #region General Variables
    [Header("General References")]
    [SerializeField] Camera fpsCam; //Ref si disparamos desde el centro de la cam
    [SerializeField] LayerMask impactLayer; //Layer con la que el Raycast interactúa
    [SerializeField] LayerMask NPCLayer;
    RaycastHit hit; //Almacén de la información de los objetos con los que impactan los disparos
    FPSController scriptController;
    CoffeeMaker coffeeMaker = null;
    

    [Header("Interacting Parameters")]
    [SerializeField] GameObject heldObject = null;
    [SerializeField] Transform holdingPoint;
    [SerializeField] float range = 4f;
    [SerializeField] float interactingCooldown = 0.1f; //Tiempo entre disparos
    [SerializeField] float reloadTime = 1.5f; //Tiempo entre disparos


    [Header("Feedback References")]
    [SerializeField] GameObject impactEffect; //Referencia al VFX de impacto de bala
    [SerializeField] GameManager gameManager;

    //Bools de estado
    [SerializeField] bool interacting; //Indica que estamos disparando
    [SerializeField] bool leaving; //Indica que estamos disparando
    [SerializeField] bool canInteract; //Indica que en este momento del juego se puede disparar
     bool isHoldingCoffee; //Indica si podemos recargar
     bool reloadingCoffee;
    int taskDone = -1;
    Mug mug;
    public NPCAIBase npcAtFrontDesk = null;

    #endregion

    private void Awake()
    {
        scriptController = GetComponent<FPSController>();
        canInteract = true;
    }

    void Start()
    {
        //impactEffect.SetActive(false); //Apaga el efecto de impacto al iniciar el juego

    }

    void Update()
    {
        if (!gameManager.someoneInSecretary && npcAtFrontDesk != null) npcAtFrontDesk = null;
        if (!reloadingCoffee)
        {
            if (interacting && canInteract)
            {
                //Inicializar la corrutina de disparo
                StartCoroutine(InteractRoutine());
                Debug.Log("Interacting try");
            }
        }
    }

    IEnumerator InteractRoutine()
    {
        canInteract = false; //Previene la acumulación por frame de disparos
        Interact();
        yield return new WaitForSeconds(interactingCooldown);
        canInteract = true; //Resetea la posibilidad de disparar
    }

    void Interact()
    {
        //poner que cuando dejes un objeto (enciendas un hijo y apagues el heldobject) este vuelva a aparecer en su sitio original si no está a tu vista (si no queda mal)
        Vector3 direction = fpsCam.transform.forward;
        if (heldObject == null)
        {
            //Physics.Raycast(Origen del rayo, dirección, almacén de info de impacto, longitud del rayo, layer a la que impacta (opcional)
            if (Physics.Raycast(fpsCam.transform.position, direction, out hit, range, impactLayer))
            {
                if(hit.collider.GetComponent<CoffeeMaker>() != null)
                {
                    hit.collider.GetComponent<CoffeeMaker>().PressButton();
                }
                /*else if (hit.collider.TryGetComponent(out TipoObjeto tocado))
                {
                    if(tocado.canBeUsedAlone)tocado.UseObject();
                }*/ //esto para el click derecho
                else
                {
                    if (hit.collider.GetComponent<Mug>() != null)
                    {
                        mug = hit.collider.GetComponent<Mug>();
                        if (coffeeMaker != null && coffeeMaker.mug == mug)
                        {
                            coffeeMaker.AdministrateMug(false, mug);
                        }
                        if (mug.isFull) isHoldingCoffee = true;
                        else isHoldingCoffee = false;

                        heldObject = hit.collider.gameObject;
                        heldObject.transform.SetParent(fpsCam.transform);
                        heldObject.GetComponent<Collider>().enabled = false;
                        if (heldObject.TryGetComponent(out Rigidbody rb))
                        {
                            rb.isKinematic = true;
                            rb.useGravity = false;
                        }
                        heldObject.transform.position = holdingPoint.position;
                    }
                    if (hit.collider.GetComponent<TipoObjeto>() != null)
                    {
                        if (hit.collider.GetComponent<TipoObjeto>().pickable)
                        {

                            heldObject = hit.collider.gameObject;
                            heldObject.transform.SetParent(fpsCam.transform);
                            heldObject.GetComponent<Collider>().enabled = false;
                            if (heldObject.TryGetComponent(out Rigidbody rb))
                            {
                                rb.isKinematic = true;
                                rb.useGravity = false;
                            }
                            heldObject.transform.position = holdingPoint.position;
                        }
                        else if (hit.collider.TryGetComponent(out TipoObjeto señalado))
                        {
                            if (señalado.objectOnTop != null)
                            {
                                heldObject = señalado.objectOnTop;
                                heldObject.transform.SetParent(fpsCam.transform);
                                heldObject.GetComponent<Collider>().enabled = false;
                                if (heldObject.TryGetComponent(out Rigidbody rb))
                                {
                                    rb.isKinematic = true;
                                    rb.useGravity = false;
                                }
                                heldObject.transform.position = holdingPoint.position;
                                señalado.objectOnTop = null;
                                Debug.Log("se coge el folio de la impresora");
                            }
                            else if (señalado.canBeUsedAlone) señalado.UseObject();
                        }
                    }
                }
            }
            if(Physics.Raycast(fpsCam.transform.position, direction, out hit, range, NPCLayer))
            {
                if (hit.collider.TryGetComponent(out NPCAIBase npc))
                {
                    Debug.Log("Trying to interact with NPC");
                    npc.AskForFavour();
                }
            }
        }
        else
        { 
            if (Physics.Raycast(fpsCam.transform.position, direction, out hit, range, impactLayer))
            {
                if (heldObject.TryGetComponent(out TipoObjeto equipado) && hit.transform.TryGetComponent(out TipoObjeto señalado))
                {
                    
                    if (señalado.mainPart && equipado.objectType!= 8 && equipado.objectType == señalado.objectType) //recargar
                    {
                        if ((señalado.initialPoint != null && señalado.transform.position != señalado.initialPoint.position) || señalado.initialPoint == null)
                        {
                            if (!señalado.gameObject.transform.GetChild(0).gameObject.activeSelf)
                            {
                                if(señalado.objectType >= 0 && señalado.objectType < 4)
                                {
                                    señalado.Replenish(equipado.objectType);
                                    heldObject.gameObject.SetActive(false);
                                    equipado.transform.position = equipado.initialPoint.position;//o poner coroutina para que tarde en salir de nuevo
                                    heldObject = null;
                                }
                                
                            }
                        }
                    }
                    else if(equipado.objectType == 4 && equipado.mainPart)
                    {
                        if (equipado.isFull)
                        {
                            RaycastHit[] hits;
                            GameObject[] pages = new GameObject[3];
                            int index = 0;
                            if (señalado.objectType == 8 && !señalado.mainPart)
                            {
                                //sprite de grapa, coger objeto vacío
                                Vector3 stapledPoint = new Vector3(hit.point.x, hit.point.y + 1, hit.point.z);
                                hits = Physics.RaycastAll(stapledPoint, Vector3.down, 2, impactLayer);
                                foreach (RaycastHit raycastHit in hits)
                                {
                                    if (raycastHit.collider.GetComponent<TipoObjeto>().objectType == 8)
                                    {
                                        equipado.partsLeft--;
                                        pages[index] = raycastHit.collider.gameObject;
                                        index++;
                                        if (index >= 3)
                                        {
                                            break;
                                        }
                                    }
                                }
                                if (pages[1] != null)
                                {
                                    pages[0].GetComponent<TipoObjeto>().activityDone = 0;
                                    pages[1].GetComponent<TipoObjeto>().activityDone = 0;
                                    pages[1].GetComponent<Rigidbody>().isKinematic = true;
                                    pages[1].GetComponent<Rigidbody>().useGravity = false;
                                    pages[1].GetComponent<Collider>().enabled = false;
                                    pages[1].transform.SetParent(pages[0].transform);
                                }
                                else if (pages[2] != null)
                                {
                                    pages[2].GetComponent<Rigidbody>().isKinematic = true;
                                    pages[2].GetComponent<Rigidbody>().useGravity = false;
                                    pages[2].GetComponent<Collider>().enabled = false;
                                    pages[2].transform.SetParent(pages[0].transform);
                                }
                            }
                        }
                        else Debug.Log("No te quedan grapas");
                    }
                    else if (señalado.canBeUsedAlone) //solo para pulsar botones?? retocaresto
                    {
                        señalado.UseObject();
                    }
                    else if(equipado.objectType == 5 && señalado.TryGetComponent(out VisitorAI visitor))
                    {
                        equipado.enabled = false;
                        Debug.Log("Acreditación dada!");
                        //se marca como tarea completada en visitante
                    }
                    else if (equipado.objectType == 8 && !equipado.mainPart) //si llevas un folio
                    {
                        if(señalado.objectType == 6)
                        {
                            equipado.gameObject.SetActive(false);
                            heldObject = null;
                            Debug.Log("Hoja siendo triturada");
                            npcAtFrontDesk.Receive(null, 3);
                            equipado = null;
                            //animación, subir numero de papeles dentro
                        }
                        else if(señalado.objectType == 7)
                        {
                            equipado.gameObject.SetActive(false);
                            heldObject = null;
                            Debug.Log("Hoja tirada a la basura"); //poner que si un folio acaba pq si en el collider de la papelera, sea strike directo y se convierta en basura
                            //añadir bolas a la papelera y subir stats de full
                            npcAtFrontDesk.Receive(null, 5);
                            equipado = null;
                        }
                        else if(señalado.objectType == 8 && señalado.mainPart)
                        {
                            equipado.gameObject.SetActive(false);
                            heldObject = null;
                            Debug.Log("Hoja metida en casillero");
                            npcAtFrontDesk.Receive(null, 4);
                            equipado = null;
                        }
                        else if (señalado.objectType == 10 && señalado.mainPart)
                        {
                            //se pone en la posición de fotocopiar
                            señalado.objectOnTop = equipado.gameObject;
                            heldObject.transform.position = señalado.initialPoint.position;
                            heldObject.transform.parent = señalado.gameObject.transform;
                            heldObject = null;
                            equipado = null;
                            Debug.Log("Folio se queda en impresora");
                            //equipado.GetComponent<Collider>().enabled = false;

                        }
                        else LeaveItem();
                    }
                    else if (equipado.objectType == 10 && !equipado.mainPart)//paquete de folios para impresora
                    {
                        if (señalado.objectType == 10 && señalado.mainPart)
                        {
                            equipado.gameObject.SetActive(false);
                            heldObject = null;
                            señalado.Replenish(equipado.objectType);
                        }

                    }
                    
                            
                        
                    
                    else LeaveItem();
                }
                else LeaveItem();
            }
            else if(Physics.Raycast(fpsCam.transform.position, direction, out hit, range, NPCLayer)) //testing
            {
                if(hit.transform.TryGetComponent(out NPCAIBase scriptNPC))
                {
                    scriptNPC.Receive(heldObject.GetComponent<TipoObjeto>(), -1);
                    Debug.Log("el npc recibe algo");
                }
            }
            else if (Physics.Raycast(fpsCam.transform.position, direction, out hit, range) && heldObject.TryGetComponent(out TipoObjeto sostenido))
            {
                if (hit.transform.name == "SM_Printer" || hit.transform.name == "SM_CoffeeMaker_Body")
                {
                    if(sostenido.objectType == hit.collider.GetComponent<TipoObjeto>().objectType)
                    {
                        if (heldObject.TryGetComponent(out Rigidbody rb))
                        {
                            rb.isKinematic = true;
                            rb.useGravity = false;
                        }
                        heldObject.GetComponent<Collider>().enabled = true;
                        if(sostenido.initialPoint != null)
                        {
                            if (hit.transform.GetComponentInParent<CoffeeMaker>() != null)
                            {
                                coffeeMaker = hit.transform.GetComponentInParent<CoffeeMaker>();
                                if(!coffeeMaker.hasMug)
                                {
                                    heldObject.transform.position = sostenido.initialPoint.position;
                                    heldObject.transform.rotation = sostenido.initialPoint.rotation;
                                    coffeeMaker.AdministrateMug(true, mug);
                                    if (mug != null) mug = null;
                                }
                                else
                                {
                                    Debug.Log("Ya hay una taza");
                                }    
                            }
                            else
                            {
                                heldObject.transform.position = sostenido.initialPoint.position;
                                heldObject.transform.rotation = sostenido.initialPoint.rotation;
                            }
                        }
                        heldObject.transform.parent = null;
                        heldObject = null; //se queda en posición donde estaba actualmente o no
                    }
                }
                else LeaveItem();
            }
            else
            {
                LeaveItem();
            }
            
            //poner UI cuando hoverees sobre botones interactuables, donde puedes devolver tu objeto o abrir algo
            //UI semipermanente -> cuando tengas un heldObject se activará el texto del botón con el que puedes droppear el objeto

        }
        interacting = false;

    }
    
    void LeaveItem()
    {
        Vector3 direction = fpsCam.transform.forward;
        leaving = false;
        if (Physics.Raycast(fpsCam.transform.position, direction, out hit, range))
        {
            if (mug != null) mug = null;
            //Debug.Log(hit.collider.name);
            Vector3 dropPosition = hit.point + hit.normal * 0.1f;
            heldObject.transform.position = dropPosition;
            //Debug.Log(hit.point);
            //if (hit.collider.CompareTag("Walls")) heldObject.transform.position = new Vector3(hit.transform.position.x, hit.transform.position.y, hit.transform.position.z);
            //else heldObject.transform.position = new Vector3 (hit.transform.position.x, hit.transform.position.y + 1, hit.transform.position.z);
            if (heldObject.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            heldObject.GetComponent<Collider>().enabled = true;
            heldObject.transform.parent = null;
            heldObject = null; //se queda en posición donde estaba actualmente o no
        }
        
    }

    IEnumerator ReloadRoutine()
    {
        Animator mugAnim = mug.gameObject.GetComponent<Animator>();
        mugAnim.SetBool("isFull", false);
        isHoldingCoffee = false;
        reloadingCoffee = true;
        mug.isFull = false;
        //Se llama a la animación de recarga
        //yield return new WaitForSeconds(reloadTime);
        scriptController.energy += 50;
        //Debug.Log("Se ha añadido energía");
        if (scriptController.energy > 100) scriptController.energy = 100;
        reloadingCoffee = false;
        yield return null;
    }

    #region Input Methods
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) interacting = true;
    }
    public void OnRecharge(InputAction.CallbackContext ctx)
    {
        if(ctx.performed)
        {
            if(isHoldingCoffee)
            {
                StartCoroutine(ReloadRoutine());
            }
            else
            {
                if (heldObject.TryGetComponent(out TipoObjeto objectType))
                {
                    objectType.UseObject();
                }
                //No puedo hacer nada!
                //si está con la grapadora, descontar grapas de un script aparte-
            }
        }
    }


    #endregion
}

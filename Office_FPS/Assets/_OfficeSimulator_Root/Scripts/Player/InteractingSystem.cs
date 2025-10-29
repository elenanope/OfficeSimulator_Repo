using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractingSystem : MonoBehaviour
{
    #region General Variables
    [Header("General References")]
    [SerializeField] Camera fpsCam; //Ref si disparamos desde el centro de la cam
    [SerializeField] LayerMask impactLayer; //Layer con la que el Raycast interact�a
    RaycastHit hit; //Almac�n de la informaci�n de los objetos con los que impactan los disparos
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

    //Bools de estado
    [SerializeField] bool interacting; //Indica que estamos disparando
    [SerializeField] bool leaving; //Indica que estamos disparando
    [SerializeField] bool canInteract; //Indica que en este momento del juego se puede disparar
     bool isHoldingCoffee; //Indica si podemos recargar
     bool reloadingCoffee;
    Mug mug;

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
        canInteract = false; //Previene la acumulaci�n por frame de disparos
        Interact();
        yield return new WaitForSeconds(interactingCooldown);
        canInteract = true; //Resetea la posibilidad de disparar
    }

    void Interact()
    {
        //poner que cuando dejes un objeto (enciendas un hijo y apagues el heldobject) este vuelva a aparecer en su sitio original si no est� a tu vista (si no queda mal)
        Vector3 direction = fpsCam.transform.forward;
        if (heldObject == null)
        {
            //Physics.Raycast(Origen del rayo, direcci�n, almac�n de info de impacto, longitud del rayo, layer a la que impacta (opcional)
            if (Physics.Raycast(fpsCam.transform.position, direction, out hit, range, impactLayer))
            {
                if(hit.collider.GetComponent<CoffeeMaker>() != null)
                {
                    hit.collider.GetComponent<CoffeeMaker>().PressButton();

                    //primero que pongas la taza y caiga el liquido, despu�s ya recharge

                }
                else
                {
                    if (hit.collider.GetComponent<Mug>() != null)
                    {
                        mug = hit.collider.GetComponent<Mug>();
                        if(coffeeMaker != null && coffeeMaker.mug == mug)
                        {
                            coffeeMaker.AdministrateMug(false, mug);
                        }
                        if (mug.isFull) isHoldingCoffee = true;
                        else isHoldingCoffee = false;
                    }
                    Debug.Log(hit.collider.name);
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
                
            }
        }
        else
        { 
            if (Physics.Raycast(fpsCam.transform.position, direction, out hit, range, impactLayer))
            {
                //CartuchosImpresora
                if (heldObject.TryGetComponent(out TipoObjeto equipado))
                {
                    if(hit.transform.TryGetComponent(out TipoObjeto se�alado))
                    {
                        if (equipado.mainPart)
                        {
                            if (equipado.objectType == se�alado.objectType)
                            {
                                if (!heldObject.transform.GetChild(0).gameObject.activeSelf)
                                {
                                    heldObject.transform.GetChild(0).gameObject.SetActive(true);
                                    hit.collider.gameObject.SetActive(false);
                                }
                                else Debug.Log("Ya est� lleno!");
                            }
                        }
                        else if(se�alado.mainPart)
                        {
                            if(se�alado.transform.position != se�alado.initialPoint.position)
                            {
                                if (!se�alado.transform.GetChild(0).gameObject.activeSelf) //color del cartucho, no la caja
                                {
                                    se�alado.transform.GetChild(0).gameObject.SetActive(true);
                                    heldObject.gameObject.SetActive(false);
                                    heldObject = null;
                                }
                            }
                            Debug.Log("As� no funciona! Intentalo al rev�s!");
                        }
                        else LeaveItem();
                    }
                }
                else LeaveItem();
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
                        heldObject = null; //se queda en posici�n donde estaba actualmente o no
                    }
                    
                }
                
                else LeaveItem();
                
            }
            else
            {
                LeaveItem();
            }
            
            //poner UI cuando hoverees sobre botones interactuables, donde puedes devolver tu objeto o abrir algo
            //UI semipermanente -> cuando tengas un heldObject se activar� el texto del bot�n con el que puedes droppear el objeto

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
            heldObject = null; //se queda en posici�n donde estaba actualmente o no
        }
        
    }

    /* poner solo si cafetera ha sido tocada por raycast
    void ReloadCoffee()
    {
        if (bulletsLeft < ammoSize)
        {
            StartCoroutine(ReloadRoutine());
        }
    }
    */

    IEnumerator ReloadRoutine()
    {
        Animator mugAnim = mug.gameObject.GetComponent<Animator>();
        mugAnim.SetBool("isFull", false);
        isHoldingCoffee = false;
        reloadingCoffee = true;
        mug.isFull = false;
        //Se llama a la animaci�n de recarga
        //yield return new WaitForSeconds(reloadTime);
        scriptController.energy += 50;
        Debug.Log("Se ha a�adido energ�a");
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
                //No puedo consumir nada!
            }
        }
    }


    #endregion
}

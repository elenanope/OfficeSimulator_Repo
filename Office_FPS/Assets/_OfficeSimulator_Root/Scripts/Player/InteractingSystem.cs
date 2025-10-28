using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractingSystem : MonoBehaviour
{
    #region General Variables
    [Header("General References")]
    [SerializeField] Camera fpsCam; //Ref si disparamos desde el centro de la cam
    [SerializeField] LayerMask impactLayer; //Layer con la que el Raycast interactúa
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

    //Bools de estado
    [SerializeField] bool interacting; //Indica que estamos disparando
    [SerializeField] bool leaving; //Indica que estamos disparando
    [SerializeField] bool canInteract; //Indica que en este momento del juego se puede disparar
     bool isHoldingCoffee; //Indica si podemos recargar
     bool reloadingCoffee;

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
                    //primero que pongas la taza y caiga el liquido, después ya recharge

                }
                else
                {
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
                    if(hit.transform.TryGetComponent(out TipoObjeto señalado))
                    {
                        if (equipado.mainPart)
                        {
                            if (equipado.objectType == señalado.objectType)
                            {
                                if (!heldObject.transform.GetChild(0).gameObject.activeSelf)
                                {
                                    heldObject.transform.GetChild(0).gameObject.SetActive(true);
                                    hit.collider.gameObject.SetActive(false);
                                }
                                else Debug.Log("Ya está lleno!");
                            }
                        }
                        else if(señalado.mainPart)
                        {
                            if(señalado.transform.position != señalado.initialPoint.position)
                            {
                                if (!señalado.transform.GetChild(0).gameObject.activeSelf) //color del cartucho, no la caja
                                {
                                    señalado.transform.GetChild(0).gameObject.SetActive(true);
                                    heldObject.gameObject.SetActive(false);
                                    heldObject = null;
                                }
                            }
                            Debug.Log("Así no funciona! Intentalo al revés!");
                        }
                        else LeaveItem();
                    }
                }
                else LeaveItem();
            }
            else if (Physics.Raycast(fpsCam.transform.position, direction, out hit, range) && heldObject.TryGetComponent(out TipoObjeto equipado))
            {
                if (hit.transform.name == "SM_Printer")
                {
                    Debug.Log("Impresora tocada");
                    if (heldObject.TryGetComponent(out Rigidbody rb))
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                    }
                    heldObject.GetComponent<Collider>().enabled = true;
                    heldObject.transform.position = equipado.initialPoint.position;
                    heldObject.transform.rotation = equipado.initialPoint.rotation;
                    heldObject.transform.parent = null;
                    heldObject = null; //se queda en posición donde estaba actualmente o no
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
            //AQUI PUEDO CODEAR TODOS LOS EFECTOS QUE QUIERO PARA MI INTERACCIÓN
            Debug.Log(hit.collider.name);
            Vector3 dropPosition = hit.point + hit.normal * 0.1f;
            heldObject.transform.position = dropPosition;
            Debug.Log(hit.point);
            //if (hit.collider.CompareTag("Walls")) heldObject.transform.position = new Vector3(hit.transform.position.x, hit.transform.position.y, hit.transform.position.z);
            //else heldObject.transform.position = new Vector3 (hit.transform.position.x, hit.transform.position.y + 1, hit.transform.position.z);
            if (heldObject.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                Debug.Log("Rigidbody restaured!");
            }
            heldObject.GetComponent<Collider>().enabled = true;
            heldObject.transform.parent = null;
            heldObject = null; //se queda en posición donde estaba actualmente o no
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
        isHoldingCoffee = false;
        reloadingCoffee = true;
        //Se llama a la animación de recarga
        yield return new WaitForSeconds(reloadTime);
        scriptController.energy += 50;
        if (scriptController.energy > 100) scriptController.energy = 100;
        reloadingCoffee = false;
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

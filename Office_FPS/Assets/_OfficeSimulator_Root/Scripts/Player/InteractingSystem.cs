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
    

    [Header("Weapon Parameters")]
    [SerializeField] float range = 4f;
    [SerializeField] float interactingCooldown = 0.1f; //Tiempo entre disparos
    [SerializeField] float reloadTime = 1.5f; //Tiempo entre disparos
    [SerializeField] bool allowButtonHold = false; //Si se dispara click a click o por mantener


    [Header("Feedback References")]
    [SerializeField] GameObject impactEffect; //Referencia al VFX de impacto de bala
    [SerializeField] GameObject heldObject = null;
    [SerializeField] Transform holdingPoint;

    //Bools de estado
    [SerializeField] bool interacting; //Indica que estamos disparando
    [SerializeField] bool leaving; //Indica que estamos disparando
    [SerializeField] bool canInteract; //Indica que en este momento del juego se puede disparar
     bool reloadingCoffee; //Indica si estamos en proceso de recarga

    #endregion

    private void Awake()
    {
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
            /*
            if (leaving)
            {
                if (heldObject != null) LeaveItem();
                else
                {
                    leaving = false;
                    Debug.Log("You don't have anything to leave");
                }
                    
            }*/
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
        Vector3 direction = fpsCam.transform.forward;
        if (heldObject == null)
        {
            //Physics.Raycast(Origen del rayo, dirección, almacén de info de impacto, longitud del rayo, layer a la que impacta (opcional)
            if (Physics.Raycast(fpsCam.transform.position, direction, out hit, range, impactLayer))
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
        else
        { 
            if (Physics.Raycast(fpsCam.transform.position, direction, out hit, range, impactLayer))
            {
                //CartuchosImpresora
                if (heldObject.TryGetComponent(out TipoCartucho cartucho0))
                {
                    if(hit.transform.TryGetComponent(out TipoCartucho cartucho))
                    {
                        if (cartucho0.mainPart)
                        {
                            if (cartucho0.cartridgeColour == cartucho.cartridgeColour)
                            {
                                if (!heldObject.transform.GetChild(0).gameObject.activeSelf)
                                {
                                    heldObject.transform.GetChild(0).gameObject.SetActive(true);
                                    hit.collider.gameObject.SetActive(false);
                                }
                                else Debug.Log("Ya está lleno!");
                            }
                        }
                        else if(cartucho.mainPart)
                        {
                            if(cartucho.transform.position != cartucho.initialPoint.position)
                            {
                                if (!cartucho.transform.GetChild(0).gameObject.activeSelf) //color del cartucho, no la caja
                                {
                                    cartucho.transform.GetChild(0).gameObject.SetActive(true);
                                    heldObject.gameObject.SetActive(false);
                                    heldObject = null;
                                }
                            }
                            Debug.Log("Así no funciona! Intentalo al revés!");
                        }
                        else
                        {
                            LeaveItem();
                        }
                    }
                }
            }
            else if (Physics.Raycast(fpsCam.transform.position, direction, out hit, range) && heldObject.TryGetComponent(out TipoCartucho cajaCartucho1))
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
                    heldObject.transform.position = cajaCartucho1.initialPoint.position;
                    heldObject.transform.rotation = cajaCartucho1.initialPoint.rotation;
                    heldObject.transform.parent = null;
                    heldObject = null; //se queda en posición donde estaba actualmente o no
                }
                else
                {
                    LeaveItem();
                }
            }
            else
            {
                LeaveItem();
            }
            
            //lanzas un raycast, capa interactuable? si si y encaja con el objeto que estás sosteniendo, el impactado se apagará y se encenderá el hijo del heldobject
            //despues ya no podrás despertar ningún hijo más, solo droppear el objeto o hacer interactuar en el sitio al que pertenezca y se meterá ahí

            //hacer que si con la caja de cartucho le das a la impresora lo devuelva donde estaba: mismo nombre? o en un array en la impresora tienen putnos empty con el mismo orden que TipoCartucho

            //poner UI cuando hoverees sobre botones interactuables, donde puedes devolver tu objeto o abrir algo
            //UI semipermanente -> cuando tengas un heldObject se activará el texto del botón con el que puedes droppear el objeto

            /*if (Physics.Raycast(fpsCam.transform.position, direction, out hit, range, impactLayer))
            {
                //AQUI PUEDO CODEAR TODOS LOS EFECTOS QUE QUIERO PARA MI INTERACCIÓN
                Debug.Log(hit.collider.name);
                heldObject = hit.collider.gameObject;
                heldObject.transform.SetParent(fpsCam.transform);
                heldObject.GetComponent<Collider>().enabled = false;
                heldObject.transform.position = holdingPoint.position;
            }*/
            //Interactuar con el objeto que tengas -> heldObject es cajon para cartucho, y haces click en un cartucho, se recarga
            //no puedes intereactuar más con ese y después le vuelves a dar donde estaba y se mete en su sitio
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
        reloadingCoffee = true;
        //Se llama a la animación de recarga
        yield return new WaitForSeconds(reloadTime);
        reloadingCoffee = false;
    }

    #region Input Methods
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (allowButtonHold)
        {
            interacting = ctx.ReadValueAsButton();
        }
        else
        {
            if (ctx.performed) interacting = true;
        }
    }
    public void OnLeaveItem(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) leaving = true;
    }


    #endregion
}

using System.Collections;
using NUnit.Framework.Internal.Commands;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FPSController : MonoBehaviour
{
    #region General Variables
    public float energy = 100;
    [SerializeField] float secondsOfEnergy = 180f;//3 o 4 minutos
    

    [Header("Movement & Look")]
    [SerializeField] GameObject camHolder; //Ref en inspector al objeto a rotar
    [SerializeField] float speed = 5f;
    [SerializeField] float sprintSpeed = 8f;
    [SerializeField] float maxForce = 1f; //Fuerza máxima de aceleración
    [SerializeField] float sensitivity = 0.1f;

    bool isBlinking;

    #endregion

    //Object References
    Rigidbody playerRb;
    [SerializeField]Animator anim;

    //Input Variables
    Vector2 moveInput;
    Vector2 lookInput;
    float lookRotation;
    [SerializeField] GameManager gameManager;
    [SerializeField] Image energyBarFill;
    [SerializeField] GameObject blinkingPanel;
    [SerializeField] Animator animBody;
    private void Awake()
    {
        playerRb = GetComponent<Rigidbody>();
        blinkingPanel.SetActive(false);
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Lock cursor
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        energy -= Time.deltaTime * (10f / 18f); //comprobar: que se vaya agotando la energía
        energyBarFill.fillAmount = energy / 100;
        if(energy <= 0 || gameManager.strikes >=3)
        {
            //Método de perder
            //Animación de blinking y se escucha un golpe en el suelo (thump)
            Debug.Log("Game over!!");
        }
        else if (gameManager.points > 10)
        {
            Debug.Log("Win!!");
        }
        //Debug ray: visible only in Scene
        Debug.DrawRay(camHolder.transform.position, camHolder.transform.forward * 100f, Color.red);
        if(energy <=20 && energy > 10 && !isBlinking)
        {
            blinkingPanel.SetActive(true);
            //StartCoroutine(Blinking(2));
        }
        else if(energy <=10 && !isBlinking)
        {
            //StopCoroutine(Blinking(2));
            //StartCoroutine(Blinking(1));
        }
        else if (isBlinking && energy > 20) blinkingPanel.SetActive(false);
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void LateUpdate()
    {
        CameraLook();
    }

    void Movement()
    {
        Vector3 currentVelocity = playerRb.linearVelocity;
        Vector3 targetVelocity = new Vector3(moveInput.x, 0, moveInput.y);
        targetVelocity *= speed;
        
        //Convertir la dirección local en global
        targetVelocity = transform.TransformDirection(targetVelocity);

        // Calcular el cambio de velocidad (aceleración)
        Vector3 velocityChange = (targetVelocity - currentVelocity);
        velocityChange = new Vector3(velocityChange.x, 0, velocityChange.z);
        velocityChange = Vector3.ClampMagnitude(velocityChange, maxForce);
        if(moveInput.x > 0 || moveInput.y > 0)
        {
            anim.SetBool("isWalking", true);
            animBody.SetBool("isWalking", true);
        }
        else
        {
            anim.SetBool("isWalking", false);
            animBody.SetBool("isWalking", false);
        }
        //Aplicar la fuerza de movimiento
        playerRb.AddForce(velocityChange, ForceMode.VelocityChange);
    }


    void CameraLook()
    {
        //Horizontal rotation (player body)
        transform.Rotate(Vector3.up * lookInput.x * sensitivity);
        //Vertical rotation (camera)
        lookRotation += (-lookInput.y * sensitivity);
        lookRotation = Mathf.Clamp(lookRotation, -90, 60);
        camHolder.transform.localEulerAngles = new Vector3(lookRotation, 0f, 0f);
    }
    IEnumerator Blinking(int secondsInBetween)
    {
        isBlinking = true;
        //animación parpadeo
        yield return new WaitForSeconds(secondsInBetween);
        isBlinking = false;
        yield return null;
    }

    #region Input Methods
    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }

    #endregion
}

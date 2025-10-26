using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class NPCAIBase : MonoBehaviour
{
    #region General Variables
    [Header("AI Configuration")]
    [SerializeField] NavMeshAgent agent; //Ref al componente "cerebro" del agente
    [SerializeField] int patience;

    [Header("Patroling Stats")]
    [SerializeField] bool walkPointSet;
    [SerializeField] bool arrived;
    [SerializeField] bool readyToGo;

    [Header("Interacting Stats")]
    [SerializeField] float timeLimit = 5f; //Tiempo entre ataque y ataque
    [SerializeField] GameObject heldObject; //Ref al prefab del proyectil
    [SerializeField] Transform holdPoint; //Ref a la posición desde la que se dispara
    

    [Header("States & Detection")]
    [SerializeField] int actualState= 0; //0 working, 1 eating, 2 going out, 3secretary ask
    [SerializeField] Transform[] destinations;
    [SerializeField] int lastDestination;
    [SerializeField] LayerMask npcLayer;

    [Header("Stuck Detection")]
    [SerializeField] float stuckCheckTime = 2f; //Tiempo que el agente espera para comprobar si está stuck
    [SerializeField] float stuckThreshold = 0.5f; //Margen de detección de stuck
    [SerializeField] float maxStuckDuration = 3f; //Tiempo máximo de estar stuck

    float stuckTimer; //Reloj que cuenta el tiempo de estar stuck
    float lastCheckTime; //Tiempo de chequeo previo de stuck
    Vector3 lastPosition; //Posición del último walkpoint perseguido
    Animator animator;
    Rigidbody rb;
    #endregion
    Vector3 destination;
    bool interrupted;


    //poner que tengan variables de hunger y que se vaya restando, así irán con tiempos regulados, tmb ciertos descansos/coffee breaks y hablar con otros hasta que estén de frente a ellos (raycasts)


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        lastPosition = transform.position;
        lastCheckTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        NPCStateUpdater();
        CheckIfStuck();
        if (!arrived) animator.SetBool("isWalking", true);
        else animator.SetBool("isWalking", false);
    }

    void NPCStateUpdater()
    {
        //juntar raycast con parar el agente, guardar y seguir la dirección inicial y cuando no tenga nada delante, resetearlo
        if(!arrived)
        {
            RaycastHit hit;
            float range = 2f;
            
            int randomDestination = 0;
            if (!walkPointSet)
            {
                randomDestination = Random.Range(0, destinations.Length);
                if(randomDestination == lastDestination) randomDestination = Random.Range(0, destinations.Length);
                else
                {
                    agent.SetDestination(destinations[randomDestination].position);
                    lastDestination = randomDestination;
                    walkPointSet = true;
                }
                
            }
            else
            {
                Debug.DrawRay(holdPoint.transform.position, transform.forward * 2f, Color.yellow);
                if (Physics.Raycast(holdPoint.transform.position, transform.forward, out hit, range, npcLayer))
                {
                    Debug.Log(hit.collider.name);
                    interrupted = true;
                    StartCoroutine(TimeInLocationRoutine());
                }
                //destination = new Vector3(destinations[randomDestination].position.x, transform.position.y, destinations[randomDestination].position.z);
                
                Debug.Log("X" + Mathf.Abs(transform.position.x - destinations[lastDestination].position.x) + " Z"+ Mathf.Abs(transform.position.z - destinations[lastDestination].position.z));
                if (Mathf.Abs(transform.position.x - destinations[lastDestination].position.x) < 1f) //Mathf.Abs para que siempre sea positivo
                {
                    if ((Mathf.Abs(transform.position.z - destinations[lastDestination].position.z) < 1f)) StartCoroutine(TimeInLocationRoutine());
                }
            }
        }
        
    }

    IEnumerator TimeInLocationRoutine()
    {
        arrived = true;
        if(lastDestination == 2 && !interrupted)
        {
            animator.SetBool("isSitting", true);
            transform.position = destinations[lastDestination].position;
            transform.rotation = destinations[lastDestination].rotation;
            agent.SetDestination(transform.position);
            rb.isKinematic = true;
            //transform.position = new Vector3(transform.position.x, 0.179f + 0.09f, transform.position.z);
            yield return new WaitForSeconds(Random.Range(5, 11));
            animator.SetBool("isSitting", false);
        } //else if de que si es interrupted que alguno de los dos rote o deje pasar al otro
        else
        {
            agent.SetDestination(transform.position);
            rb.isKinematic = true;
            int cooldown = (int)Random.Range(1, timeLimit);
            yield return new WaitForSeconds(cooldown);
        }
        rb.isKinematic = false;
        arrived = false;
        walkPointSet = false;
        interrupted = false;
    }

    void SearchWalkPoint()
    {
    }


    void CheckIfStuck()
    {
        //Acción que se encarga de revisar si el agente está atrapado
        if (Time.time - lastCheckTime > stuckCheckTime)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);

            if (distanceMoved < stuckThreshold && agent.hasPath)
            {
                stuckTimer += stuckCheckTime;
            }
            else
            {
                stuckTimer = 0;
            }

            if (stuckTimer >= maxStuckDuration)
            {
                walkPointSet = false;
                agent.ResetPath();
                stuckTimer = 0f;
            }

            lastPosition = transform.position;
            lastCheckTime = Time.time;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying) return; //Determinar que esto solo se ejecute en el editor de Unity

        Gizmos.color = Color.red; 
        Gizmos.DrawSphere(destination, 1);
    }
}


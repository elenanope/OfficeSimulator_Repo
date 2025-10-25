using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class NPCAIBase : MonoBehaviour
{
    #region General Variables
    [Header("AI Configuration")]
    [SerializeField] NavMeshAgent agent; //Ref al componente "cerebro" del agente

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

    [Header("Stuck Detection")]
    [SerializeField] float stuckCheckTime = 2f; //Tiempo que el agente espera para comprobar si está stuck
    [SerializeField] float stuckThreshold = 0.1f; //Margen de detección de stuck
    [SerializeField] float maxStuckDuration = 3f; //Tiempo máximo de estar stuck

    float stuckTimer; //Reloj que cuenta el tiempo de estar stuck
    float lastCheckTime; //Tiempo de chequeo previo de stuck
    Vector3 lastPosition; //Posición del último walkpoint perseguido
    #endregion

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        lastPosition = transform.position;
        lastCheckTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        NPCStateUpdater();
        CheckIfStuck();
    }

    void NPCStateUpdater()
    {
        if(!arrived)
        {
            int randomDestination = 0;
            if (!walkPointSet)
            {
                randomDestination = Random.Range(0, destinations.Length);
                agent.SetDestination(destinations[randomDestination].position);
                walkPointSet = true;
            }
            else
            {
                
                Vector3 destination = new Vector3(destinations[randomDestination].position.x, transform.position.y, destinations[randomDestination].position.z);
                Debug.Log(Vector3.Distance(transform.position, destination));
                if (Vector3.Distance(transform.position, destination) < 1f)
                {
                    StartCoroutine(TimeInLocationRoutine());
                }
            }
        }
        
    }

    IEnumerator TimeInLocationRoutine()
    {
        arrived = true;
        float cooldown = Random.Range(0, timeLimit);
        yield return new WaitForSeconds(cooldown);

        walkPointSet = false;
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
        //Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}


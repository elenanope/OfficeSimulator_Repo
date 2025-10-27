using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class NPCAIBase : MonoBehaviour
{
    #region General Variables
    [Header("AI Configuration")]
    [SerializeField] NavMeshAgent agent; //Ref al componente "cerebro" del agente
    [SerializeField] int patience = 100;
    [SerializeField] int maxPatience = 100;

    [Header("NPC Needs")]
    //estás 5 condiciones las podría poner en un array
    //valores random al principio de la partida, así no van a la vez
    //TESTEARLO
    //no volver a contar el tiempo de esa actividad hasta que vuelvan de eso
    //Ej.: se elige hunger (50), se pone automáticamente a 100, se realiza la acción y el tiempo de espera y antes de salir del método se settea a 0 de nuevo
    [SerializeField] int hunger = 0; // = a bit slower than socialization, 7 min?
    [SerializeField] int workDone = 0; //kind of slow, 4 min
    [SerializeField] int needForBreak = 0; // faster, 5-6 min?
    [SerializeField] int secretaryNeed = 0; // = need for break, 6 min
    [SerializeField] int socializationNeed = 0; // = a bit slower than break, 3 min

    [Header("Patroling Stats")]
    [SerializeField] bool walkPointSet;
    [SerializeField] bool goalSet;
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
    GameManager gameManager; //Referencia a Scriptable Object?
    Vector3 destination;

    bool interrupted;
    bool satAtWorkdesk;
    bool waiting;


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
        if (!arrived) animator.SetBool("isWalking", true); //retocar esto para casos específicos
        else animator.SetBool("isWalking", false);
    }

    void NPCStateUpdater()
    {
        //juntar raycast con parar el agente, guardar y seguir la dirección inicial y cuando no tenga nada delante, resetearlo
        if(!arrived)
        {
            RaycastHit hit;
            float range = 2f;
            
            if (!goalSet)
            {
                if(workDone > 90)//o que cuando llegue a 100 tiene que hacer otra actividad y las otras necesidades subirán +20/+40  por ejemplo
                {
                    //Si cualquiera de ellos es mayor que 95 irás a ese, si más de uno es mayor que 95, random entre esos
                    //Sino, todo random
                }
                else
                {
                    if(gameManager.working < 7) //si hay muy poca gente trabajando actualmente
                    {
                        //sigues trabajando -> MomentaryWork()
                    }

                    else
                    {
                        //Se mantiene sentado (satAtWorkdesk = true)
                        //if no hay nadie en secretaria, se incluye VisitSecretary()
                        //Elegir entre Eat(), VisitSecretary(), TakeBreak(), Socialize()
                    }
                }
            }
            else if(goalSet && walkPointSet)
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

    #region Different States
    void Eat()
    {
        gameManager.working = false;
        if(satAtWorkdesk) //o todas las sillas de la cafetería están llenas
        {
            //si tienes hambre sacas la comida a tu mesa
        }
        else
        {
            //te manda a una silla de la cafetería
            //25-40 segundos?
        }
    }

    void VisitSecretary()
    {
        gameManager.working = false;
        //te manda a la ventana de secretaría y pides algo a través de otro script que gestiona el diálogo y los objetos de oficina que spawnees
        // los objetos se quedarán en secretaría, se encienden y apagan ahí (un npc te pide algo, se encienden objetos separados o de cierta manera predeterminada, se lo das
        // si es lo que quería, se vuelve a apagar, se resetea sus posiciones y el npc se va como si nada)
    }

    void TakeBreak()
    {
        gameManager.working = false;
        if (satAtWorkdesk)
        {
            //si necesitas un descanso, te duermes en la mesa
        }
        else
        {
            //te manda a un punto fuera del edificio (o se van en ascensor que tiene un collider para ti)
            //40-90 segundos?
        }
    }

    void Socialize()
    {
        gameManager.working = false;
        if (satAtWorkdesk)
        {
            //si quieres socializar, llamas por telefono a otro que esté en su mesa
            //Animación de llamar por teléfono a alguien random de la oficina (al que llames, se considerará que si está trabajando?)
        }
        else
        {
            //te manda a un punto fuera del edificio (o se van en ascensor que tiene un collider para ti)
            //15-35 segundos?
        }
    }

    void MomentaryWork()
    {
        if (waiting)
        {
            //Hacer IEnumerator?
            //walkPointSet = true, pero que se quede en el sitio
            //Trabaja durante 20 segundos y vuelve a checkear si puede irse
            //walkPointSet = false
        }
        else
        {
            //Ponerse a trabajar
            gameManager.working = true;
            //30-45 segundos trabajando
        }

    }
    #endregion

    #region Movement Handler
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
    #endregion

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying) return; //Determinar que esto solo se ejecute en el editor de Unity

        Gizmos.color = Color.red; 
        Gizmos.DrawSphere(destination, 1);
    }
}


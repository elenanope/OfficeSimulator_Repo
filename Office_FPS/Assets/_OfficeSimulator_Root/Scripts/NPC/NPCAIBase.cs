using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class NPCAIBase : MonoBehaviour
{
    #region General Variables
    [Header("AI Configuration")]
    [SerializeField] NavMeshAgent agent; //Ref al componente "cerebro" del agente
    [SerializeField] NavMeshObstacle obstacle; //Ref al componente "cerebro" del agente
    [SerializeField] int patience = 100;
    [SerializeField] int maxPatience = 100;
    
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
    [SerializeField]GameManager gameManager; //Referencia a Scriptable Object?
    Vector3 destination;
    [SerializeField]Transform[] seatsCafeteria;
    
    bool willSatAtWorkdesk;
    public bool favourDone;//cuando se realice la tarea de la secretaría
    public bool hasAsked;//cuando se realice la tarea de la secretaría
    bool canAskYou;//cuando llegue a secretaría
    int seatNumber;
    float timePassed = -10;
    [SerializeField] int activityToDo = -1;
    [SerializeField] int lastActivity = -1;
    [SerializeField] int favourAsked = -1; //0 Grapados, 1 papeles impresos, 2 Acreditaciones, 3 triturar, 4 clasificado, 5 papelera
    int timesWorked;
    float stateUpdateTimer = 0f;
    float stateUpdateInterval = 0.5f;
    bool isBusy = false;
    public NPCAIBase npcScript;
    [SerializeField] InteractingSystem interactingSystem;
    //poner que tengan variables de hunger y que se vaya restando, así irán con tiempos regulados, tmb ciertos descansos/coffee breaks y hablar con otros hasta que estén de frente a ellos (raycasts)


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        obstacle = GetComponent<NavMeshObstacle>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        lastPosition = transform.position;
        lastCheckTime = Time.time;
        gameManager.workingPeople = 0;
        gameManager.someoneInSecretary = false;
        npcScript = GetComponent<NPCAIBase>();
        gameManager.seatsFree = 5; //o los que haya
        gameManager.strikes = 0;
    }
    private void Start()
    {
        patience = maxPatience;
        willSatAtWorkdesk = true;
        goalSet = true;
        Work();
    }
    void Update()
    {
        if(favourDone) //comprobar si después de esto se envian más npcs ahí
        {
            favourDone = false;
            hasAsked = false;
            arrived = false;
            patience = maxPatience;
            lastActivity = 4;
            //apagar todos los objetos que había mostrado
            gameManager.someoneInSecretary = false;
            Work();
        }
        timePassed += Time.deltaTime;
        if(timePassed >= 10)
        {
            timePassed = 0;
            if (hasAsked)
            {
                patience -= 10; //por ejemplo
            }
            if(patience <= 0)
            {
                gameManager.strikes += 1;
                //animacion sad
                favourDone = true;
            }
        }
        //aumentar variables de stats
        stateUpdateTimer += Time.deltaTime;
        if (stateUpdateTimer >= stateUpdateInterval)
        {
            NPCStateUpdater();
            stateUpdateTimer = 0f;
        }
        //CheckIfStuck();
        if (!arrived) animator.SetBool("isWalking", true); //retocar esto para casos específicos
        else animator.SetBool("isWalking", false);
    }

    void NPCStateUpdater()
    {
        if (!arrived)
        {
            if (!goalSet)
            {
                if (gameManager.workingPeople < 2) Work();
                else ChooseActivity();
                
            }
            else if (goalSet && walkPointSet)
            {
                if(activityToDo == 4)
                {
                    if (Mathf.Abs(transform.position.x - destinations[4].position.x) < 0.5f) //Mathf.Abs para que siempre sea positivo
                    {
                        if ((Mathf.Abs(transform.position.z - destinations[4].position.z) < 0.5f)) StartCoroutine(TimeInLocationRoutine());
                    }
                }
                else if(willSatAtWorkdesk)
                {
                    if (Mathf.Abs(transform.position.x - destinations[0].position.x) < 0.5f)
                    {
                        if ((Mathf.Abs(transform.position.z - destinations[0].position.z) < 0.5f)) StartCoroutine(TimeInLocationRoutine());
                    }
                }
                else if(activityToDo == 1 && !willSatAtWorkdesk)
                {
                    if (Mathf.Abs(transform.position.x - seatsCafeteria[seatNumber].position.x) < 0.5f)
                    {
                        if ((Mathf.Abs(transform.position.z - seatsCafeteria[seatNumber].position.z) < 0.5f)) StartCoroutine(TimeInLocationRoutine());
                    }
                    Debug.Log(name + " is my name.Trying to take the seat " + seatNumber);
                }
            }
        }
        //juntar raycast con parar el agente, guardar y seguir la dirección inicial y cuando no tenga nada delante, resetearlo
    }
    void ChooseActivity()
    {
        int randomActivity = Random.Range(0, 101);
        goalSet = true;
        if(lastActivity == 1 && !willSatAtWorkdesk)
        {
            gameManager.seatsFree++;
        }
        
        if(!gameManager.someoneInSecretary) VisitSecretary();
        else
        {
            if (timesWorked >= 3 && gameManager.seatsFree >= 0)
            {
                timesWorked = 0;
                willSatAtWorkdesk = false;
                Eat();
            }
            else
            {
                willSatAtWorkdesk = true;
                if (randomActivity < 60) Work();//40
                else if (randomActivity < 75) Eat();//15
                else if (randomActivity < 90) Socialize();//15
                else Sleep();
            }
        }
    }
    #region Different States
    void Work()//aparecen de repente en otros sitios
    {
        activityToDo = 0;
        willSatAtWorkdesk = true;
        timesWorked++;
        ResetActivityStatus();
        if (lastActivity == 0) walkPointSet = false;
        else
        {
            gameManager.workingPeople++;
            walkPointSet = true;
            ReturnToSeat();
        }
        goalSet = true;
        StartCoroutine(ActivityDuration(2, 11));
    }
    void Eat()
    {
        activityToDo = 1;
        //willSatAtWorkdesk = true;
        ResetActivityStatus();
        
        if(willSatAtWorkdesk)
        {
            if (lastActivity != 0)
            {
                gameManager.workingPeople++;
                ReturnToSeat();
            }
        }
        else
        {
            gameManager.workingPeople--;
            animator.SetBool("isSitting", false);
            goalSet = true;
            walkPointSet = true;
            //agent.SetDestination(destinations[activityToDo].position);
            agent.SetDestination(seatsCafeteria[gameManager.seatsFree].position);
            seatNumber = gameManager.seatsFree;
            gameManager.seatsFree--;
        }
        //Animación de comer
        StartCoroutine(ActivityDuration(7, 15));
    }
    void Socialize()
    {
        activityToDo = 2;
        willSatAtWorkdesk = true;
        ResetActivityStatus();
        //if (lastActivity == 0) gameManager.workingPeople--;

        if (lastActivity != 0)
        {
            ReturnToSeat();
        }
        //Animación de llamar por teléfono
        StartCoroutine(ActivityDuration(2, 5));
    }
    void Sleep()
    {
        activityToDo = 3;
        willSatAtWorkdesk = true;
        ResetActivityStatus();
        //if (lastActivity == 0) gameManager.workingPeople--;

        if (lastActivity != 0)
        {
            ReturnToSeat();
        }
        //Animación de dormir
        StartCoroutine(ActivityDuration(2, 5));
    }
    void VisitSecretary()
    {
        //no se pq inGame se pone a activityToDo = 1 cuando está delante de secretario
        activityToDo = 4;
        willSatAtWorkdesk = false;
        animator.SetBool("isSitting", false);
        goalSet = true;
        walkPointSet = true;
        ResetActivityStatus();
        if (lastActivity == 0) gameManager.workingPeople--;
        gameManager.someoneInSecretary = true;
        agent.SetDestination(destinations[activityToDo].position);
        interactingSystem.npcAtFrontDesk = this.gameObject.GetComponent<NPCAIBase>();
        //se elige actividad entre grapar, triturar, fotocopiar, imprimir, clasificar documentos, NO NPC, PERO SI OTROS: JEFE -> vaciar la papelera, VISITANTES -> acreditaciones  etc.?
        //se activa la paciencia y en el update, si la condicion de favourDone == true apagará el objeto, animación feliz y se irá a trabajar
    }
    //te manda a la ventana de secretaría y pides algo a través de otro script que gestiona el diálogo y los objetos de oficina que spawnees
    // los objetos se quedarán en secretaría, se encienden y apagan ahí (un npc te pide algo, se encienden objetos separados o de cierta manera predeterminada, se lo das
    // si es lo que quería, se vuelve a apagar, se resetea sus posiciones y el npc se va como si nada)

    #endregion
    public void AskForFavour()//Esto se llama al interactuar con el NPC
    {
        if (activityToDo == 4)
        {
            Debug.Log("Se pregunta un favor");
            bool favourChosen = false;
            if (!favourChosen)
            {
                favourChosen = true;
                favourAsked = Random.Range(0, 6);
                Debug.Log("se elige la tarea" + favourAsked);
                hasAsked = true;//esto por ahora aqui
                //Elegir petición actividad entre grapar, triturar, fotocopiar, imprimir, clasificar documentos
                if(favourAsked == 0 || favourAsked == 3 || favourAsked == 5)//0 Grapados, 1 papeles impresos, 2 Acreditaciones, 3 triturar, 4 clasificado, 5 papelera
                {
                    //Te spawnean varias hojas random
                }
                else if(favourAsked == 4)
                {
                    //Te spawnean varias hojas con colores distintos
                }
            }
            else
            {
                if (canAskYou)
                {
                    if (!hasAsked) hasAsked = true;
                    Debug.Log("te dice tarea");
                    //Enseñar diálogo con petición
                }
            }
        }
    }
    public void Receive(TipoObjeto handedObject, int taskDone)
    {
        if(hasAsked && favourAsked!= -1)
        {
            if(handedObject != null)
            {
                
                if (favourAsked == handedObject.activityDone)
                {
                    favourDone = true;
                    favourAsked = -1;
                    lastActivity = 4;
                    hasAsked = false;
                    handedObject.gameObject.SetActive(false);
                    handedObject.gameObject.transform.parent = null;
                    if(handedObject.activityDone == 0)
                    {
                        handedObject.gameObject.transform.GetChild(0).parent = null; //si son folios grapados se separan
                    }
                    Debug.Log("Te han dado lo correcto");
                }
                else
                {
                    Debug.Log("Yo no he pedido esto");
                }
            }
            else
            {
                if(taskDone != -1)
                {
                    if (taskDone == favourAsked)
                    {
                        favourDone = true;
                        favourAsked = -1;
                        hasAsked = false;
                        lastActivity = activityToDo;
                        Debug.Log("Han hecho algo, y lo has recibido");
                    }
                    else Debug.Log("Han hecho algo, pero no coincide");
                }
            }
        }
    }
    void ReturnToSeat()
    {
        agent.SetDestination(destinations[0].position); //para que se siente en su mesa
        animator.SetBool("isSitting", false);
    }
    IEnumerator ActivityDuration(int min, int max)
    {
        //if (isBusy) yield break;
        //isBusy = true;
        if ((walkPointSet && arrived)||!walkPointSet)
        {
            animator.SetBool("isWalking", false);
            if(willSatAtWorkdesk || (!willSatAtWorkdesk && activityToDo == 1)) animator.SetBool("isSitting", true);
            agent.enabled = false;
            obstacle.enabled = true;
            //poner aquí animaciones de cada lugar??
            if (activityToDo == 0)
            {
                transform.position = destinations[activityToDo].position;
                transform.rotation = destinations[activityToDo].rotation; // no va bien
            }
            else if (activityToDo == 1 && !willSatAtWorkdesk)
            {
                transform.position = seatsCafeteria[seatNumber].position;
                transform.rotation = seatsCafeteria[seatNumber].rotation;
            }
            else if (activityToDo == 4)
            {
                transform.position = destinations[activityToDo].position;
                transform.rotation = destinations[activityToDo].rotation; //no del todo
            }
            //animator.SetBool("isSitting", true);
            rb.isKinematic = true;

            float timeToSpend = Random.Range(min, max +1);
            yield return new WaitForSeconds(timeToSpend);

            if (activityToDo == 4)
            {
                gameManager.someoneInSecretary = false;
            }
            if (willSatAtWorkdesk) lastActivity = 0;
            else lastActivity = activityToDo;
            goalSet = false;
            arrived = false;
            walkPointSet = false;

            isBusy = false;
            yield break;
        }
        else
        {
            yield return new WaitForSeconds(Random.Range(1f, 2f));
            //isBusy = false;
            StartCoroutine(ActivityDuration(min, max)); //Va a crear problemas??
        }
    }
    void ResetActivityStatus()
    {
        if((lastActivity != 0 && willSatAtWorkdesk) || (lastActivity == 0 && !willSatAtWorkdesk))
        {
            Debug.Log("Resetting Status");
            obstacle.enabled = false;
            agent.enabled = true;
            rb.isKinematic = false;
        }
    }

    #region Movement Handler
    IEnumerator TimeInLocationRoutine()
    {
        arrived = true;
        if(activityToDo == 4)
        {
            canAskYou = true;
        }
        yield break;
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


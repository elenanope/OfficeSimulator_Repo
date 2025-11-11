using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class BossAI : MonoBehaviour
{
    #region Variables
    [Header("AI Configuration")]
    [SerializeField] NavMeshAgent agent; //Ref al componente "cerebro" del agente
    [SerializeField] NavMeshObstacle obstacle; //Ref al componente "cerebro" del agente
    [SerializeField] int patience = 30;
    [SerializeField] int maxPatience = 30;

    [Header("Patroling Stats")]
    [SerializeField] bool walkPointSet;
    [SerializeField] bool goalSet;
    [SerializeField] bool arrived;

    [Header("States & Detection")]
    [SerializeField] bool willSatAtWorkdesk;
    [SerializeField] Transform[] destinations;
    [SerializeField] LayerMask npcLayer;
    [SerializeField] GameObject[] objectsToUse;//sushi para comer, etc.
    bool isInsideOffice;
    public bool favourDone;//cuando se realice la tarea de la secretaría
    public bool hasAsked;//cuando se realice la tarea de la secretaría
    bool canAskYou;//cuando llegue a secretaría
    bool favourChosen = false;
    int seatNumber;
    float timePassed = -10;
    [SerializeField] int activityToDo = -1;
    [SerializeField] int lastActivity = -1;
    [SerializeField] int favourAsked = -1; //0 Grapados, 1 papeles impresos, 2 Acreditaciones, 3 triturar, 4 clasificado, 5 papelera
    int timesWorked;
    bool isBusy = false;

    float stateUpdateTimer = 0f;
    float stateUpdateInterval = 0.5f;

    [Header("Stuck Detection")]
    [SerializeField] float stuckCheckTime = 2f; //Tiempo que el agente espera para comprobar si está stuck
    [SerializeField] float stuckThreshold = 0.5f; //Margen de detección de stuck
    [SerializeField] float maxStuckDuration = 3f; //Tiempo máximo de estar stuck

    float stuckTimer; //Reloj que cuenta el tiempo de estar stuck
    float lastCheckTime; //Tiempo de chequeo previo de stuck
    Vector3 lastPosition; //Posición del último walkpoint perseguido

    Animator animator;
    Rigidbody rb;
    [Header("Other References")]
    [SerializeField] GameManager gameManager;
    [SerializeField] InteractingSystem interactingSystem;
    [SerializeField] FrontDeskManager frontDesk;
    [SerializeField] VisitorAI visitor;
    #endregion
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
        gameManager.strikes = 0;
        gameManager.points = 0;
        Time.timeScale = 0;
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
        if (favourDone) //comprobar si después de esto se envian más npcs ahí
        {
            favourDone = false;
            favourChosen = false;
            hasAsked = false;
            arrived = false;
            patience = maxPatience;
            lastActivity = 4;
            //apagar todos los objetos que había mostrado
            gameManager.someoneInSecretary = false;
            Debug.Log("Vuelta, caso 1");
            Work();
        }
        timePassed += Time.deltaTime;
        if (timePassed >= 10)
        {
            timePassed = 0;
            if (hasAsked)
            {
                patience -= 10; //por ejemplo
            }
            if (patience <= 0)
            {
                gameManager.strikes ++;
                gameManager.points--;
                Debug.Log("PAciencia acabada");
                frontDesk.ResetObjects();
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
                Debug.Log("Aqui aqui");
                if (timesWorked < 12) Work();
                else ChooseActivity();

            }
            else if (goalSet && walkPointSet)
            {
                if (activityToDo == 4)
                {
                    if (Mathf.Abs(transform.position.x - destinations[4].position.x) < 0.5f) //Mathf.Abs para que siempre sea positivo
                    {
                        if ((Mathf.Abs(transform.position.z - destinations[4].position.z) < 0.5f)) StartCoroutine(TimeInLocationRoutine());
                    }
                }
                else if (willSatAtWorkdesk)
                {
                    if (Mathf.Abs(transform.position.x - destinations[0].position.x) < 0.5f)
                    {
                        if ((Mathf.Abs(transform.position.z - destinations[0].position.z) < 0.5f)) StartCoroutine(TimeInLocationRoutine());
                    }
                }
            }
        }
    }
    void ChooseActivity()
    {
        int randomActivity = Random.Range(0, 101);
        goalSet = true;
        Debug.Log("elijo");
        if (!gameManager.someoneInSecretary) VisitSecretary();
        else
        {
            gameManager.bossInQueue = true;
            willSatAtWorkdesk = true;
            if (randomActivity < 60) Work();//40
            else if (randomActivity < 75) Eat();//15
            else if (randomActivity < 90) Socialize();//15
            else Sleep();
        }
    }
    #region Different States
    void Work()//aparecen de repente en otros sitios
    {
        activityToDo = 0;
        willSatAtWorkdesk = true;
        timesWorked++;
        if (lastActivity == 0) walkPointSet = false;
        else
        {
            gameManager.workingPeople++;
            walkPointSet = true;
            goalSet = true;

            ResetActivityStatus();
            ReturnToSeat();
        }
        StartCoroutine(ActivityDuration(9, 25));
    }
    void Eat()
    {
        activityToDo = 1;
        willSatAtWorkdesk = true;

        if (lastActivity != 0)
        {
            gameManager.workingPeople++;
            walkPointSet = true;
            goalSet = true;

            ResetActivityStatus();
            ReturnToSeat();
        }
        StartCoroutine(ActivityDuration(8, 20));
    }
    void Socialize()
    {
        activityToDo = 2;
        willSatAtWorkdesk = true;

        if (lastActivity != 0)
        {
            walkPointSet = true;
            ResetActivityStatus();
            ReturnToSeat();
        }
        //Animación de llamar por teléfono
        StartCoroutine(ActivityDuration(2, 5));
    }
    void Sleep()
    {
        activityToDo = 3;
        willSatAtWorkdesk = true;

        if (lastActivity != 0)
        {
            walkPointSet = true;
            ResetActivityStatus();
            ReturnToSeat();
        }
        //Animación de dormir
        StartCoroutine(ActivityDuration(2, 5));
    }
    void VisitSecretary()
    {
        activityToDo = 4;
        willSatAtWorkdesk = false;
        animator.SetBool("isSitting", false);
        goalSet = true;
        walkPointSet = true;
        ResetActivityStatus();
        if (lastActivity == 0) gameManager.workingPeople--;
        gameManager.someoneInSecretary = true;
        gameManager.bossInQueue = false;
        agent.SetDestination(destinations[activityToDo].position);
        interactingSystem.bossAtFrontDesk = this.gameObject.GetComponent<BossAI>();
        
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
            goalSet = true;
            if (!favourChosen)
            {
                favourChosen = true;
                bool alPrincipio = Random.Range(0, 2) == 0;
                if (alPrincipio) favourAsked = Random.Range(0, 2);
                else favourAsked = Random.Range(3, 5);
                Debug.Log("se elige la tarea" + favourAsked);
                hasAsked = true;//esto por ahora aqui
                //Elegir petición actividad entre grapar, triturar, fotocopiar, imprimir, clasificar documentos
                frontDesk.PrepareObjects(favourAsked);
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
        Debug.Log("Jefe recibe");
        if (hasAsked && favourAsked != -1)
        {
            if (handedObject != null)
            {
                if (favourAsked != 1 && favourAsked != 0 && favourAsked == handedObject.activityDone)
                {
                    gameManager.points++;
                    Debug.Log("Te han dado lo correcto, caso 1");
                }
                else if (favourAsked == 1 && favourAsked == handedObject.activityDone && handedObject.activityDone == 1)
                {
                    gameManager.points++;
                    Debug.Log("Te han dado lo correcto, caso 2");
                }
                else if (favourAsked == 0 && favourAsked == handedObject.activityDone && handedObject.activityDone == 0)
                {
                    gameManager.points++;
                    Debug.Log("Te han dado lo correcto, caso 3");
                }
                else
                {
                    gameManager.strikes++;
                    gameManager.points--;
                    Debug.Log("Yo no he pedido esto");
                    //quitar de que sea el hijo del player y poner strike? o quitar que sean hijos y volver a poner el transform.position como al spawnear
                }
                favourDone = true;
                favourChosen = false;
                favourAsked = -1;
                lastActivity = 4;
                hasAsked = false;
                handedObject.gameObject.SetActive(false);
                handedObject.gameObject.transform.parent = null;
                if (handedObject.activityDone == 0)
                {
                    handedObject.gameObject.transform.GetChild(0).gameObject.SetActive(false);
                    handedObject.gameObject.transform.GetChild(0).parent = null; //si son folios grapados se separan
                }
                interactingSystem.heldObject = null;
                interactingSystem.bossAtFrontDesk = null;
                frontDesk.ResetObjects();
                
            }
            else
            {
                if (taskDone != -1)
                {
                    if (taskDone == favourAsked)
                    {
                        gameManager.points++;
                        Debug.Log("Han hecho algo, y lo has recibido");
                    }
                    else
                    {
                        gameManager.strikes++;
                        gameManager.points--;
                        Debug.Log("Han hecho algo, pero no coincide");
                    }
                    favourDone = true; 
                    favourChosen = false;
                    favourAsked = -1;
                    hasAsked = false;
                    lastActivity = activityToDo;
                    frontDesk.ResetObjects();
                }
            }
        }
    }
    void ReturnToSeat()
    {
        Debug.Log("Jefe vuelve");
        agent.SetDestination(destinations[0].position); //para que se siente en su mesa
        animator.SetBool("isSitting", false);
    }
    IEnumerator ActivityDuration(int min, int max)
    {
        Debug.Log("Jefe resetea actividad");
        if (isBusy) yield break;
        isBusy = true;
        int lunchToday = 0;
        if (activityToDo > -1)
        {
            if (walkPointSet)
            {
                while (!arrived)
                    yield return null;
            }
            if ((walkPointSet && arrived) || !walkPointSet)
            {
                animator.SetBool("isWalking", false);
                if (willSatAtWorkdesk || (!willSatAtWorkdesk && activityToDo == 1)) animator.SetBool("isSitting", true);
                agent.enabled = false;
                obstacle.enabled = true;
                if (activityToDo == 0)
                {
                    transform.position = destinations[activityToDo].position;
                    transform.rotation = destinations[activityToDo].rotation;
                }
                else if (activityToDo == 4)
                {
                    transform.position = destinations[activityToDo].position;
                    transform.rotation = destinations[activityToDo].rotation;
                }
                else if (willSatAtWorkdesk)
                {
                    transform.position = destinations[0].position;
                    transform.rotation = destinations[0].rotation;
                    if (activityToDo == 1)
                    {
                        //poner aquí animación
                        lunchToday = Random.Range(0, 2);
                        objectsToUse[lunchToday].SetActive(true);
                    }
                }
                rb.isKinematic = true;

                float timeToSpend = Random.Range(min, max + 1);
                yield return new WaitForSeconds(timeToSpend);
                if (activityToDo == 4) gameManager.someoneInSecretary = false;
                else if (activityToDo == 5) visitor.meetingState = 3;

                if (willSatAtWorkdesk)
                {
                    lastActivity = 0;
                    if (activityToDo == 1) objectsToUse[lunchToday].SetActive(false); // + quitar aquí animación
                }
                else lastActivity = activityToDo;
                activityToDo = -1; //para que no se marque como que ha llegado de nuevo
                goalSet = false;
                walkPointSet = false;
                isBusy = false;
                arrived = false;
                yield break;
            }
        }
    }
    void ResetActivityStatus()
    {
        if ((lastActivity != 0 && willSatAtWorkdesk) || (lastActivity == 0 && !willSatAtWorkdesk) || lastActivity == 1)
        {
            obstacle.enabled = false;
            agent.enabled = true;
        }
    }

    IEnumerator TimeInLocationRoutine()
    {
        arrived = true;
        if (activityToDo == 4)
        {
            canAskYou = true;
        }
        else if(activityToDo == 5)
        {
            visitor.meetingState = 2;
        }
        yield break;
    }

    public void HeadBackToOffice()
    {
        Debug.Log("Soy llamado a caaasa");
        StopAllCoroutines();
        if(!willSatAtWorkdesk)
        {
            if(activityToDo == 4)
            {
                favourDone = true;
                favourChosen = false;
                favourAsked = -1;
                hasAsked = false;
                lastActivity = activityToDo;
                frontDesk.ResetObjects();
                Debug.Log("Perdon, llego tarde a un meeting");
            }
            agent.SetDestination(destinations[0].position);
            walkPointSet = true;
            willSatAtWorkdesk = true;
            activityToDo = 5;
        }

        willSatAtWorkdesk = true;
        activityToDo = 5;
        StartCoroutine(ActivityDuration(30, 40));
    }
    //hacer otro script para ontriggerenter en las puertas y que si te acercas por cualquiera de los laods siendo jefe o visitor o player (si no está locked) se abran las puertas
    //(se cierran cuando ontriggerexit)
}

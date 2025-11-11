using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class NPCAIBase : MonoBehaviour
{
    #region Variables
    [Header("AI Configuration")]
    [SerializeField] NavMeshAgent agent; //Ref al componente "cerebro" del agente
    [SerializeField] NavMeshObstacle obstacle; //Ref al componente "cerebro" del agente
    [SerializeField] int patience = 60;
    [SerializeField] int maxPatience = 60;
    
    [Header("Patroling Stats")]
    [SerializeField] bool walkPointSet;
    [SerializeField] bool goalSet;
    [SerializeField] bool arrived;

    [Header("States & Detection")]
    [SerializeField] bool willSatAtWorkdesk;
    [SerializeField] Transform[] destinations;
    [SerializeField] Transform[] seatsCafeteria;
    [SerializeField] LayerMask npcLayer;
    [SerializeField] GameObject[] objectsToUse;//sushi para comer, etc.
    public bool favourDone;//cuando se realice la tarea de la secretaría
    public bool hasAsked;//cuando se realice la tarea de la secretaría
    bool canAskYou;//cuando llegue a secretaría
    bool favourChosen = false;
    int seatNumber;
    float timePassed = -10;
    [SerializeField] int activityToDo = -1;
    [SerializeField] int lastActivity = -1;
    [SerializeField] int favourAsked = -1; //0 Grapados, 1 papeles impresos, 2 Acreditaciones, 3 triturar/papelera, 4 clasificado,// 5 papelera
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
    [SerializeField]GameManager gameManager;
    [SerializeField] InteractingSystem interactingSystem;
    [SerializeField] FrontDeskManager frontDesk;
    public NPCAIBase npcScript;
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
        npcScript = GetComponent<NPCAIBase>();
        gameManager.seatFree[0] = true;
        gameManager.seatFree[1] = true;
        gameManager.seatFree[2] = true;
        gameManager.seatFree[3] = true;
        gameManager.seatFree[4] = true;
        gameManager.seatFree[5] = true;
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
                gameManager.strikes ++;
                gameManager.points--;
                frontDesk.Dialogue(7);
                StartCoroutine(TimeInLocationRoutine(2));
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
                if (gameManager.workingPeople < 5) Work();
                else ChooseActivity();
                
            }
            else if (goalSet && walkPointSet)
            {
                if(activityToDo == 4)
                {
                    if (Mathf.Abs(transform.position.x - destinations[4].position.x) < 0.5f) //Mathf.Abs para que siempre sea positivo
                    {
                        if ((Mathf.Abs(transform.position.z - destinations[4].position.z) < 0.5f)) StartCoroutine(TimeInLocationRoutine(0));
                    }
                }
                else if(willSatAtWorkdesk)
                {
                    if (Mathf.Abs(transform.position.x - destinations[0].position.x) < 0.5f)
                    {
                        if ((Mathf.Abs(transform.position.z - destinations[0].position.z) < 0.5f)) StartCoroutine(TimeInLocationRoutine(0));
                    }
                }
                else if(activityToDo == 1 && !willSatAtWorkdesk)
                {
                    if (Mathf.Abs(transform.position.x - seatsCafeteria[seatNumber].position.x) < 0.5f)
                    {
                        if ((Mathf.Abs(transform.position.z - seatsCafeteria[seatNumber].position.z) < 0.5f)) StartCoroutine(TimeInLocationRoutine(0));
                    }
                }
            }
        }
        //juntar raycast con parar el agente, guardar y seguir la dirección inicial y cuando no tenga nada delante, resetearlo
    }
    void ChooseActivity()
    {
        bool seatsFree = false;
        int randomActivity = Random.Range(0, 101);
        for (int i = 0; i < 6; i++)
        {
            if (gameManager.seatFree[i])
            {
                seatsFree = true;
                break;
            }
        }
        goalSet = true;
        
        if(!gameManager.someoneInSecretary && (!gameManager.bossInQueue && !gameManager.visitorInQueue)) VisitSecretary();
        else
        {
            if (timesWorked >= 3)
            {
                if(seatsFree)
                {
                    timesWorked = 0;
                    willSatAtWorkdesk = false;
                    Eat();
                }
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
        if (lastActivity == 0) walkPointSet = false;
        else
        {
            gameManager.workingPeople++;
            walkPointSet = true;

            ResetActivityStatus();
            ReturnToSeat();
        }
        goalSet = true;
        StartCoroutine(ActivityDuration(2, 11));
    }
    void Eat()
    {
        activityToDo = 1;
        //willSatAtWorkdesk = true;
        
        if(willSatAtWorkdesk)
        {
            if (lastActivity != 0)
            {
                gameManager.workingPeople++;
                walkPointSet = true;
                goalSet = true;

                ResetActivityStatus();
                ReturnToSeat();
            }
        }
        else
        {
            if(lastActivity != 1)
            {
                gameManager.workingPeople--;
                animator.SetBool("isSitting", false);
                goalSet = true;
                walkPointSet = true;
                //agent.SetDestination(destinations[activityToDo].position);
                for (int i = 0; i < 6; i++)
                {
                    if (gameManager.seatFree[i] == true)
                    {
                        seatNumber = i;
                        gameManager.seatFree[i] = false;
                        break;
                    }
                }

                ResetActivityStatus();
                agent.SetDestination(seatsCafeteria[seatNumber].position);
            }
        }
        //Animación de comer
        StartCoroutine(ActivityDuration(7, 15));
    }
    void Socialize()
    {
        activityToDo = 2;
        willSatAtWorkdesk = true;
        //if (lastActivity == 0) gameManager.workingPeople--;

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
        //if (lastActivity == 0) gameManager.workingPeople--;

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
            
            if (!favourChosen)
            {
                favourChosen = true;
                bool alPrincipio = Random.Range(0, 2) == 0;
                if(alPrincipio)favourAsked = Random.Range(0, 2);
                else favourAsked = Random.Range(3, 5);
                Debug.Log("se elige la tarea" + favourAsked);
                hasAsked = true;//esto por ahora aqui
                                //Elegir petición actividad entre grapar, triturar, fotocopiar, imprimir, clasificar documentos

                frontDesk.objectsSet = false;
                frontDesk.StartActivity(favourAsked);
            }
            else
            {
                if (canAskYou)
                {
                    if (!hasAsked) hasAsked = true;
                    frontDesk.StartActivity(favourAsked);
                    if(favourAsked == -1)
                    {
                        favourChosen = false;
                    }
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
                if (favourAsked != 1 && favourAsked != 0 && favourAsked == handedObject.activityDone)
                {
                    gameManager.points++;
                    frontDesk.Dialogue(5);
                    Debug.Log("Te han dado lo correcto, caso 1");
                }
                else if (favourAsked == 1 && favourAsked == handedObject.activityDone && handedObject.activityDone == 1)
                {
                    gameManager.points++;
                    frontDesk.Dialogue(5);
                    Debug.Log("Te han dado lo correcto, caso 2");
                }
                else if (favourAsked == 0 && favourAsked == handedObject.activityDone && handedObject.activityDone == 0)
                {
                    gameManager.points++;
                    frontDesk.Dialogue(5);
                    Debug.Log("Te han dado lo correcto, caso 3");
                }
                else
                {
                    gameManager.strikes++;
                    gameManager.points--;
                    frontDesk.Dialogue(8);
                    Debug.Log("Yo no he pedido esto");
                    //quitar de que sea el hijo del player y poner strike? o quitar que sean hijos y volver a poner el transform.position como al spawnear
                }
                StartCoroutine(TimeInLocationRoutine(1));
                handedObject.gameObject.SetActive(false);
                handedObject.gameObject.transform.parent = null;
                if (handedObject.activityDone == 0)
                {
                    handedObject.gameObject.transform.GetChild(0).gameObject.SetActive(false);
                    handedObject.gameObject.transform.GetChild(0).parent = null; //si son folios grapados se separan
                }
                
            }
            else
            {
                if(taskDone != -1)
                {
                    if (taskDone == favourAsked)
                    {
                        gameManager.points++;
                        frontDesk.Dialogue(5);
                        Debug.Log("Han hecho algo, y lo has recibido");
                    }
                    else
                    {
                        gameManager.strikes++;
                        gameManager.points--;
                        frontDesk.Dialogue(8);
                        Debug.Log("Han hecho algo, pero no coincide");
                    }
                    StartCoroutine(TimeInLocationRoutine(1));
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
                    lunchToday = Random.Range(0, 2);
                    objectsToUse[lunchToday].SetActive(true); //que estén siempre enfrente
                }
                else if (activityToDo == 4)
                {
                    transform.position = destinations[activityToDo].position;
                    transform.rotation = destinations[activityToDo].rotation; //no del todo
                }
                else if (willSatAtWorkdesk)
                {
                    transform.position = destinations[0].position;
                    transform.rotation = destinations[0].rotation; //no del todo
                }
                //animator.SetBool("isSitting", true);
                rb.isKinematic = true;

                float timeToSpend = Random.Range(min, max + 1);
                yield return new WaitForSeconds(timeToSpend);
                if (activityToDo == 4)
                {
                    gameManager.someoneInSecretary = false;
                }
                else if (activityToDo == 1 && !willSatAtWorkdesk)
                {
                    gameManager.seatFree[seatNumber] = true;
                    objectsToUse[lunchToday].SetActive(false); //que estén siempre enfrente
                }
                if (willSatAtWorkdesk) lastActivity = 0;
                else lastActivity = activityToDo;
                activityToDo = -1; //para que no se marque como que ha llegado de nuevo
                goalSet = false;
                walkPointSet = false;
                arrived = false;

                isBusy = false;
                yield break;
            }
        }
        
    }
    void ResetActivityStatus()
    {
        if((lastActivity != 0 && willSatAtWorkdesk) || (lastActivity == 0 && !willSatAtWorkdesk) || lastActivity == 1)
        {
            obstacle.enabled = false;
            agent.enabled = true;
            //rb.isKinematic = false;
        }
    }

    IEnumerator TimeInLocationRoutine(int caseNumber)
    {
        if(caseNumber == 0)
        {
            arrived = true;
            if (activityToDo == 4)
            {
                canAskYou = true;
            }
        }
        else if (caseNumber == 1)
        {
            yield return new WaitForSeconds(2);
            favourDone = true;
            favourChosen = false;
            favourAsked = -1;
            hasAsked = false;
            lastActivity = activityToDo;
            interactingSystem.heldObject = null;
            interactingSystem.bossAtFrontDesk = null;
            frontDesk.ResetObjects();
        }
        else
        {
            yield return new WaitForSeconds(2);
            frontDesk.ResetObjects();
            //animacion sad
            favourDone = true;
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
    

}


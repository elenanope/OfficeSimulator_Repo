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
    bool interrupted;
    bool satAtWorkdesk;
    bool waiting;
    bool firstTask = true;
    public bool favourDone;//cuando se realice la tarea de la secretaría
    int seatNumber;
    float timePassed = -10;
    [SerializeField] int activityToDo = -1;
    [SerializeField] int lastActivity = -1;

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
        gameManager.seatsFree = 6; //o los que haya
        gameManager.strikes = 0;
        gameManager.seats.Clear();
        gameManager.seats.Add(0);
        gameManager.seats.Add(1);
        gameManager.seats.Add(2);
        gameManager.seats.Add(3);
        gameManager.seats.Add(4);
        gameManager.seats.Add(5);
    }
    private void Start()
    {
        hunger = Random.Range(0, 100);
        workDone = Random.Range(0, 100);
        needForBreak = Random.Range(0, 100);
        secretaryNeed = Random.Range(0, 100);
        socializationNeed = Random.Range(0, 100);
    }
    // Update is called once per frame
    void Update()
    {
        timePassed += Time.deltaTime;
        if(timePassed >= 10)
        {
            timePassed = 0;
            Debug.Log("Stats added!");
            hunger += 10;
            socializationNeed += 10;
            needForBreak += 10;
            secretaryNeed += 20;
            //si la paciencia esta a 0, cambias de meta y se marca un strike
        }
        //aumentar variables de stats
        NPCStateUpdater();
        //CheckIfStuck();
        if (!arrived) animator.SetBool("isWalking", true); //retocar esto para casos específicos
        else animator.SetBool("isWalking", false);
    }

    void NPCStateUpdater()
    {
        if(firstTask) //así todos trabajan al inicio y no se ralla el contador
        {
            firstTask = false;
            waiting = true;
            MomentaryWork();
        }
        if (workDone >= 100)
        {
            Debug.Log("Has trabajado mucho, toma un bonus");
            hunger += 10;
            needForBreak += 40;
            socializationNeed += 30;
            //secretaría??
            //hay que poner que se resetee a 0 en algun momento
        }
        //juntar raycast con parar el agente, guardar y seguir la dirección inicial y cuando no tenga nada delante, resetearlo
        if(!arrived)
        {
            RaycastHit hit;
            float range = 2f;
            
            if (!goalSet)
            {
                if(obstacle.enabled) obstacle.enabled = false;//echarle un ojo
                if(!agent.enabled) agent.enabled = true;
                if (workDone >= 70)//si ya has trabajado mucho
                {
                    if (gameManager.workingPeople < 7) //si hay muy poca gente trabajando actualmente
                    {
                        MomentaryWork();
                        waiting = true;
                    }
                    else
                    {
                        satAtWorkdesk = false;
                        if (!gameManager.someoneInSecretary)
                        {
                            //Debug.Log("Se incluye visitar la secretaría");
                            ChooseActivity(30, 20, 20, 10);
                            //activityToDo = Random.Range(0, 5);
                        }
                        else
                        {
                            //Debug.Log("No se incluye visitar la secretaría");
                            ChooseActivity(40, 20, 20, 21);//activityToDo = Random.Range(0, 4);
                        }
                    }
                }
                else
                {
                    if(gameManager.workingPeople < 5) //si hay muy poca gente trabajando de verdad
                    {
                        MomentaryWork();
                        waiting = true;
                    }
                    else //puedes hacer otras actividades desde tu sitio sentado
                    {
                        if (secretaryNeed >= 70 && !gameManager.someoneInSecretary)
                        {
                            VisitSecretary();
                        }
                        else if(hunger >= 80)
                        {
                            Eat();
                        }
                        else if (needForBreak >= 80)
                        {
                            TakeBreak();
                        }
                        else if (socializationNeed >= 80)
                        {
                            Socialize();
                        }
                        else
                        {
                            if (!gameManager.someoneInSecretary) ChooseActivity(60, 10, 10, 10);
                            else ChooseActivity(70, 10, 10, 11);
                        }
                        
                        //if no hay nadie en secretaria, se incluye VisitSecretary()
                        //Elegir entre Eat(), VisitSecretary(), TakeBreak(), Socialize()
                    }
                    if(secretaryNeed < 70) satAtWorkdesk = true;
                    else satAtWorkdesk = false;
                }
            }
            else if(goalSet && walkPointSet)
            {/*NPC stopper
                Debug.DrawRay(holdPoint.transform.position, transform.forward * 2f, Color.yellow);
                if (Physics.Raycast(holdPoint.transform.position, transform.forward, out hit, range, npcLayer))
                {
                    Debug.Log(hit.collider.name);
                    interrupted = true;
                    StartCoroutine(TimeInLocationRoutine());
                }*/
                //destination = new Vector3(destinations[randomDestination].position.x, transform.position.y, destinations[randomDestination].position.z);
                //Debug.Log("Soy " + name + "X" + Mathf.Abs(transform.position.x - destinations[activityToDo].position.x) + " Z" + Mathf.Abs(transform.position.z - destinations[activityToDo].position.z));
                if (!satAtWorkdesk && activityToDo ==1)
                {
                    if (Mathf.Abs(transform.position.x - seatsCafeteria[seatNumber].position.x) < 0.5f) //Mathf.Abs para que siempre sea positivo
                    {
                        if ((Mathf.Abs(transform.position.z - seatsCafeteria[seatNumber].position.z) < 0.5f)) StartCoroutine(TimeInLocationRoutine());
                    }
                    
                }
                else
                {
                    if (Mathf.Abs(transform.position.x - destinations[activityToDo].position.x) < 0.5f) //Mathf.Abs para que siempre sea positivo
                    {
                        if ((Mathf.Abs(transform.position.z - destinations[activityToDo].position.z) < 0.5f)) StartCoroutine(TimeInLocationRoutine());
                    }
                }
            }
        }
    }
    void ChooseActivity(int partWork, int partEat, int partSocial, int partBreak)
    {
        int randomActivity = Random.Range(0, 101);

        //goalSet = true;
        if (randomActivity < partWork) MomentaryWork();//40
        else if (randomActivity < partWork + partEat) Eat();//15
        else if (randomActivity < partWork + partEat + partSocial) Socialize();//15
        else if (randomActivity < partWork + partEat + partSocial + partBreak) TakeBreak();//10
        else VisitSecretary();//20

    }
    #region Different States
    void MomentaryWork()//aparecen de repente en otros sitios
    {
        
        activityToDo = 0;
        if (lastActivity == 0)
        {
            walkPointSet = false;
            satAtWorkdesk = true;
            //Debug.Log("Soy " + name + "Mi última actividad era trabajar, así que ya estoy en el sitio");
        }
        else
        {
            gameManager.workingPeople++;
            satAtWorkdesk = false;
            walkPointSet = true;
            agent.SetDestination(destinations[activityToDo].position);
            //Volver a tu mesa
            //Debug.Log("Soy " + name + "Mi última actividad no era trabajar pero ahora sí, así que vuelvo al sitio");
        }
        if (waiting)
        {
            //Debug.Log("Soy " + name + "Estoy trabajando, pero esperando! Porque mi nivel de trabajo" + workDone);
            //Hacer IEnumerator?
            //walkPointSet = true, pero que se quede en el sitio
            //Trabaja durante 20 segundos y vuelve a checkear si puede irse
            StartCoroutine(ActivityDuration(10, 20));
            //walkPointSet = false
        }
        else
        {
            //Debug.Log("Soy " + name + "Estoy trabajando! Porque mi nivel de trabajo es:" + workDone);
            //Ponerse a trabajar
            //30-45 segundos trabajando
            StartCoroutine(ActivityDuration(30, 45));
        }
        goalSet = true;
    }
    void Eat()
    {
        activityToDo = 1;

        if(lastActivity == 0) gameManager.workingPeople --;
        if(gameManager.seatsFree < 1)
        {
            satAtWorkdesk = true;
        }
        else
        {
            satAtWorkdesk = false;
            gameManager.seatsFree -= 1;
        }
        if(satAtWorkdesk) //o todas las sillas de la cafetería están llenas
        {
            //Debug.Log("Soy " + name + "Estoy comiendo en mi mesa! Porque mis niveles de trabajo y de comer son:" + workDone + ", " + hunger);
            if(lastActivity != 0) agent.SetDestination(destinations[0].position); //para que se siente en su mesa
            //si tienes hambre sacas la comida a tu mesa
        }
        else
        {
            //Debug.Log("Soy " + name + "Estoy comiendo en un lugar! Porque mis niveles de trabajo y de comida son:" + workDone + ", " + hunger);
            walkPointSet = true;
            if (lastActivity != activityToDo)
            {
                int randomNumber;
                randomNumber = Random.Range(0, gameManager.seats.Count);
                seatNumber = gameManager.seats[randomNumber];
                agent.SetDestination(seatsCafeteria[seatNumber].position); //para que vaya a cafetería (distintos puntos para cada asiento)
                gameManager.seats.Remove(seatNumber);//alguno no llega del todo!!
            }
            //te manda a una silla de la cafetería
            //25-40 segundos? ->
        }
        StartCoroutine(ActivityDuration(25, 40)); //pero que esto empiece después de llegar al sitio?
        goalSet = true;
    }
    void Socialize()
    {
        
        activityToDo = 2;
        if (lastActivity == 0) gameManager.workingPeople--;
        if (satAtWorkdesk)
        {
            //Debug.Log("Soy " + name + "Estoy socializando en mi mesa! Porque mis niveles de trabajo y de socializar son:" + workDone + ", " + socializationNeed);
            if (lastActivity != 0) agent.SetDestination(destinations[0].position); //para que se siente en su mesa
            //si quieres socializar, llamas por telefono a otro que esté en su mesa
            //Animación de llamar por teléfono a alguien random de la oficina (al que llames, se considerará que si está trabajando?)
        }
        else
        {
            //Debug.Log("Soy " + name + "Estoy socializando en un lugar! Porque mis niveles de trabajo y de socializar son:" + workDone + ", " + socializationNeed);
            walkPointSet = true;
            if (lastActivity != activityToDo) agent.SetDestination(destinations[activityToDo].position); //punto random de emsa de otro
            //15-35 segundos?
        }
        StartCoroutine(ActivityDuration(15, 35));
        goalSet = true;
    }
    void TakeBreak()
    {
        goalSet = true;
        activityToDo = 3;
        if (lastActivity == 0) gameManager.workingPeople--;

        if (satAtWorkdesk)
        {
            //Debug.Log("Soy " + name +" Descansando en mi mesa! Niveles de trabajo y de descansar son:" + workDone + ", " + needForBreak);
            //si necesitas un descanso, te duermes en la mesa
            if (lastActivity != 0) agent.SetDestination(destinations[0].position); //para que se siente en su mesa
        }
        else
        {
            //Debug.Log("Soy " + name + "Descansando en un lugar! Niveles de trabajo y de descansar son:" + workDone + ", " + needForBreak);
            walkPointSet = true;
            if (lastActivity != activityToDo) agent.SetDestination(destinations[activityToDo].position);//te manda a un punto fuera del edificio (o se van en ascensor que tiene un collider para ti)
            //40-90 segundos?
        }
        StartCoroutine(ActivityDuration(40, 90)); //aqui no se ejecuta??
    }
    void VisitSecretary()
    {
        goalSet = true;
        activityToDo = 4;
        if (lastActivity == 0) gameManager.workingPeople--;
        gameManager.someoneInSecretary = true;
        //Debug.Log("Soy " + name + "Estoy en secretaría! Porque mis niveles de trabajo y de secretaría son:" + workDone + ", " + secretaryNeed);
        walkPointSet = true;
        agent.SetDestination(destinations[activityToDo].position);
        //se elige actividad entre grapar, triturar, fotocopiar, imprimir, etc.?
        //se activa la paciencia
        //en el update, si la condicion de favourDone == true apagará el objeto, animación feliz y se irá a trabajar

        //te manda a la ventana de secretaría y pides algo a través de otro script que gestiona el diálogo y los objetos de oficina que spawnees
        // los objetos se quedarán en secretaría, se encienden y apagan ahí (un npc te pide algo, se encienden objetos separados o de cierta manera predeterminada, se lo das
        // si es lo que quería, se vuelve a apagar, se resetea sus posiciones y el npc se va como si nada)
    }
    #endregion

    IEnumerator ActivityDuration(int min, int max)
    {
        if((walkPointSet && arrived)||!walkPointSet)
        {
            animator.SetBool("isWalking", false);
            agent.enabled = false;
            obstacle.enabled = true;
            //poner aquí animaciones de cada lugar??
            if (activityToDo == 0)
            {
                animator.SetBool("isSitting", true);
                transform.position = destinations[activityToDo].position;
                transform.rotation = destinations[activityToDo].rotation; // no va bien
                //agent.SetDestination(transform.position);
                rb.isKinematic = true;
            }
            else if (activityToDo == 1 && !satAtWorkdesk)
            {
                animator.SetBool("isSitting", true);
                transform.position = seatsCafeteria[seatNumber].position;
                transform.rotation = seatsCafeteria[seatNumber].rotation;
                rb.isKinematic = true;
            }

            float timeToSpend = Random.Range(min, max +1);//yield return new WaitForSeconds(Random.Range(min, max + 1));
            //Debug.Log(name + ", estaré este tiempo en la actividad:" + timeToSpend);
            yield return new WaitForSeconds(timeToSpend);

            if (activityToDo == 0)
            {
                workDone += 20;
                if (workDone >= 100) workDone = 100;
                animator.SetBool("isSitting", false);
            }
            else if (activityToDo == 1)
            {
                hunger -= 80;
                if (hunger < 0) hunger = 0;
                if(!satAtWorkdesk)
                {
                    gameManager.seatsFree += 1;
                    gameManager.seats.Add(seatNumber);
                    animator.SetBool("isSitting", false);
                }
            }
            else if (activityToDo == 2)
            {
                socializationNeed -= 40;
                if (socializationNeed < 0) socializationNeed = 0;
            }
            else if (activityToDo == 3)
            {
                needForBreak -= 50;
                if (needForBreak < 0) needForBreak = 0;
            }
            else if (activityToDo == 4)
            {
                gameManager.someoneInSecretary = false;
                secretaryNeed -= 20;
                if (secretaryNeed < 0) secretaryNeed = 0;
            }
            walkPointSet = false;
            obstacle.enabled = false;
            yield return null;
            agent.enabled = true;
            if (satAtWorkdesk)lastActivity = 0;
            else lastActivity = activityToDo;
            if(waiting) waiting = false;
            goalSet = false;
            rb.isKinematic = false;
            arrived = false;
            interrupted = false;
            yield break;
        }
        else
        {
            yield return new WaitForSeconds(2);
            StartCoroutine(ActivityDuration(min, max)); //Va a crear problemas??
            //Debug.Log("Retry!");
            //yield break;
        }
    }
    

    #region Movement Handler
    IEnumerator TimeInLocationRoutine()
    {
        arrived = true;
        /*if(lastActivity == 2 && !interrupted) //si es un lugar de sentarse
        {}else if de que si es interrupted que alguno de los dos rote o deje pasar al otro
        else*/
        {//Pasar datos al otro IEnumerator!!!!
            agent.SetDestination(transform.position);
            rb.isKinematic = true;
            //int cooldown = (int)Random.Range(1, timeLimit);
            //yield return new WaitForSeconds(cooldown);
            yield return null;
        }
        
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


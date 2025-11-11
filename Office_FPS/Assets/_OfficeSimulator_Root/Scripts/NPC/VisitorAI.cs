using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class VisitorAI : MonoBehaviour
{
    [SerializeField] int cardState = 0; //0 ha llegado alli, 1 la pide, 2 la tiene, 3 la devuelve
    public int meetingState = 0; //0 no ha llegado a la oficina, 1 ha llegado, 2 ha llegado el jefe, 3 ha acabado
    public bool bossArrived;
    [SerializeField] Transform bossOffice;
    [SerializeField] Transform frontDesk;
    [SerializeField] Transform initialPos;
    [SerializeField] BossAI boss;

    #region Variables
    [Header("AI Configuration")]
    [SerializeField] NavMeshAgent agent; //Ref al componente "cerebro" del agente
    [SerializeField] NavMeshObstacle obstacle; //Ref al componente "cerebro" del agente
    [SerializeField] int patience = 50;
    [SerializeField] int maxPatience = 50;

    [Header("Patroling Stats")]
    [SerializeField] bool arrived;
    [SerializeField] bool started;
    [SerializeField] bool hasAsked;

    bool canAskYou;//cuando llegue a secretaría
    float timePassed = -10;
    [SerializeField] int timeUntilNewAsk = 30;

    float stateUpdateTimer = 0f;
    float stateUpdateInterval = 0.5f;


    Animator animator;
    Rigidbody rb;
    [Header("Other References")]
    [SerializeField] GameManager gameManager;
    [SerializeField] InteractingSystem interactingSystem;
    [SerializeField] FrontDeskManager frontDeskManager;
    #endregion
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        obstacle = GetComponent<NavMeshObstacle>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }
    private void Start()
    {
        patience = maxPatience;
        cardState = -1;
        meetingState = 0;
    }
    void Update()
    {
        timePassed += Time.deltaTime;
        if (timePassed >= 5)
        {
            timePassed = 0;
            if((cardState == -1 ||meetingState==3) && !started)
            {
                timeUntilNewAsk -= 10;
            }
            else if (cardState == 1)
            {
                patience -= 10; //por ejemplo
            }
            else if(meetingState == 3 && started)
            {
                ChooseActivity();
            }
            if(timeUntilNewAsk <= 0 && !started)
            {
                ChooseActivity();
            }
            if (patience <= 0)
            {
                gameManager.strikes ++;
                gameManager.points--;
                //animacion sad
                cardState = 3;
                meetingState = 3;
            }
        }
        stateUpdateTimer += Time.deltaTime;
        if (stateUpdateTimer >= stateUpdateInterval)
        {
            NPCStateUpdater();
            stateUpdateTimer = 0f;
        }
        //if (!arrived) animator.SetBool("isWalking", true); //retocar esto para casos específicos
        //else animator.SetBool("isWalking", false);
    }

    void NPCStateUpdater()
    {
        if(!arrived)
        {
            if (cardState == -1 || (cardState == 2 && meetingState == 3))//recoge o devuelve acreditacion
            {
                if (Mathf.Abs(transform.position.x - frontDesk.position.x) < 0.5f) //Mathf.Abs para que siempre sea positivo
                {
                    if ((Mathf.Abs(transform.position.z - frontDesk.position.z) < 0.5f)) StartCoroutine(TimeInLocationRoutine(0));
                }
            }
            else if (cardState == 2 && meetingState == 0)//va a reunion
            {
                if (Mathf.Abs(transform.position.x - bossOffice.position.x) < 0.5f)
                {
                    if ((Mathf.Abs(transform.position.z - bossOffice.position.z) < 0.5f)) StartCoroutine(TimeInLocationRoutine(0));
                }
            }
            else if (cardState == 3 && meetingState == 3)//se va
            {
                if (Mathf.Abs(transform.position.x - initialPos.position.x) < 0.5f)
                {
                    if ((Mathf.Abs(transform.position.z - initialPos.position.z) < 0.5f)) StartCoroutine(TimeInLocationRoutine(0));
                }
            }
        }
        
    }
    void ChooseActivity()
    {
        started = true;
        if(cardState == -1 || meetingState == 3)
        {
            if (!gameManager.someoneInSecretary && !gameManager.bossInQueue) VisitSecretary();
            else
            {
                gameManager.visitorInQueue = true;
                timeUntilNewAsk = 15;
                started = false;
            }
        }
        
    }
    void VisitSecretary()
    {
        animator.SetBool("isSitting", false);
        animator.SetBool("isWalking", true);
        ResetActivityStatus();
        gameManager.someoneInSecretary = true;
        gameManager.visitorInQueue = false;
        agent.SetDestination(frontDesk.position);
        interactingSystem.visitorAtFrontDesk = this.gameObject.GetComponent<VisitorAI>();
    }

    public void AskForCard()//Esto se llama al interactuar con el NPC
    {
        if (canAskYou)
        {
            if (cardState == 0)
            {
                cardState = 1;
                frontDeskManager.objectsSet = false;
                frontDeskManager.StartActivity(2);
                Debug.Log("te dice tarea");
            }
            else if(cardState == 1)
            {
                Debug.Log("te repite la tarea");
                frontDeskManager.StartActivity(2);
                //frontDeskManager.Dialogue(2);
            }
            else if(cardState == 2 && meetingState == 3)
            {

                frontDeskManager.visitorCard.SetActive(true);
                frontDeskManager.visitorCard.GetComponent<Collider>().enabled = true;
                frontDeskManager.visitorCard.GetComponent<Rigidbody>().useGravity = true;
                frontDeskManager.visitorCard.GetComponent<Rigidbody>().isKinematic = false;
                gameManager.someoneInSecretary = false;
                frontDeskManager.visitorCard.transform.position = frontDeskManager.spawnPoint1.position;
                agent.SetDestination(initialPos.position);
                cardState = -1;
                meetingState = 0;
                timeUntilNewAsk = 90;
            }
            //Enseñar diálogo con petición
        }
    }
    public void Receive(TipoObjeto handedObject, int taskDone)
    {
        if (cardState == 1 && meetingState == 0)
        {
            if (handedObject != null)
            {

                if (handedObject.objectType == 5)
                {
                    arrived = false;
                    cardState = 2;
                    patience = maxPatience;

                    handedObject.gameObject.SetActive(false);
                    handedObject.gameObject.transform.parent = null; 
                    StartCoroutine(TimeInLocationRoutine(1));
                    gameManager.points++;
                    frontDeskManager.Dialogue(5);
                    Debug.Log("Te han dado lo correcto");
                }
                else
                {
                    frontDeskManager.Dialogue(6);
                    Debug.Log("Yo no he pedido esto");
                }
            }
            else
            {
                frontDeskManager.Dialogue(6);
                Debug.Log("Eh, que solo quiero una acreditacion");
            }
        }
    }
   IEnumerator WaitInOffice()
    {
        bool isBusy = false;
        if (isBusy) yield break;
        isBusy = true;
        yield return new WaitForSeconds(Random.Range(15, 25));
        meetingState = 3;
        isBusy =false;
    }
    void ResetActivityStatus()
    {
        Debug.Log("Resetting Status");
        obstacle.enabled = false;
        agent.enabled = true;
    }

    IEnumerator TimeInLocationRoutine(int caseNumber)
    {
        if(caseNumber == 0)
        {
            arrived = true;
            if (cardState == -1)
            {
                cardState = 0;
                canAskYou = true;
            }
            else if (cardState == 2 && meetingState == 0)
            {
                meetingState = 1;
                animator.SetBool("isSitting", true);
                boss.HeadBackToOffice();
                StartCoroutine(WaitInOffice());
                yield break;
            }
            else if (cardState == 2 && meetingState == 3) //comprobar que hace esto!!!
            {
                //soltar acreditacion en punto
                frontDeskManager.visitorCard.SetActive(true);
                frontDeskManager.visitorCard.transform.position = frontDeskManager.spawnPoint1.position;
                agent.SetDestination(initialPos.position);
                cardState = -1;
                meetingState = 0;
            }
            yield break;
        }
        else if(caseNumber == 1)
        {

            ResetActivityStatus();
            gameManager.someoneInSecretary = false;
            agent.SetDestination(bossOffice.position);
            animator.SetBool("isWalking", true);
            interactingSystem.heldObject = null;
            interactingSystem.visitorAtFrontDesk = null;
        }

    }
}

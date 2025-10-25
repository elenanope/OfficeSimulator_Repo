using UnityEngine;
using UnityEngine.AI;

public class EnemyAIBase : MonoBehaviour
{
    #region General Variables
    [Header("AI Configuration")]
    [SerializeField] NavMeshAgent agent; //Ref al componente "cerebro" del agente
    [SerializeField] Transform target; //Ref a la posici�n del target a perseguir
    [SerializeField] LayerMask targetLayer; //Ref a la layer de los objetos target
    [SerializeField] LayerMask groundLayer; //Ref a la layer del suelo

    [Header("Patroling Stats")]
    [SerializeField] float walkPointRange = 10f; //Radio m�ximo de generaci�n de puntos a perseguir
    Vector3 walkPoint; //Posici�n del punto random a perseguir
    [SerializeField]bool walkPointSet;
    [SerializeField] Transform[] destinations;

    [Header("Interacting Stats")]
    [SerializeField] float timeBetweenAttacks = 1f; //Tiempo entre ataque y ataque
    [SerializeField] GameObject projectile; //Ref al prefab del proyectil
    [SerializeField] Transform shootPoint; //Ref a la posici�n desde la que se dispara
    [SerializeField] float shootSpeedY; //Fuerza de disparo hacia arriba (solo para catapulta)
    [SerializeField] float shootSpeedZ = 10f; //Fuerza de disparo hacia delante (siempre debe estar)
    bool alreadyAttacked;

    [Header("States & Detection")]
    [SerializeField] float sightRange = 10f; //Radio de persecuci�n
    [SerializeField] float attackRange = 2f; //Radio de ataque
    [SerializeField] bool targetInSightRange; //Bool que determina si se puede perseguir al target
    [SerializeField] bool targetInAttackRange; //Bool que determina si se puede atacar al target

    [Header("Stuck Detection")]
    [SerializeField] float stuckCheckTime = 2f; //Tiempo que el agente espera para comprobar si est� stuck
    [SerializeField] float stuckThreshold = 0.1f; //Margen de detecci�n de stuck
    [SerializeField] float maxStuckDuration = 3f; //Tiempo m�ximo de estar stuck

    float stuckTimer; //Reloj que cuenta el tiempo de estar stuck
    float lastCheckTime; //Tiempo de chequeo previo de stuck
    Vector3 lastPosition; //Posici�n del �ltimo walkpoint perseguido
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
        EnemyStateUpdater();
        CheckIfStuck();
    }

    void EnemyStateUpdater()
    {
        /*//Acci�n que se encarga de actualizar el estado del enemigo

        //Detectar un solo query de f�sicas
        Collider[] hits = Physics.OverlapSphere(transform.position, sightRange, targetLayer);
        targetInSightRange = hits.Length > 0;

        if (targetInSightRange)
        {
            float distance = Vector3.Distance(transform.position, target.position);
            targetInAttackRange = distance <= attackRange;
        }
        else targetInAttackRange = false;

        // Actualziaci�n de los estados de la IA
        if (!targetInSightRange && !targetInAttackRange) 
        {*/
            Patroling();/*
        }
        else if ( targetInSightRange && !targetInAttackRange)
        {
            ChaseTarget();
        }
        /*else if (targetInSightRange && targetInAttackRange)
        {
            AttackTarget();
        }*/
    }

    void Patroling()
    {
        int randomDestination = 0;
        if (!walkPointSet)
        {
            randomDestination = Random.Range(0, destinations.Length);
            agent.SetDestination(destinations[randomDestination].position);
            walkPointSet = true;
        }
        //Distancia calculada mediante operaci�n por magnitud entre vectores
        else 
        {
            if ((transform.position - destinations[randomDestination].position).sqrMagnitude < 1f) walkPointSet = false;
        }
    }

    void SearchWalkPoint()
    {
    }

    void ChaseTarget()
    {
        //Acci�n que se encarga de perseguir al objetivo
        agent.SetDestination(target.position);
    }

    /*void AttackTarget()
    {
        //Acci�n que contiene la l�gica compleja de ataque

        //Se determina que el agente se persiga a s� mismo (estar quieto)
        agent.SetDestination(transform.position);

        //La rotaci�n para mirar al objetivo va a ser suavizada
        //LookAt no es una mala opci�n, pero la rotaci�n es m�s directa
        Vector3 direction = (target.position - transform.position).normalized;
        //Condicional que se pregunta si agente y target no se est�n mirando
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, agent.angularSpeed * Time.deltaTime);
        }

        //Solo atacar� si no se est� atacando
        if (!alreadyAttacked)
        {
            Rigidbody rb = Instantiate(projectile, shootPoint.position, Quaternion.identity).GetComponent<Rigidbody>();
            rb.AddForce(transform.forward * shootSpeedZ, ForceMode.Impulse);
            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    void ResetAttack()
    {
        //Acci�n que resetea el AttackTarget()
        alreadyAttacked = false;
    }*/

    void CheckIfStuck()
    {
        //Acci�n que se encarga de revisar si el agente est� atrapado
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
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }





}

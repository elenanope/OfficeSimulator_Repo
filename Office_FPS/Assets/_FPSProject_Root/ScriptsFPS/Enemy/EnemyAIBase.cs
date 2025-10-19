using UnityEngine;
using UnityEngine.AI;

public class EnemyAIBase : MonoBehaviour
{
    #region General Variables
    [Header("AI Configuration")]
    [SerializeField] NavMeshAgent agent; //Ref al componente "cerebro" del agente
    [SerializeField] Transform target; //Ref a la posición del target a perseguir
    [SerializeField] LayerMask targetLayer; //Ref a la layer de los objetos target
    [SerializeField] LayerMask groundLayer; //Ref a la layer del suelo

    [Header("Patroling Stats")]
    [SerializeField] float walkPointRange = 10f; //Radio máximo de generación de puntos a perseguir
    Vector3 walkPoint; //Posición del punto random a perseguir
    bool walkPointSet;

    [Header("Attacking Stats")]
    [SerializeField] float timeBetweenAttacks = 1f; //Tiempo entre ataque y ataque
    [SerializeField] GameObject projectile; //Ref al prefab del proyectil
    [SerializeField] Transform shootPoint; //Ref a la posición desde la que se dispara
    [SerializeField] float shootSpeedY; //Fuerza de disparo hacia arriba (solo para catapulta)
    [SerializeField] float shootSpeedZ = 10f; //Fuerza de disparo hacia delante (siempre debe estar)
    bool alreadyAttacked;

    [Header("States & Detection")]
    [SerializeField] float sightRange = 10f; //Radio de persecución
    [SerializeField] float attackRange = 2f; //Radio de ataque
    [SerializeField] bool targetInSightRange; //Bool que determina si se puede perseguir al target
    [SerializeField] bool targetInAttackRange; //Bool que determina si se puede atacar al target

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
        target = GameObject.Find("PF_Player").transform;
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
        //Acción que se encarga de actualizar el estado del enemigo

        //Detectar un solo query de físicas
        Collider[] hits = Physics.OverlapSphere(transform.position, sightRange, targetLayer);
        targetInSightRange = hits.Length > 0;

        if (targetInSightRange)
        {
            float distance = Vector3.Distance(transform.position, target.position);
            targetInAttackRange = distance <= attackRange;
        }
        else targetInAttackRange = false;

        // Actualziación de los estados de la IA
        if (!targetInSightRange && !targetInAttackRange) 
        {
            Patroling();
        }
        else if ( targetInSightRange && !targetInAttackRange)
        {
            ChaseTarget();
        }
        else if (targetInSightRange && targetInAttackRange)
        {
            AttackTarget();
        }
    }

    void Patroling()
    {
        //Cuando se ejecute, el enemigo patrulla
        if (!walkPointSet)
        {
            //Si walkpointset es falso, no hay punto a perseguir, entonces debe generarlo
            SearchWalkPoint();
        }
        else agent.SetDestination(walkPoint);

        //Distancia calculada mediante operación por magnitud entre vectores
        if ((transform.position - walkPoint).sqrMagnitude < 1f)
        {
            walkPointSet = false;
        }
    }

    void SearchWalkPoint()
    {
        //Cuando ni persigue ni ataca, busca puntos a patrullar
        int attempts = 0; //Número de intentos de generación de puntos óptimos
        const int maxAttempts = 5;

        while (!walkPointSet && attempts < maxAttempts) 
        {
            attempts++;
            Vector3 randomPoint = transform.position + new Vector3(Random.Range(-walkPointRange, walkPointRange), 0, Random.Range(-walkPointRange, walkPointRange));

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                walkPoint = hit.position; //Determina el punto random a perseguir
                if (Physics.Raycast(walkPoint, -transform.up, 2f, groundLayer))
                {
                    walkPointSet = true;
                }
            }
        }
    }

    void ChaseTarget()
    {
        //Acción que se encarga de perseguir al objetivo
        agent.SetDestination(target.position);
    }

    void AttackTarget()
    {
        //Acción que contiene la lógica compleja de ataque

        //Se determina que el agente se persiga a sí mismo (estar quieto)
        agent.SetDestination(transform.position);

        //La rotación para mirar al objetivo va a ser suavizada
        //LookAt no es una mala opción, pero la rotación es más directa
        Vector3 direction = (target.position - transform.position).normalized;
        //Condicional que se pregunta si agente y target no se están mirando
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, agent.angularSpeed * Time.deltaTime);
        }

        //Solo atacará si no se está atacando
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
        //Acción que resetea el AttackTarget()
        alreadyAttacked = false;
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
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }





}

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GunSystem : MonoBehaviour
{
    #region General Variables
    [Header("General References")]
    [SerializeField] Camera fpsCam; //Ref si disparamos desde el centro de la cam
    [SerializeField] Transform shootPoint; //Ref si queremos disparar desde la punta del ca�on
    [SerializeField] LayerMask impactLayer; //Layer con la que el Raycast interact�a
    RaycastHit hit; //Almac�n de la informaci�n de los objetos con los que impactan los disparos
    

    [Header("Weapon Parameters")]
    [SerializeField] int damage = 10; //Da�o del arma
    [SerializeField] float range = 100f; //Distancia de disparo
    [SerializeField] float spread = 0; //Dispersi�n de disparo
    [SerializeField] float shootingCooldown = 0.2f; //Tiempo entre disparos
    [SerializeField] float reloadTime = 1.5f; //Tiempo entre disparos
    [SerializeField] bool allowButtonHold = false; //Si se dispara click a click o por mantener

    [Header("Bullet Management")]
    [SerializeField] int ammoSize = 30; //Cantidad max de balas por cargador
    [SerializeField] int bulletsPerTap = 1; //Cantidad de balas que se disparan por disparo
    int bulletsLeft; //Cantidad de balas dentro del cargador actual

    [Header("Feedback References")]
    [SerializeField] GameObject impactEffect; //Referencia al VFX de impacto de bala

    //Bools de estado
    bool shooting; //Indica que estamos disparando
    bool canShoot; //Indica que en este momento del juego se puede disparar
    bool reloading; //Indica si estamos en proceso de recarga

    #endregion

    private void Awake()
    {
        bulletsLeft = ammoSize; //Al inicio de la partida, tenemos cargador lleno
        canShoot = true;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        impactEffect.SetActive(false); //Apaga el efecto de impacto al iniciar el juego
    }

    // Update is called once per frame
    void Update()
    {
        if (canShoot && shooting && !reloading && bulletsLeft > 0)
        {
            //Inicializar la corrutina de disparo
            StartCoroutine(ShootRoutine());
        }
    }

    IEnumerator ShootRoutine()
    {
        canShoot = false; //Previene la acumulaci�n por frame de disparos
        if (!allowButtonHold) shooting = false; //Configuraci�n del disparo por tap
        for (int i = 0; i < bulletsPerTap; i++)
        {
            if (bulletsLeft <= 0) break; //Segunda prevenci�n de errores

            Shoot();
            bulletsLeft--;
        }

        yield return new WaitForSeconds(shootingCooldown);
        canShoot = true; //Resetea la posibilidad de disparar
    }

    void Shoot()
    {
        //ESTE ES EL M�TODO M�S IMPORTANTE
        //AQU� SE DEFINE EL DISPARO POR RAYCAST

        //Almacenar la direcci�n del disparo
        Vector3 direction = fpsCam.transform.forward;
        direction.x += Random.Range(-spread, spread); //Esto a�ade una dispersi�n aleatoria en caso de que no valga 0
        direction.y += Random.Range(-spread, spread);

        //DECLARACI�N DEL RAYCAST
        //Physics.Raycast(Origen del rayo, direcci�n, almac�n de info de impacto, longitud del rayo, layer a la que impacta (opcional)
        if (Physics.Raycast(fpsCam.transform.position, direction, out hit, range, impactLayer))
        {
            //AQUI PUEDO CODEAR TODOS LOS EFECTOS QUE QUIERO PARA MI INTERACCI�N
            Debug.Log(hit.collider.name);

            if (hit.collider.TryGetComponent(out EnemyHealth health))
            {
                //COMUNICACI�N ENTRE: OBJETO QUE DISPARA + RAYO + OBJETO QUE RECIBE
                health.TakeDamage(damage);
            }
        }


    }

    void Reload()
    {
        if (bulletsLeft < ammoSize && !reloading)
        {
            StartCoroutine(ReloadRoutine());
        }
    }

    IEnumerator ReloadRoutine()
    {
        reloading = true;
        //Se llama a la animaci�n de recarga
        yield return new WaitForSeconds(reloadTime);
        bulletsLeft = ammoSize;
        reloading = false;
    }

    #region Input Methods
    public void OnShoot(InputAction.CallbackContext ctx)
    {
        if (allowButtonHold)
        {
            shooting = ctx.ReadValueAsButton();
        }
        else
        {
            if (ctx.performed) shooting = true;
        }
    }
    public void OnReload(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) Reload();
    }


    #endregion
}

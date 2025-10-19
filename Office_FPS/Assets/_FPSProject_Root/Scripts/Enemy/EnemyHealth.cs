using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health System Management")]
    [SerializeField] int maxHealth = 100; //Vida máxima del enemigo
    [SerializeField] int health; //Vida actual del enemigo

    [Header("Feedback Configuration")]
    [SerializeField] Material damagedMat; //Material feedback de daño
    Material baseMat; //Material base del enemigo
    MeshRenderer enemyRend; //Referencia al MeshRenderer propio


    private void Awake()
    {
        enemyRend = GetComponent<MeshRenderer>();
        health = maxHealth;
        baseMat = enemyRend.material;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (health <= 0)
        {
            health = 0; //Prevención de valor negativo
            gameObject.SetActive(false); //Apaga el enemigo, "muere"
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        enemyRend.material = damagedMat; //Cambia el material al de feedback de daño
        Invoke(nameof(ResetDamageMat), 0.1f);
    }

    void ResetDamageMat()
    {
        //Devuelve el material base al enemigo una vez pasa la fase visual de feedback de daño
        enemyRend.material = baseMat;
    }
}

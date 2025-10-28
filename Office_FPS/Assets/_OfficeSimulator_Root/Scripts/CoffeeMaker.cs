using UnityEngine;

public class CoffeeMaker : MonoBehaviour
{
    bool hasMug;
    //ESTE OBJETO TENDRÁ EL COLLIDER EN UN BOTÓN, NO EN TODO

    public void PressButton()
    {
        if(hasMug)
        {
            //animación del café
            //cuando acabe la animación se activa el collider en la taza, para que puedas cogerlo
        }
        else
        {
            //poner diálogo de: oh! estaría bien tener una taza...
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Mug"))
        {
            hasMug = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Mug"))
        {
            hasMug = false;
        }
    }
}

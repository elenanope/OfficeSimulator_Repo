using UnityEngine;

public class CoffeeMaker : MonoBehaviour
{
    bool hasMug;
    //ESTE OBJETO TENDR� EL COLLIDER EN UN BOT�N, NO EN TODO

    public void PressButton()
    {
        if(hasMug)
        {
            //animaci�n del caf�
            //cuando acabe la animaci�n se activa el collider en la taza, para que puedas cogerlo
        }
        else
        {
            //poner di�logo de: oh! estar�a bien tener una taza...
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

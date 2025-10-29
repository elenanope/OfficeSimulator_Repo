using System.Collections;
using UnityEngine;

public class CoffeeMaker : MonoBehaviour
{
    public bool hasMug;
    public Mug mug = null;
    //ESTE OBJETO TENDR� EL COLLIDER EN UN BOT�N, NO EN TODO

    public void PressButton()
    {
        if(hasMug && mug!= null)
        {
            if (!mug.isFull) StartCoroutine(PourCoffee());
            else Debug.Log("Pero si ya est� llena!");
        }
        else
        {
            Debug.Log("oh! estar�a bien tener una taza...");
            //poner di�logo de: oh! estar�a bien tener una taza...
        }
    }
    public void AdministrateMug(bool entered, Mug heldMug)
    {
        if(entered)
        {
            hasMug = true;
            mug = heldMug;
        }
        else
        {
            hasMug = false;
            mug = null;
        }
    }
    IEnumerator PourCoffee()
    {
        Collider mugCol = mug.GetComponent<Collider>();
        mugCol.enabled = false;
        //animaci�n + sonido
        Animator mugAnim = mug.gameObject.GetComponent<Animator>();
        mugAnim.SetBool("isFull", true);
        yield return new WaitForSeconds(2);
        //apagar sonido, activar collider taza
        mug.isFull = true;
        mugCol.enabled = true;
        Debug.Log("Ya puedes cogerlo!");
    }
}

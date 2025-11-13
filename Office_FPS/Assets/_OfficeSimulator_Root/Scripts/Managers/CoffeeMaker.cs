using System.Collections;
using UnityEngine;

public class CoffeeMaker : MonoBehaviour
{
    public bool hasMug;
    public Mug mug = null;
    //ESTE OBJETO TENDRÁ EL COLLIDER EN UN BOTÓN, NO EN TODO

    public void PressButton()
    {
        if(hasMug && mug!= null)
        {
            if (!mug.isFull) StartCoroutine(PourCoffee());
            else Debug.Log("Pero si ya está llena!");
        }
        else
        {
            Debug.Log("oh! estaría bien tener una taza...");
            //poner diálogo de: oh! estaría bien tener una taza...
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
        //animación + sonido
        Animator mugAnim = mug.gameObject.GetComponent<Animator>();
        mugAnim.SetBool("isFull", true);
        yield return new WaitForSeconds(11);
        //apagar sonido, activar collider taza
        mug.isFull = true;
        mugCol.enabled = true;
        Debug.Log("Ya puedes cogerlo!");
    }
}

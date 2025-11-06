using UnityEngine;

public class BossAI : MonoBehaviour
{
    bool canAskYou;
    bool isInsideOffice;
    int patience;

    public void AskForFavour()//Esta el mismo en el NPC, hacer en otro script???
        //Esto se llama al interactuar con el NPC
    {
        bool favourChosen = false;
        if (!favourChosen)
        {
            favourChosen = true;
            //Elegir petición actividad entre grapar, triturar, fotocopiar, imprimir, clasificar documentos

        }
        else
        {
            if (canAskYou)
            {
                //Enseñar diálogo con petición

            }
        }
    }
    public void HeadBackToOffice()
    {
        if(!isInsideOffice)
        {
            isInsideOffice = true;
        }
    }
    //hacer otro script para ontriggerenter en las puertas y que si te acercas por cualquiera de los laods siendo jefe o visitor o player (si no está locked) se abran las puertas
    //(se cierran cuando ontriggerexit)
    void FavourCompleted()
    {

    }
}

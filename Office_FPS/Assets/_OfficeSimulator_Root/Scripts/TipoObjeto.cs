using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class TipoObjeto : MonoBehaviour
{
    //public int cartridgeColour = 0; //0 black, 1 yellow, 2 pink, 3 cyan
    public int objectType = 0; //0 black, 1 yellow, 2 pink, 3 cyan, 4 Grapadora, 5 Acreditaciones - (npc), 6 Cafe - taza, 7 trituradora, 8 papelera - bolsa, 9 2/3 bandejas para documentos - papeles,
                               //10 calendario - post its
    public bool mainPart;
    //public bool sticky;
    public Transform initialPoint;

}

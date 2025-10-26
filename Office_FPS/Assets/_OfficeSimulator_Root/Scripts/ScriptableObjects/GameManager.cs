using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameManager", menuName = "Scriptable Objects/GameManager")]
public class GameManager : ScriptableObject
{
    public List<Transform> availableDestinations = new List<Transform>();
    public Dictionary<int, Transform> availablePlaces = new Dictionary<int, Transform>();
    public List<GameObject> workingNPC = new List<GameObject>(); //para que siempre haya algunos esperando y tmb sepan quien puede ir al sitio del otro para hablar

    //añadir lista para que haya un máximo de 5 npcs a la vez por ejmplo moviéndose hasta un punto y que mientras no lo estén, quizá se apague su navmesh?
    //si uno se queda mucho tiempo quieto y no está arrived o a X metros de su meta, si no encuentro mejor manera de solucionarlo:
    //->hacer que se abra un portal, desaparezca hacia abajo y caiga sentado en su puesto de trabajo
    //si eran más de uno, hacerlo en uno y si los otros siguen congelados, hacerlo otra vez y etc.




}

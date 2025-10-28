using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameManager", menuName = "Scriptable Objects/GameManager")]
public class GameManager : ScriptableObject
{
    public List<int> seats;
    //public List<GameObject> workingNPC = new List<GameObject>(); //para que siempre haya algunos esperando y tmb sepan quien puede ir al sitio del otro para hablar
    public int workingPeople;//n�mero para saber cuantos trabajan de verdad actualmente
    public int seatsFree;//n�mero para saber cuantos trabajan de verdad actualmente
    public bool someoneInSecretary;//n�mero para saber cuantos trabajan de verdad actualmente
    public int strikes; //quejas de ti al jefe, si hay 3 a la calle

    //Resetear datos entre encendido/apagado

    //a�adir lista para que haya un m�ximo de 5 npcs a la vez por ejmplo movi�ndose hasta un punto y que mientras no lo est�n, quiz� se apague su navmesh?
    //si uno se queda mucho tiempo quieto y no est� arrived o a X metros de su meta, si no encuentro mejor manera de solucionarlo:
    //->hacer que se abra un portal, desaparezca hacia abajo y caiga sentado en su puesto de trabajo
    //si eran m�s de uno, hacerlo en uno y si los otros siguen congelados, hacerlo otra vez y etc.



}

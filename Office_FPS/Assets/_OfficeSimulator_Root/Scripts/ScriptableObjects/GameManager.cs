using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameManager", menuName = "Scriptable Objects/GameManager")]
public class GameManager : ScriptableObject
{
    //public Transform[] seats;
    public int workingPeople;//número para saber cuantos trabajan de verdad actualmente
    public bool[] seatFree;//número para saber cuantos trabajan de verdad actualmente
    public bool someoneInSecretary;//número para saber cuantos trabajan de verdad actualmente
    public int strikes; //quejas de ti al jefe, si hay 3 a la calle
    public int tasksPoints; //para ganar??
    //public NPCAIBase npcAI;
    //public GameObject npcAtFrontDesk;


    //Resetear datos entre encendido/apagado

    //añadir lista para que haya un máximo de 5 npcs a la vez por ejmplo moviéndose hasta un punto y que mientras no lo estén, quizá se apague su navmesh?
    //si uno se queda mucho tiempo quieto y no está arrived o a X metros de su meta, si no encuentro mejor manera de solucionarlo:
    //->hacer que se abra un portal, desaparezca hacia abajo y caiga sentado en su puesto de trabajo
    //si eran más de uno, hacerlo en uno y si los otros siguen congelados, hacerlo otra vez y etc.



}

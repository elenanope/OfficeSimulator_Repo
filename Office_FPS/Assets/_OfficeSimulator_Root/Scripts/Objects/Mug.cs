using UnityEngine;

public class Mug : MonoBehaviour
{
    public bool isFull;
    [SerializeField] Material emptyMat;
    [SerializeField] Material normalMat;
    //[SerializeField] MeshRenderer
    private void Update()
    {
        //if (isFull) gameObject.GetComponent<MeshRenderer>().material = emptyMat;
        //else
        {
            //gameObject.GetComponent<MeshRenderer>().material = normalMat;
        }
    }
}

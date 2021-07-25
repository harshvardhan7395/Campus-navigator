using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommonScript : MonoBehaviour
{
    public static string dest;
    // Start is called before the first frame update
    public void UpdateDestination(string destination)
    {
        dest = destination;
        Debug.Log("In CommonScript : " + dest);
    }
}

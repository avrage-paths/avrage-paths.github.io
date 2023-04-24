using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugManager : MonoBehaviour
{
    public GameObject debug;
    public bool enableDebug = false;

    void updateRecursively(GameObject o)
    {
        o.SetActive(enableDebug);
        for (int i = 0; i < o.transform.childCount; i++)
            updateRecursively(o.transform.GetChild(i).gameObject);
    }

    void Update()
    {
        updateRecursively(debug);
    }
}

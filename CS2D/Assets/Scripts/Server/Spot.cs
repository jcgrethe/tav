using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spot : MonoBehaviour
{
    private List<Collider> colliders = new List<Collider>();

    // Start is called before the first frame update
    public bool IsSpotable()
    {
        foreach (var col in colliders)
        {
            if (string.Compare(col.gameObject.tag, "Player", StringComparison.Ordinal) == 0)
            {
                return false;
            }
        }

        return true;
    }


    public void OnTriggerEnter (Collider other) {
        if (!colliders.Contains(other)) { colliders.Add(other); }
    }
 
    public void OnTriggerExit (Collider other) {
        colliders.Remove(other);
    }
    
}

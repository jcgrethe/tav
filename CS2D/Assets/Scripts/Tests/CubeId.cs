using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public class CubeId : MonoBehaviour
{
    // Start is called before the first frame update
    private String id;
    
    public string Id
    {
        get => id;
        set => id = value;
    }

}

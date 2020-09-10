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

    private Random random = new Random();
    void Start()
    {
        id = RandomString(10);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public string Id
    {
        get => id;
        set => id = value;
    }


    public string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

}

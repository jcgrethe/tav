using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update

    public String ip;
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.LoadScene("Scene/MainMenu");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    
}

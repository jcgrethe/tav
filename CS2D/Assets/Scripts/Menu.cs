using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public String clientAndServer;
    public String client;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            SceneManager.LoadScene(client);
        }else if (Input.GetKeyDown(KeyCode.S))
        {
            SceneManager.LoadScene(clientAndServer);
        }

    }
}

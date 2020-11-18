using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{

    public GameObject inputField;
    private GameManager gameManager;

    public void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Cursor.lockState = CursorLockMode.None;
    }

    public void StartServer()
    {
        gameManager.ip = "127.0.0.1";
        SceneManager.LoadScene("Scene/Server");
    }
    
    public void StartClient()
    {
        gameManager.ip = inputField.GetComponent<TMP_InputField>().text;
        SceneManager.LoadScene("Scene/Client");
    }
    
    public void StartServerOnly()
    {
        gameManager.ip = "127.0.0.1";
        SceneManager.LoadScene("Scene/OnlyServer");
    }}

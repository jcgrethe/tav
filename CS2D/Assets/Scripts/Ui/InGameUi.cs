using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InGameUi : MonoBehaviour
{

    public Canvas canvas;
    public TMP_Text ammoText;
    public TMP_Text lifeText;
    public TMP_Text killsText;
    public GameObject win;
    public GameObject lose;
    public void setCamera(Camera camera)
    {
        canvas.worldCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
    }

    public void setAmmo(String ammo)
    {
        ammoText.SetText(ammo);
    }
    
    
    public void setLife(String life)
    {
        lifeText.SetText(life);
    }
    
    public void setKills(String kills)
    {
        killsText.SetText(kills);
    }

    public void Lose()
    {
        lose.SetActive(true);
    }
    
    public void Win()
    {
        win.SetActive(true);
    }
    

}

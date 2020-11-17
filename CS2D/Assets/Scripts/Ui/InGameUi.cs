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
    


}

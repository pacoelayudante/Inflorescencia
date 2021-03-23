using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CamControl : MonoBehaviour
{
    public Text textCams;
    public Dropdown camsDropDown;

    // Start is called before the first frame update
    void Start()
    {
        var texto = "Cam - \n";
        foreach(var dev in WebCamTexture.devices) {
            texto += $" - {dev.name} kind:{dev.kind} front:{dev.isFrontFacing} autofocus:{dev.isAutoFocusPointSupported}\n";
            if (dev.availableResolutions == null) continue;
            foreach(var avres in dev.availableResolutions) {
                texto += $" --- {avres}\n";
            }
        }
        textCams.text = texto;
    }
}

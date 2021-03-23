using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using System.Linq;

[CreateAssetMenu]
public class ConfigExtraerHoja : ScriptableObject
{

    public float _sizeLimit = 480;    
    public float _cannyBajo = 100;
    public float _cannyAlto = 200;
    public int _apertureCanny = 3;
    public bool _cannyL2 = false;
    public bool _cannyDilate = true;

}
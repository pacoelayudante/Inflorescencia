using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ConfigExtraerContornoFlor : ScriptableObject
{

    public float _sizeLimit = 2048;    
    public int _border = 4;    
    public float _threshold = 20;
    public OpenCvSharp.ThresholdTypes _threshType = OpenCvSharp.ThresholdTypes.Binary;
    
    public bool _usarCanny = true;      
    public float _cannyBajo = 100;
    public float _cannyAlto = 200;
    public int _apertureCanny = 3;
    public bool _cannyL2 = false;
    public bool _cannyDilate = true;
    public bool _cannyErode = true;

    public enum EpsilonDeAproximacion {
        NoAproximar, EpsilonAbsoluto, EpsilonRelativoArcLen
    }
    public EpsilonDeAproximacion _epsilonDeAproximacion = EpsilonDeAproximacion.NoAproximar;
    public double _valorDeEpsilon = 1f;
}

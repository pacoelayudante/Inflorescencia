using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [System.Serializable]
public struct Contorno
{
    [SerializeField]public Color color;
    [SerializeField]public Vector3[] vertices;
    [SerializeField]public Contorno[] contornosInternos;
}

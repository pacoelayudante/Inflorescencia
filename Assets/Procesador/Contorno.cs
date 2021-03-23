using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Contorno
{
    public Color color;
    public Vector2[] vertices;
    public Contorno[] contornosInternos;
}

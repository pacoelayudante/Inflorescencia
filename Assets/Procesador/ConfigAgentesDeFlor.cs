using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ConfigAgentesDeFlor : ScriptableObject
{
    public float _distCerca = 30f;
    public float _multRepulsion = 4f;
    public float _multAtraccion = 0.0001f;
    public float _expansion = 0.1f;
    public float _escalaFuerza = 0.01f;
    public float _decaeFuerza = 1f;
}

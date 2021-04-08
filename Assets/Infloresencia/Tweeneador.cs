using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Tweeneador : MonoBehaviour
{
    public enum Tipo {
        Lineal, Exponencial, Curva
    }
    public bool enviarEventoInicial = true;
    public Tipo tipo = Tipo.Lineal;
    public AnimationCurve curva = AnimationCurve.EaseInOut(0,0,1,1);

    // tambien debería haber algo para el tipo de Escala Temporal

    float t;
    public float valorInicial = 0f;
    float _irA = 0f;
    public float IrA {
        get => _irA;
        set => _irA = value;
    }

    public float velocidad = 1f;

    public enum TipoValor {
        Float
    }
    public TipoValor tipoValor = TipoValor.Float;

    public Vector2 floatMinMax = new Vector2(0,1);
    float ultimaSalidaFloat = 0f;

    public UnityEvent<float> alActualizar = new UnityEvent<float>();

    void Start() {
        if (enviarEventoInicial) {
            ActualizarValor(valorInicial);
        }
    }

    void Update() {
        if (t != _irA) {
            ActualizarValor( Mathf.MoveTowards(t,_irA,velocidad*Time.deltaTime) );
        }
    }
    public void ActualizarValor(float newT) {
        t = newT;
        //si alguna vez hay mas de un tipo de valor se hara la cuestion que se io
        var salidaFloat = t;
        if (tipo == Tipo.Curva) {
            salidaFloat = Mathf.Lerp(floatMinMax[0], floatMinMax[1], curva.Evaluate(t));
        }
        else if (tipo == Tipo.Exponencial) {
            //https://twitter.com/iquilezles/status/1377402811638407169
            // c(t) = a¹⁻ᵗ · bᵗ
            // If for scalars and you need performance:
            // c(t) = 2^[ (1-t)·log₂a + t·log₂b ]
            salidaFloat = Mathf.Pow(floatMinMax[0],1-t)*Mathf.Pow(floatMinMax[1],t);
        }
        else {
            salidaFloat = Mathf.Lerp(floatMinMax[0], floatMinMax[1], t);
        }

        if (ultimaSalidaFloat != salidaFloat) {
            ultimaSalidaFloat = salidaFloat;
            alActualizar.Invoke(ultimaSalidaFloat);
        }
        
    }
}

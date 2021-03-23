using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OpenCvSharp;

public class TestExtraerContorno : EditorWindow
{
    [MenuItem("Lab/Extraer Contorno")]
    static void Abrir() => GetWindow<TestExtraerContorno>();
    public Texture2D texturaDeContornos;
    public ConfigExtraerContornoFlor config;
    
    public VerTexturaSola verThresh, verContornos;

    void OnGUI()
    {
        config = EditorGUILayout.ObjectField("Config",config,typeof(ConfigExtraerContornoFlor),false) as ConfigExtraerContornoFlor;
        texturaDeContornos = EditorGUILayout.ObjectField("Textura",texturaDeContornos,typeof(Texture2D),false) as Texture2D;

        EditorGUI.BeginDisabledGroup(texturaDeContornos==null||config==null);
        if (GUILayout.Button("Procesar"))
        {
            var observar = new Texture2D[2];
            var procesador = new ExtraerContornoFlor(config);
            var matParaProcesar = OpenCvSharp.Unity.TextureToMat(texturaDeContornos);
            procesador.Procesar(matParaProcesar,observar);

            if (verThresh) verThresh.Textura = observar[0];
            else verThresh = VerTexturaSola.Mostrar(observar[0],true,true);
            if (verContornos) verContornos.Textura = observar[1];
            else verContornos = VerTexturaSola.Mostrar(observar[1],true,true);

            matParaProcesar.Dispose();
            procesador.Dispose();
        }
        EditorGUI.EndDisabledGroup();
    }
}

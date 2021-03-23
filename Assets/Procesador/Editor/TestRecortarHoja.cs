using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using OpenCvSharp;
using UnityEngine.Networking;

public class TestRecortarHoja : EditorWindow
{
    [MenuItem("Lab/Buscar Rect Facil")]
    static void Abrir() => GetWindow<TestRecortarHoja>();
    public Texture2D texturaDescargada;
    public string UrlDescarga
    {
        get => EditorPrefs.GetString("TestRecortarHoja.UrlDescarga", "");
        set => EditorPrefs.SetString("TestRecortarHoja.UrlDescarga", value);
    }
    public bool urlDescargaValida = false;
    public UnityWebRequest descargaActual;

    public Vector2 posFloodFill;

    public VerTexturaSola verCanny, verFill, verTransformada;
    public ConfigExtraerHoja config;

    void OnGUI()
    {
        config = EditorGUILayout.ObjectField("Config",config,typeof(ConfigExtraerHoja),false) as ConfigExtraerHoja;
        EditorGUI.BeginDisabledGroup(descargaActual != null && !descargaActual.isDone);
        UrlDescarga = EditorGUILayout.TextField(UrlDescarga);
        if (GUILayout.Button("Descargar")) DescargarTextura(UrlDescarga);
        EditorGUI.EndDisabledGroup();
        if (texturaDescargada)
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(100), GUILayout.Width(100 * texturaDescargada.width / (float)texturaDescargada.height));
            var mouse = Event.current;
            EditorGUI.DrawPreviewTexture(rect, texturaDescargada);
            if (mouse.type == EventType.MouseDown && rect.Contains(mouse.mousePosition))
            {
                posFloodFill = (mouse.mousePosition - rect.position) / rect.size;
                this.Repaint();
            }
            EditorGUI.DrawRect(new UnityEngine.Rect(posFloodFill * rect.size + rect.position, Vector2.one * 5), Color.red);

        }

        EditorGUI.BeginDisabledGroup((descargaActual != null && !descargaActual.isDone)||texturaDescargada==null||config==null);
        if (GUILayout.Button("Procesar"))
        {
            var observar = new Texture2D[3];
            var procesador = new ExtraerHoja(config);
            var matDescargado = OpenCvSharp.Unity.TextureToMat(texturaDescargada);
            procesador.Procesar(matDescargado,new OpenCvSharp.Point2f(posFloodFill.x,posFloodFill.y),observar);

            if (verCanny) verCanny.Textura = observar[0];
            else verCanny = VerTexturaSola.Mostrar(observar[0],true,true);
            if (verFill) verFill.Textura = observar[1];
            else verFill = VerTexturaSola.Mostrar(observar[1],true,true);
            if (verTransformada) verTransformada.Textura = observar[2];
            else verTransformada = VerTexturaSola.Mostrar(observar[2],true,true);

            matDescargado.Dispose();
            procesador.Dispose();
        }
        EditorGUI.EndDisabledGroup();
    }

    void OnDestroy()
    {
        if (texturaDescargada) DestroyImmediate(texturaDescargada);
    }

    void DescargarTextura(string url)
    {
        if (descargaActual != null && !descargaActual.isDone) Debug.LogWarning("Ya habia una descarga en progreso pero bueno");
        var request = descargaActual = UnityWebRequestTexture.GetTexture(UrlDescarga);
        request.SendWebRequest().completed += (resultado) =>
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                urlDescargaValida = true;
                if (texturaDescargada) DestroyImmediate(texturaDescargada);
                texturaDescargada = DownloadHandlerTexture.GetContent(request);
            }
            else
            {
                urlDescargaValida = false;
            }
        };
    }
}

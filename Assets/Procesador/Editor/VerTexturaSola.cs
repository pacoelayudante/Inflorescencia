using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OpenCvSharp;
using UnityEngine.Networking;

public class VerTexturaSola : EditorWindow
{
    public static VerTexturaSola Mostrar(Texture2D txt, bool autoDestruir = true, bool instanciar = false) {
        var win = instanciar ? EditorWindow.CreateWindow<VerTexturaSola>() : GetWindow<VerTexturaSola>(true);
        if (win.textura && win.autoDestruir) {
            DestroyImmediate(win.textura);
        }
        win.textura = txt;
        win.autoDestruir = autoDestruir;
        return win;
    }

    bool autoDestruir = false;
    bool tamOriginal = false;
    Texture2D textura;
    public Texture2D Textura {
        get => textura;
        set {
            if (textura && autoDestruir) DestroyImmediate(textura);
            textura = value;
            this.Repaint();
        }
    }
    Vector2 scroll;

    void OnDestroy() {
        if (textura && autoDestruir) DestroyImmediate(textura);
    }

    void OnGUI() {
        if (!textura) {
            this.Close();
            return;
        }
        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (tamOriginal) {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Width(textura.width),GUILayout.Height(textura.height));
            EditorGUI.DrawPreviewTexture(rect,textura);
        }
        else {
            var escala = Mathf.Min( position.width/textura.width, position.height/textura.height ) ;
            var rect = EditorGUILayout.GetControlRect(GUILayout.Width(textura.width*escala),GUILayout.Height(textura.height*escala));
            EditorGUI.DrawPreviewTexture(rect,textura);
        }
        EditorGUILayout.EndScrollView();
    }
}
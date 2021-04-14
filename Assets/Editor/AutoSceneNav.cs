﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using Scene = UnityEngine.SceneManagement.Scene;

public static class AutoSceneNav
{
    [InitializeOnLoadMethod]
    public static void IniciarToolbarLitKillah()
    {
        EditorSceneManager.activeSceneChangedInEditMode -= DetectarEscenaSeleccionada;
        EditorSceneManager.activeSceneChangedInEditMode += DetectarEscenaSeleccionada;
        EditorBuildSettings.sceneListChanged -= ListarEscenasEnBuild;
        EditorBuildSettings.sceneListChanged += ListarEscenasEnBuild;
        ListarEscenasEnBuild();

        UnityToolbarExtender.ToolbarExtender.RightToolbarGUI.Add(ToolbarDerecha);
    }

    public static void ToolbarDerecha() {
        EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

        //popup escenas
        EditorGUI.BeginChangeCheck();
        int nuevaSeleccion = EditorGUILayout.Popup(escenaSeleccionadaActual, escenasEnBuild, GUILayout.ExpandWidth(false));
        if (EditorGUI.EndChangeCheck())
        {
            if (nuevaSeleccion != escenaSeleccionadaActual && nuevaSeleccion != -1 && nuevaSeleccion < escenasEnBuildPaths.Length)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(escenasEnBuildPaths[nuevaSeleccion]);
                }
            }
        }

        EditorGUI.EndDisabledGroup();
    }

    static string[] escenasEnBuild = new string[1] { "cargando..." };
    static string[] escenasEnBuildPaths = new string[1] { "cargando..." };
    static int escenaSeleccionadaActual = -1;
    static void ListarEscenasEnBuild()
    {
        escenasEnBuildPaths = EditorBuildSettings.scenes.Select(elemento => elemento.path).ToArray();
        escenasEnBuild = escenasEnBuildPaths.Select(elemento => System.IO.Path.GetFileNameWithoutExtension(elemento)).ToArray();
        DetectarEscenaSeleccionada();
    }
    static void DetectarEscenaSeleccionada()
    {
        DetectarEscenaSeleccionada(default(Scene), EditorSceneManager.GetActiveScene());
    }
    static void DetectarEscenaSeleccionada(Scene vieja, Scene nueva)
    {
        if (escenasEnBuild.Length == 0)
        {
            escenaSeleccionadaActual = -1;
            return;
        }
        escenaSeleccionadaActual = escenasEnBuildPaths.TakeWhile(elem => nueva.path != elem).Count();
        if (escenaSeleccionadaActual >= escenasEnBuildPaths.Length) escenaSeleccionadaActual = -1;
    }

}

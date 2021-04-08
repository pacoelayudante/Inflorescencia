using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using OpenCvSharp;

public class ControlTapa : MonoBehaviour
{
    public static Texture2D imagenRecuperada;
    public static Texture2D imagenRecortada;
    public static Vector2 puntoInteres;

    public UnityEvent<Texture2D> alAbrirTextura = new UnityEvent<Texture2D>();
    public ConfigExtraerHoja config;
    public string escenaGeneradora = "testflor";
    Texture2D texturaObservada;
    
    ExtraerHoja _extraerHoja;
    ExtraerHoja ExtraerHoja => _extraerHoja==null?_extraerHoja=new ExtraerHoja(config,true):_extraerHoja;

    public void PedirImagen() {
        var permiso = NativeCamera.TakePicture( path => {
            if (!string.IsNullOrEmpty(path)) {
                if (imagenRecuperada) Destroy(imagenRecuperada);
                imagenRecuperada = NativeCamera.LoadImageAtPath(path,-1,false,false);
                alAbrirTextura.Invoke(imagenRecuperada);
            }
        });
        if (permiso == NativeCamera.Permission.ShouldAsk) {
            if (NativeCamera.CanOpenSettings()) NativeCamera.OpenSettings();
        }
    }

    void OnDestroy() {
        if (_extraerHoja != null) _extraerHoja.Dispose();
        if (texturaObservada) Destroy(texturaObservada);
    }

    public void ProbarDeteccion(Vector2 puntoInteresNormalizado) {
        puntoInteres = puntoInteresNormalizado;
        Mat matImagen = OpenCvSharp.Unity.TextureToMat(imagenRecuperada);
        Mat resultante = ExtraerHoja.Procesar(matImagen, new Point2f(puntoInteresNormalizado.x,1f-puntoInteresNormalizado.y),null,true);
        var posibleNueva = OpenCvSharp.Unity.MatToTexture(resultante, texturaObservada);
        if (texturaObservada != null && texturaObservada != posibleNueva) Destroy(texturaObservada);
        texturaObservada = posibleNueva;
        if (resultante != null) resultante.Dispose();
        matImagen.Dispose();
        if (texturaObservada) alAbrirTextura.Invoke(texturaObservada);
    }

    public void RecortarImagen() {
        Mat matImagen = OpenCvSharp.Unity.TextureToMat(imagenRecuperada);
        Mat resultante = ExtraerHoja.Procesar(matImagen, new Point2f(puntoInteres.x,1f-puntoInteres.y));
        var posibleNueva = OpenCvSharp.Unity.MatToTexture(resultante, imagenRecortada);
        if (imagenRecortada != null && imagenRecortada != posibleNueva) Destroy(imagenRecortada);
        imagenRecortada = posibleNueva;
        if (resultante != null) resultante.Dispose();
        matImagen.Dispose();
        UnityEngine.SceneManagement.SceneManager.LoadScene(escenaGeneradora);
    }
}

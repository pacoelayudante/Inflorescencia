using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ControlTapa : MonoBehaviour
{
    static Texture2D imagenRecuperada;

    public UnityEvent<Texture2D> alAbrirTextura = new UnityEvent<Texture2D>();
    
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
}

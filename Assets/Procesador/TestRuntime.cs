using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCvSharp;
using System.Linq;

public class TestRuntime : MonoBehaviour
{
    public bool soloMarcar;
    public ConfigExtraerHoja config;
    public RawImage cam, canny, mask, result;
    public Material camMat;
    ExtraerHoja extractor;
    Texture2D[] texturas = new Texture2D[3];
    WebCamTexture camTex;
    Texture2D resultadoMarcado,resultadoExtraido;

    IEnumerator Start()
    {
        camTex = new WebCamTexture(WebCamTexture.devices[0].name, 9999, 9999);
        cam.texture = camTex;
        camTex.Play();
        while (camTex.width <= 0) yield return null;
        // var ar = cam.gameObject.AddComponent<AspectRatioFitter>();
        // ar.aspectRatio = camTex.width/(float)camTex.height;
        // ar.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

        // cam.transform.rotation = Quaternion.Euler(0,0, -camTex.videoRotationAngle);
        if (camTex.videoRotationAngle != 0) cam.material.EnableKeyword("ROTATE");
        else cam.material.DisableKeyword("ROTATE");

        extractor = new ExtraerHoja(config);
    }

    void OnDestroy()
    {
        camTex.Stop();
        if (extractor != null) extractor.Dispose();
    }

    void Update()
    {
        if (extractor != null && camTex.isPlaying)
        {
            var mat = OpenCvSharp.Unity.TextureToMat(camTex);
            var resultado = extractor.Procesar(mat, new Point2f(0.5f, 0.5f), soloMarcar ? null : texturas, soloMarcar);
            if (soloMarcar)
            {
                resultadoMarcado = OpenCvSharp.Unity.MatToTexture(resultado, resultadoMarcado);
                if (cam.texture != resultadoMarcado)
                {
                    if (camTex.videoRotationAngle != 0) cam.material.EnableKeyword("ROTATE");
                    else cam.material.DisableKeyword("ROTATE");
                    if (cam.texture != camTex) Destroy(cam.texture);
                    cam.texture = resultadoMarcado;
                }
            }
            mat.Dispose();
            if (!soloMarcar)
            {
                resultadoExtraido = OpenCvSharp.Unity.MatToTexture(resultado, resultadoMarcado);
                if (canny.texture != texturas[0])
                {
                    Destroy(canny.texture);
                    canny.texture = texturas[0];
                    var ar = canny.gameObject.GetComponent<AspectRatioFitter>();
                    if (!ar) ar = canny.gameObject.AddComponent<AspectRatioFitter>();
                    ar.aspectRatio = canny.texture.width / (float)canny.texture.height;
                    ar.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                }
                if (mask.texture != texturas[2])
                {
                    Destroy(mask.texture);
                    mask.texture = texturas[2];
                    var ar = mask.gameObject.GetComponent<AspectRatioFitter>();
                    if (!ar) ar = mask.gameObject.AddComponent<AspectRatioFitter>();
                    ar.aspectRatio = mask.texture.width / (float)mask.texture.height;
                    ar.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                }
                if (result.texture != texturas[1])
                {
                    Destroy(result.texture);
                    result.texture = texturas[1];
                    var ar = result.gameObject.GetComponent<AspectRatioFitter>();
                    if (!ar) ar = result.gameObject.AddComponent<AspectRatioFitter>();
                    ar.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    ar.aspectRatio = result.texture.width / (float)result.texture.height;
                }
            }

        }
    }

    public void Guardar() {
        #if UNITY_EDITOR
        var path = UnityEditor.EditorUtility.SaveFilePanelInProject("extraida","dibujo","png","");
        if (string.IsNullOrEmpty(path)) return;
        System.IO.File.WriteAllBytes(path,resultadoExtraido.EncodeToPNG());
        #endif
    }
}

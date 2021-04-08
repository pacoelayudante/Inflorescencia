using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class RawImagenEnCuadro : MonoBehaviour
{
    RawImage _rawImage;
    RawImage RawImage => _rawImage ? _rawImage : _rawImage = GetComponentInChildren<RawImage>();

    RectTransform RectTransform => (RectTransform)transform;
    RectTransform RawImageRect => RawImage?(RectTransform)RawImage.transform:null;

    public void CambiarImagen(Texture2D textura)
    {
        if (RawImage)
        {
            RawImage.enabled = true;
            RawImage.texture = textura;
            var esc = new Vector2(textura.width/RectTransform.rect.width, textura.height/RectTransform.rect.height);
            RawImageRect.offsetMin = RawImageRect.offsetMax = Vector2.zero;
            if (esc.x < esc.y)
            {
                RawImageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, RectTransform.rect.width * esc.x/esc.y);
            }
            else
            {
                RawImageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, RectTransform.rect.height * esc.y/esc.x);
            }
        }
    }
}

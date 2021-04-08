using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class TocarPosicionNormalizada : MonoBehaviour, IPointerDownHandler
{
    public UnityEvent<Vector2> alActualizar = new UnityEvent<Vector2>();
    public RectTransform RectTransform => (RectTransform)transform;

    public void OnPointerDown(PointerEventData eventData)
    {
        Vector2 resultado;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(RectTransform,eventData.position,null,out resultado)) {
            alActualizar.Invoke(Rect.PointToNormalized(RectTransform.rect,resultado));
        }
        // else {
            // pues nada, toco fuera del rect supongo?
        // }
    }
}

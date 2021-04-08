using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


[RequireComponent(typeof(RectTransform))]
public class RectTransformLerp : MonoBehaviour
{
    public RectTransform destino;
    [SerializeField] float lerp = 0f;
    RectTransform RectTransform => (RectTransform)transform;

    Vector2 sizeDeltaO,anchoredPositionO,anchorMaxO,anchorMinO,pivotO,offsetMaxO,offsetMinO;

    void Start() {
        sizeDeltaO = RectTransform.sizeDelta;
        anchoredPositionO = RectTransform.anchoredPosition;
        anchorMaxO = RectTransform.anchorMax;
        anchorMinO = RectTransform.anchorMin;
        pivotO = RectTransform.pivot;
        offsetMaxO = RectTransform.offsetMax;
        offsetMinO = RectTransform.offsetMin;
        if (lerp != 0f) Lerp(lerp);
    }

    public void Lerp(float t) {
        if (!destino) return;
        lerp = t;

        RectTransform.sizeDelta = Vector2.Lerp( sizeDeltaO, destino.sizeDelta, t );
        RectTransform.anchoredPosition = Vector2.Lerp( anchoredPositionO, destino.anchoredPosition, t );
        RectTransform.anchorMax = Vector2.Lerp( anchorMaxO, destino.anchorMax, t );
        RectTransform.anchorMin = Vector2.Lerp( anchorMinO, destino.anchorMin, t );
        RectTransform.pivot = Vector2.Lerp( pivotO, destino.pivot, t );
        RectTransform.offsetMax = Vector2.Lerp( offsetMaxO, destino.offsetMax, t );
        RectTransform.offsetMin = Vector2.Lerp( offsetMinO, destino.offsetMin, t );

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(RectTransformLerp))]
    public class MiEditor : Editor {
        float t;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            EditorGUI.BeginChangeCheck();
            t = EditorGUILayout.Slider("lerp test",t,0,1);
            if (EditorGUI.EndChangeCheck()) {
                foreach(RectTransformLerp coso in targets) coso.Lerp(t);
            }
            EditorGUI.EndDisabledGroup();
        }            
    }
#endif
}

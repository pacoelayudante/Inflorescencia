// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Skybox/Gradient Skybox"
{
    Properties
    {
        _Offset ("Offset", Color) = (1, 1, 1, 0)
        _Escala ("Escala", Color) = (1, 1, 1, 0)
        _Velocidad ("Velocidad", Color) = (1, 1, 1, 0)
        _Fase ("Fase", Color) = (1, 1, 1, 0)
        _UpVector ("Up Vector", Vector) = (0, 1, 0, 0)
        _Intensity ("Intensity", Float) = 1.0
        //_Exponent ("Exponent", Float) = 1.0
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    struct appdata
    {
        float4 position : POSITION;
        float3 texcoord : TEXCOORD0;
    };
    
    struct v2f
    {
        float4 position : SV_POSITION;
        float3 texcoord : TEXCOORD0;
    };
    
    half4 _Offset;
    half4 _Escala;
    half4 _Velocidad;
    half4 _Fase;
    half4 _UpVector;
    half _Intensity;
    //half _Exponent;
    
    v2f vert (appdata v)
    {
        v2f o;
        o.position = UnityObjectToClipPos (v.position);
        o.texcoord = v.texcoord;
        return o;
    }
    
    fixed4 frag (v2f i) : COLOR
    {
        const float TWO_PI = 3.14159265 * 2;
        half d = dot (normalize (i.texcoord), _UpVector) * 0.5f + 0.5f;
        // return lerp (_Color1, _Color2, pow (d, _Exponent)) * _Intensity;
        return _Offset + _Escala * cos ( TWO_PI * (d*_Velocidad + _Fase) );
    }

    ENDCG

    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" }
        Pass
        {
            ZWrite Off
            Cull Off
            Fog { Mode Off }
            CGPROGRAM
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
}

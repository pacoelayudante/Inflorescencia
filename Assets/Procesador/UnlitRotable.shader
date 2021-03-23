Shader "Unlit/UnlitRotable"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [Toggle(ROTATE)] _Invert("Rotate", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile shader_feature ROTATE

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
#ifdef ROTATE
                float screenAspect = _ScreenParams.x/_ScreenParams.y;
                float textureAspect = _MainTex_TexelSize.w/_MainTex_TexelSize.z;
                float x = max(1.0,screenAspect/textureAspect);
                float y = max(1.0,textureAspect/screenAspect);
                o.uv = float2(1.0-v.uv.y*y,v.uv.x*x);
#else
                float screenAspect = _ScreenParams.x/_ScreenParams.y;
                float textureAspect = _MainTex_TexelSize.z/_MainTex_TexelSize.w;
                float x = max(1.0,screenAspect/textureAspect);
                float y = max(1.0,textureAspect/screenAspect);
                o.uv = v.uv*float2(x,y);
#endif
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}

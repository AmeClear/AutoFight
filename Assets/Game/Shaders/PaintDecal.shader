Shader "AutoFight/PaintDecal"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.15, 0.4, 1)
        _Edge ("Edge", Range(0, 1)) = 0.32
        _Softness ("Softness", Range(0.01, 1)) = 0.48
        _Blob ("Blob", Range(0, 1)) = 0.4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Offset -2, -2
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Edge;
            float _Softness;
            float _Blob;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                float n = sin(p.x * 13.7 + p.y * 8.3) * 0.12;
                n += sin(p.x * 6.1 - p.y * 15.4) * 0.1;
                n += sin((p.x + p.y) * 21.0) * 0.05;
                float r = length(p) + n * _Blob;
                float a = 1.0 - smoothstep(_Edge, _Edge + _Softness, r);
                a *= a;
                a *= i.color.a * _Color.a;
                clip(a - 0.02);
                return fixed4(_Color.rgb * i.color.rgb, a);
            }
            ENDCG
        }
    }
    FallBack Off
}

Shader "Custom/RangeShader"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (1,1,1,0.5)
        _EdgeColor ("Edge Color", Color) = (1,0,0,1)
        _EdgeWidth ("Edge Width", Range(0, 1)) = 0.1
        _PatternTex ("Pattern Texture", 2D) = "white" {}
        _PatternTiling ("Pattern Tiling", Float) = 1
        _PulsateSpeed ("Pulsate Speed", Float) = 0
        _RotateSpeed ("Rotate Speed", Float) = 0
        _GlowIntensity ("Glow Intensity", Float) = 2
        _ExpansionProgress ("Expansion Progress", Range(0, 1)) = 1
        _InnerColor ("Inner Color", Color) = (1,0.5,0,0.8)
        _UseColorTransition ("Use Color Transition", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                float2 worldPos : TEXCOORD1;
            };

            fixed4 _MainColor;
            fixed4 _EdgeColor;
            float _EdgeWidth;
            sampler2D _PatternTex;
            float _PatternTiling;
            float _PulsateSpeed;
            float _RotateSpeed;
            float _GlowIntensity;
            float _ExpansionProgress;
            fixed4 _InnerColor;
            float _UseColorTransition;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float distanceFromCenter = distance(i.uv, center);

                // 扩散裁剪：超出当前进度部分丢弃
                if (distanceFromCenter > _ExpansionProgress)
                {
                    discard;
                }

                // 旋转效果
                if (_RotateSpeed > 0)
                {
                    float2 rotatedUV = i.uv - center;
                    float angle = _Time.y * _RotateSpeed * 0.1;
                    float sinAngle, cosAngle;
                    sincos(angle, sinAngle, cosAngle);
                    rotatedUV = float2(
                        rotatedUV.x * cosAngle - rotatedUV.y * sinAngle,
                        rotatedUV.x * sinAngle + rotatedUV.y * cosAngle
                    );
                    rotatedUV += center;
                    i.uv = rotatedUV;
                }

                // 脉冲效果
                float pulse = 1.0;
                if (_PulsateSpeed > 0)
                {
                    pulse = 0.9 + 0.1 * sin(_Time.y * _PulsateSpeed);
                }

                // 图案纹理
                fixed4 pattern = tex2D(_PatternTex, i.uv * _PatternTiling);

                // 颜色渐变
                fixed4 finalMainColor = _MainColor;
                if (_UseColorTransition > 0.5)
                {
                    float colorLerp = distanceFromCenter / _ExpansionProgress;
                    finalMainColor = lerp(_InnerColor, _MainColor, colorLerp);
                }

                // 边缘检测
                float edge = smoothstep(1.0 - _EdgeWidth, 1.0, distanceFromCenter);
                float inverseEdge = 1.0 - edge;

                // 主区域颜色
                fixed4 col = finalMainColor;
                col.rgb *= pattern.rgb * pulse;
                col.a *= pattern.a * inverseEdge;

                // 边缘光
                fixed4 edgeCol = _EdgeColor;
                edgeCol.a *= edge;
                edgeCol.rgb *= _GlowIntensity;

                col = lerp(col, edgeCol, edge);
                return col;
            }
            ENDCG
        }
    }
}
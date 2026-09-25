Shader "FTGame/CharacterColourHighX" {
    Properties {
        _Color ("Main Color", Vector) = (1,1,1,1)
        _Bright ("Bright", Float) = 1
        _AlphaValue ("AlphaValue", Float) = 0
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _ReplaceTex1 ("ReplaceTex1 (RGBA)", 2D) = "white" {}
        _RepTexOffsetFromX1 ("RepTexOffsetFromX1", Float) = 0.5
        _RepTexOffsetFromY1 ("RepTexOffsetFromY1", Float) = 0
        _RepTexOffsetToX1 ("RepTexOffsetToX1", Float) = 1
        _RepTexOffsetToY1 ("RepTexOffsetToY1", Float) = 0.5
        _SpecularRamp ("Specular Map", 2D) = "black" {}
        _SpeSaturation ("Specular Saturation", Range(-1, 1)) = 0
        _SpeIntensity ("Specular Intensity", Range(0, 15)) = 1
        _FLSpeed ("FL Speed", Range(0, 5)) = 0
        _FLIntensity ("FL Intensity", Range(0, 5)) = 1
        _EnvColor ("Env Color", Vector) = (1,1,1,1)
        _RimColor ("Rim Color", Vector) = (0.25,0.12,0.018,1)
        _RimLightDir ("RimLightDir", Vector) = (0.9,0.7,0,0)
        _RimPow ("RimPow", Range(2, 20)) = 4
        _RimBrightness ("RimBrightness", Range(0, 5)) = 1
        _ColourTex ("ColourTex (RGB)", 2D) = "white" {}
        [MaterialToggle] _GrayValue ("GrayValue", Float) = 0
        _CastColor1 ("CastColor1", Vector) = (1,1,1,1)
        _CastLight1 ("ColorLight1", Range(0, 5)) = 1
        _CastColor2 ("CastColor2", Vector) = (1,1,1,1)
        _CastLight2 ("ColorLight2", Range(0, 5)) = 1
        _CastColor3 ("CastColor3", Vector) = (1,1,1,1)
        _CastLight3 ("ColorLight3", Range(0, 5)) = 1
        _CastColor4 ("CastColor4", Vector) = (1,1,1,1)
        _CastLight4 ("ColorLight4", Range(0, 5)) = 0
        _CastColor5 ("CastColor5", Vector) = (1,1,1,1)
        _CastLight5 ("ColorLight5", Range(0, 5)) = 0
        _Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5
        [Enum(Off,0,Front,1,Back,2)] _Cull ("Cull Mode", Float) = 2
        [HideInInspector] _SrcAlphaBlend ("", Float) = 0
    }

    SubShader {
        Tags { 
            "Queue"="AlphaTest" 
            "RenderType"="TransparentCutout" 
            "IgnoreProjector"="True" 
        }
        LOD 200
        Cull [_Cull]

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _ColourTex;
            float4 _MainTex_ST;
            
            fixed4 _Color;
            half _Bright;
            fixed _Cutoff;
            half _GrayValue;

            half4 _CastColor1; half _CastLight1;
            half4 _CastColor2; half _CastLight2;
            half4 _CastColor3; half _CastLight3;
            half4 _CastColor4; half _CastLight4;
            half4 _CastColor5; half _CastLight5;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half3 ApplyDye(half3 currentCol, half3 sourceCol, half mask, half4 castCol, half castLight) {
                // [Suy luận]: saturate(castLight) bảo vệ vùng không bị đen khi ColorLight = 0
                half blendWeight = mask * saturate(castLight);
                half3 dyedCol = sourceCol * castCol.rgb * castLight;
                return lerp(currentCol, dyedCol, blendWeight);
            }

            fixed4 frag(v2f i) : SV_Target {
                half4 baseTex = tex2D(_MainTex, i.uv);
                half4 maskTex = tex2D(_ColourTex, i.uv);

                // Cutoff pixel theo kênh Alpha của ColourTex
                clip(maskTex.a - _Cutoff);

                // Khử màu nền về Grayscale khi bật _GrayValue
                half gray = dot(baseTex.rgb, half3(0.299, 0.587, 0.114));
                half3 sourceCol = lerp(baseTex.rgb, gray.xxx, _GrayValue);

                // Tách 5 ID mask từ ColourTex
                half r = maskTex.r;
                half g = maskTex.g;
                half b = maskTex.b;

                half maskWhite = min(r, min(g, b));         // Vùng 5 (Trắng)
                half maskCyan  = saturate(min(g, b) - r);    // Vùng 4 (Cyan)
                half maskRed   = saturate(r - max(g, b));    // Vùng 1 (Đỏ)
                half maskGreen = saturate(g - max(r, b));    // Vùng 2 (Xanh lá)
                half maskBlue  = saturate(b - max(r, g));    // Vùng 3 (Xanh dương)

                // Áp màu nhuộm
                half3 finalRGB = baseTex.rgb;
                finalRGB = ApplyDye(finalRGB, sourceCol, maskRed,   _CastColor1, _CastLight1);
                finalRGB = ApplyDye(finalRGB, sourceCol, maskGreen, _CastColor2, _CastLight2);
                finalRGB = ApplyDye(finalRGB, sourceCol, maskBlue,  _CastColor3, _CastLight3);
                finalRGB = ApplyDye(finalRGB, sourceCol, maskCyan,  _CastColor4, _CastLight4);
                finalRGB = ApplyDye(finalRGB, sourceCol, maskWhite, _CastColor5, _CastLight5);

                // Độ sáng tổng và màu chính
                finalRGB *= _Color.rgb * _Bright;

                return fixed4(finalRGB, 1.0);
            }
            ENDCG
        }
    }
    Fallback "Transparent/Cutout/VertexLit"
}
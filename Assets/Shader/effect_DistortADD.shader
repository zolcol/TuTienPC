Shader "effect/DistortADD" {
    Properties {
        _TintColor ("Tint Color", Vector) = (0.5, 0.5, 0.5, 0.5)
        _NoiseTex ("Distort Texture", 2D) = "white" {}
        _MainTex ("Alpha Texture", 2D) = "white" {}
        _HeatTime ("Heat Time(-1 1)", Float) = 0
        _ForceX ("Strength X(0 1)", Float) = 0.1
        _ForceY ("Strength Y(0 1)", Float) = 0.1
    }

    SubShader {
        Tags { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
        }
        
        Blend SrcAlpha One
        Cull Off
        Lighting Off
        ZWrite Off

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            fixed4 _TintColor;
            float _HeatTime;
            float _ForceX;
            float _ForceY;

            struct appdata_t {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uvMain : TEXCOORD0;
                float2 uvNoise : TEXCOORD1;
            };

            v2f vert (appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uvMain = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.uvNoise = TRANSFORM_TEX(v.texcoord, _NoiseTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Cuộn UV của Noise Texture theo HeatTime và thời gian
                float2 noiseUV = i.uvNoise + float2(_HeatTime, _HeatTime) * _Time.y;
                
                // Lấy độ lệch từ kênh R/G của ảnh Distort Texture
                fixed4 noise = tex2D(_NoiseTex, noiseUV);
                float2 offset = (noise.rg * 2.0 - 1.0) * float2(_ForceX, _ForceY);

                // Sample Alpha Texture (Flipbook) sau khi bị bẻ cong UV
                fixed4 mainCol = tex2D(_MainTex, i.uvMain + offset);

                // Additive chuẩn Particle Unity: nhân đôi _TintColor (mặc định 0.5 để giữ nguyên độ sáng gốc)
                return 2.0f * i.color * _TintColor * mainCol;
            }
            ENDCG
        }
    }
}
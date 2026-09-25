Shader "effect/Falloff" {
    Properties {
        _FalloffLevel ("FalloffLevel", Range(0, 5)) = 1.153846
        _Color ("Color", Vector) = (1, 0, 0, 1)
        [MaterialToggle] _Invert ("Invert", Float) = 1
        [HideInInspector] _SrcAlphaBlend ("", Float) = 1
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

            float _FalloffLevel;
            fixed4 _Color;
            float _Invert;

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldViewDir : TEXCOORD1;
                fixed4 color : COLOR;
            };

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldViewDir = UnityWorldSpaceViewDir(worldPos);
                
                // Fallback màu trắng nếu mesh 3D không có dữ liệu màu đỉnh
                o.color = any(v.color) ? v.color : fixed4(1, 1, 1, 1);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.worldViewDir);

                // Dùng abs() để viền cánh hoa sáng đều cả 2 mặt khi tắt Cull (Cull Off)
                float NdotV = abs(dot(normal, viewDir));

                // Invert = 1: sáng ở viền (Fresnel/Rim); Invert = 0: sáng ở tâm diện tích
                float falloff = (_Invert > 0.5) ? (1.0 - NdotV) : NdotV;
                falloff = pow(saturate(falloff), _FalloffLevel);

                // Áp dụng độ sáng vào kênh alpha cho chế độ Blend SrcAlpha One
                fixed4 col = _Color * i.color;
                return fixed4(col.rgb, col.a * falloff);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
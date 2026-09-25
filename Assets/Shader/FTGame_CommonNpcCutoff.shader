Shader "FTGame/CommonNpcCutoff" {
	Properties {
		_Color ("Main Color", Vector) = (1,1,1,1)
		_Bright ("Bright", Float) = 1
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5
		_AlphaValue ("AlphaValue", Float) = 0
		[HideInInspector] _SrcAlphaBlend ("", Float) = 0
	}

	SubShader {
		Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
		LOD 200

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;

			fixed4 _Color;
			half _Bright;
			fixed _Cutoff;
			half _AlphaValue;

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

			fixed4 frag(v2f i) : SV_Target {
				fixed4 baseTex = tex2D(_MainTex, i.uv);

				// Cắt bỏ phần trong suốt dựa vào kênh Alpha của _MainTex
				clip(baseTex.a * _Color.a - _Cutoff - _AlphaValue);

				// Áp độ sáng tổng và Main Color
				half3 finalRGB = baseTex.rgb * _Color.rgb * _Bright;

				return fixed4(finalRGB, baseTex.a * _Color.a);
			}
			ENDCG
		}
	}
	Fallback "Transparent/Cutout/VertexLit"
}
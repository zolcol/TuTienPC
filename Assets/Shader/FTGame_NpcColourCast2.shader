Shader "FTGame/NpcColourCast2" {
	Properties {
		_Color ("Main Color", Vector) = (1,1,1,1)
		_Bright ("Bright", Float) = 1
		_AlphaValue ("AlphaValue", Float) = 0
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_ColourTex ("ColourTex (RGB)", 2D) = "white" {}
		[MaterialToggle] _GrayValue ("GrayValue", Float) = 0
		_CastColor1 ("CastColor1", Vector) = (1,1,1,1)
		_CastColor2 ("CastColor2", Vector) = (1,1,1,1)
		_CastColor3 ("CastColor3", Vector) = (1,1,1,1)
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
		[HideInInspector] _SrcAlphaBlend ("", Float) = 0
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Cull [_Cull]

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _ColourTex;
			float4 _ColourTex_ST;

			float4 _Color;
			half _Bright;
			half _AlphaValue;
			half _GrayValue;

			float4 _CastColor1;
			float4 _CastColor2;
			float4 _CastColor3;

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

			half3 ApplyDye(half3 currentCol, half3 sourceCol, half mask, float4 castCol) {
				half blendWeight = mask * saturate(castCol.a);
				half3 dyedCol = sourceCol * castCol.rgb;
				return lerp(currentCol, dyedCol, blendWeight);
			}

			fixed4 frag(v2f i) : SV_Target {
				fixed4 baseTex = tex2D(_MainTex, i.uv);
				fixed4 maskTex = tex2D(_ColourTex, i.uv);

				// Khử màu nền về Grayscale khi bật _GrayValue
				half gray = dot(baseTex.rgb, half3(0.299, 0.587, 0.114));
				half3 sourceCol = lerp(baseTex.rgb, gray.xxx, _GrayValue);

				// Nhuộm màu theo từng vùng dựa trên 3 kênh R, G, B của ColourTex
				half3 finalRGB = baseTex.rgb;
				finalRGB = ApplyDye(finalRGB, sourceCol, maskTex.r, _CastColor1);
				finalRGB = ApplyDye(finalRGB, sourceCol, maskTex.g, _CastColor2);
				finalRGB = ApplyDye(finalRGB, sourceCol, maskTex.b, _CastColor3);

				// Áp độ sáng tổng và Main Color
				finalRGB *= _Color.rgb * _Bright;

				return fixed4(finalRGB, baseTex.a * _Color.a);
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
}
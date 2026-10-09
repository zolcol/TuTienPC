Shader "effects/TextureDistortionAdd_mask" {
	Properties{
		_TintColor("Color", Vector) = (0.5,0.5,0.5,0.5)
		_MainTex("mainTexture (RGB)", 2D) = "white" {}
		_MainTex_alpha("mainTexture (R)", 2D) = "white" {}
		_MoveX("Move X", Float) = 0
		_MoveY("Move Y", Float) = 0
		_NoiseTex("Distortion Texture (RG)", 2D) = "white" {}
		_MaskTex("Distortion Mask Texture", 2D) = "white" {}
		_HeatTime("Distortion Move Dirition", Float) = 0
		_ForceX("Power X", Range(0, 1)) = 0.1
		_ForceY("Power Y", Range(0, 1)) = 0.1
		_Brightness("Brightness", Float) = 2
		[HideInInspector] _SrcAlphaBlend("", Float) = 0
	}

		SubShader{
			Tags {
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
				"PreviewType" = "Plane"
			}

			Cull Off
			Lighting Off
			ZWrite Off
			Blend[_SrcAlphaBlend] One

			Pass {
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				struct appdata {
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
					fixed4 color : COLOR;
				};

				struct v2f {
					float4 pos : SV_POSITION;
					float2 uv : TEXCOORD0;
					fixed4 color : COLOR;
				};

				fixed4 _TintColor;
				sampler2D _MainTex;
				float4 _MainTex_ST;
				sampler2D _MainTex_alpha;
				float4 _MainTex_alpha_ST;
				float _MoveX;
				float _MoveY;
				sampler2D _NoiseTex;
				float4 _NoiseTex_ST;
				sampler2D _MaskTex;
				float4 _MaskTex_ST;
				float _HeatTime;
				float _ForceX;
				float _ForceY;
				float _Brightness;

				v2f vert(appdata v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;
					o.color = v.color;
					return o;
				}

				fixed4 frag(v2f i) : SV_Target {
					// 1. Tính toán chuyển động và lấy mẫu Distortion Noise (RG)
					float2 noiseUV = TRANSFORM_TEX(i.uv, _NoiseTex) + float2(_HeatTime, _HeatTime) * _Time.y;
					float2 noise = tex2D(_NoiseTex, noiseUV).rg;

					// 2. Lấy mẫu Distortion Mask (giới hạn vùng biến dạng)
					float2 maskUV = TRANSFORM_TEX(i.uv, _MaskTex);
					float mask = tex2D(_MaskTex, maskUV).r;

					// 3. Vector độ dời UV
					float2 distortionOffset = (noise * 2.0 - 1.0) * float2(_ForceX, _ForceY) * mask;

					// 4. Áp dụng độ dời và tốc độ di chuyển (Move X, Move Y) cho Main UV
					float2 moveOffset = float2(_MoveX, _MoveY) * _Time.y;
					float2 mainUV = TRANSFORM_TEX(i.uv, _MainTex) + moveOffset + distortionOffset;
					fixed4 mainCol = tex2D(_MainTex, mainUV);

					// 5. Lấy mẫu kênh Alpha từ MainTex_alpha
					float2 alphaUV = TRANSFORM_TEX(i.uv, _MainTex_alpha) + moveOffset + distortionOffset;
					fixed alphaTex = tex2D(_MainTex_alpha, alphaUV).r;

					// 6. Tổng hợp màu sắc và độ trong suốt
					fixed finalAlpha = mainCol.a * alphaTex * _TintColor.a * i.color.a;
					fixed3 finalRGB = mainCol.rgb * _TintColor.rgb * i.color.rgb * _Brightness;

					return fixed4(finalRGB * finalAlpha, finalAlpha);
				}
				ENDCG
			}
		}
			FallBack Off
}
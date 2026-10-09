Shader "effects/TextureDistortionBlend_mask" {
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
		[Enum(UnityEngine.Rendering.BlendMode)] _SourceBlend("Source Blend Mode", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DestBlend("Dest Blend Mode", Float) = 10
		[HideInInspector] _SrcAlphaBlend("", Float) = 0
	}

		SubShader{
			Tags {
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
				"PreviewType" = "Plane"
			}
			Blend[_SourceBlend][_DestBlend]
			Cull Off
			Lighting Off
			ZWrite Off

			Pass {
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0
				#include "UnityCG.cginc"

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
					float2 uvMask : TEXCOORD2;
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

				v2f vert(appdata_t v) {
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.color = v.color;
					o.uvMain = TRANSFORM_TEX(v.texcoord, _MainTex);
					o.uvNoise = TRANSFORM_TEX(v.texcoord, _NoiseTex);
					o.uvMask = TRANSFORM_TEX(v.texcoord, _MaskTex);
					return o;
				}

				fixed4 frag(v2f i) : SV_Target {
					// Mask giới hạn vùng distortion và tạo hình mờ viền
					fixed4 maskCol = tex2D(_MaskTex, i.uvMask);

				// Tính toán trôi UV cho NoiseTex
				float2 noiseUV = i.uvNoise + float2(_HeatTime, _HeatTime) * _Time.y;
				fixed4 noiseCol = tex2D(_NoiseTex, noiseUV);

				// Chuyển kênh RG từ [0, 1] sang [-1, 1], khống chế bởi Force và Mask
				float2 distort = (noiseCol.rg * 2.0 - 1.0) * float2(_ForceX, _ForceY) * maskCol.r;

				// Cuộn UV MainTex kết hợp bù lệch từ distortion
				float2 mainUV = i.uvMain + float2(_MoveX, _MoveY) * _Time.y + distort;
				fixed4 mainCol = tex2D(_MainTex, mainUV);
				fixed4 alphaCol = tex2D(_MainTex_alpha, mainUV);

				// Xuất màu cuối
				fixed4 col;
				col.rgb = mainCol.rgb * _TintColor.rgb * _Brightness * i.color.rgb;
				col.a = mainCol.a * alphaCol.r * maskCol.r * saturate(_TintColor.a * 2.0) * i.color.a;

				return col;
			}
			ENDCG
		}
		}
			FallBack Off
}
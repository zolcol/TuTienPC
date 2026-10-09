Shader "FT/Effect/Dif_Rd_Mask_Rd" {
	Properties{
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src(Add:SrcAlpha Blend:SrcAlpha)", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst(Add:One Blend:OneMinusSrcAlpha)", Float) = 1
		_TintColor("TintColor", Vector) = (1,1,1,1)
		_Intensity("Intensity", Float) = 1
		[Space(20)] _MainTex("MainTex(RGBA)", 2D) = "white" {}
		[Toggle] _Animation_Main("Animation", Float) = 0
		[Toggle] _Custom1xy("Custom1xy", Float) = 0
		_Animation1stX("SpeedX", Float) = 0.5
		_Animation1stY("SpeedY", Float) = 0.5
		[Space(20)] _RaoDongMain("RaoDong(Maintex)", 2D) = "white" {}
		[Toggle] _Animation_RaoDong("Animation", Float) = 0
		[Toggle] _Custom1zw("Custom1zw", Float) = 0
		_UvSpeed2edX("SpeedX", Float) = 0
		_UvSpeed2edY("SpeedY", Float) = 0
		_Force1stX("Power X", Range(-1, 1)) = 0
		_Force1stY("Power Y", Range(-1, 1)) = 0
		[Space(20)] _MaskTex("MaskTex(R)", 2D) = "white" {}
		_MaskSpeedX("Mask SpeedX", Float) = 0
		_MaskSpeedY("Mask SpeedY", Float) = 0
		[Space(20)][Space(20)][Toggle] _RGBScal("RGB_Scal", Float) = 1
		[Toggle] _AScal("A_Scal", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 0
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2
		[HideInInspector] _SrcAlphaBlend("", Float) = 0
	}

		SubShader{
			Tags {
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
				"PreviewType" = "Plane"
			}
			Blend[_SrcBlend][_DstBlend]
			Cull[_Cull]
			ZWrite[_ZWrite]
			Lighting Off

			Pass {
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				struct appdata {
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float2 uv : TEXCOORD0;
					float4 custom1 : TEXCOORD1;
				};

				struct v2f {
					float4 pos : SV_POSITION;
					fixed4 color : COLOR;
					float2 uvMain : TEXCOORD0;
					float2 uvRd : TEXCOORD1;
					float2 uvMask : TEXCOORD2;
				};

				sampler2D _MainTex;
				float4 _MainTex_ST;
				float _Animation_Main;
				float _Custom1xy;
				float _Animation1stX;
				float _Animation1stY;

				sampler2D _RaoDongMain;
				float4 _RaoDongMain_ST;
				float _Animation_RaoDong;
				float _Custom1zw;
				float _UvSpeed2edX;
				float _UvSpeed2edY;
				float _Force1stX;
				float _Force1stY;

				sampler2D _MaskTex;
				float4 _MaskTex_ST;
				float _MaskSpeedX;
				float _MaskSpeedY;

				float4 _TintColor;
				float _Intensity;
				float _RGBScal;
				float _AScal;
				float _SrcAlphaBlend;

				v2f vert(appdata v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.color = v.color;

					// UV MainTex
					float2 uvMain = TRANSFORM_TEX(v.uv, _MainTex);
					if (_Animation_Main > 0.5) {
						uvMain += _Time.y * float2(_Animation1stX, _Animation1stY);
					}
					if (_Custom1xy > 0.5) {
						uvMain += v.custom1.xy;
					}
					o.uvMain = uvMain;

					// UV RaoDongMain
					float2 uvRd = TRANSFORM_TEX(v.uv, _RaoDongMain);
					if (_Animation_RaoDong > 0.5) {
						uvRd += _Time.y * float2(_UvSpeed2edX, _UvSpeed2edY);
					}
					if (_Custom1zw > 0.5) {
						uvRd += v.custom1.zw;
					}
					o.uvRd = uvRd;

					// UV MaskTex
					float2 uvMask = TRANSFORM_TEX(v.uv, _MaskTex);
					uvMask += _Time.y * float2(_MaskSpeedX, _MaskSpeedY);
					o.uvMask = uvMask;

					return o;
				}

				fixed4 frag(v2f i) : SV_Target {
					float2 offset = float2(0, 0);

					// Chỉ sample texture méo khi Power X/Y khác 0 (tránh lệch UV và tiết kiệm lệnh khi để trống)
					if (abs(_Force1stX) > 0.001 || abs(_Force1stY) > 0.001) {
						fixed4 rd = tex2D(_RaoDongMain, i.uvRd);
						offset = (rd.rg - 0.5) * float2(_Force1stX, _Force1stY);
					}

					// Sample MainTex và MaskTex
					fixed4 mainCol = tex2D(_MainTex, i.uvMain + offset);
					fixed mask = tex2D(_MaskTex, i.uvMask + offset).r;

					// Xuất màu cuối
					fixed4 col;
					col.rgb = mainCol.rgb * _TintColor.rgb * i.color.rgb;
					col.a = mainCol.a * _TintColor.a * i.color.a * mask;

					if (_RGBScal > 0.5) {
						col.rgb *= _Intensity;
					}
					if (_AScal > 0.5) {
						col.a *= _Intensity;
					}

					return col;
				}
				ENDCG
			}
		}
			FallBack Off
}
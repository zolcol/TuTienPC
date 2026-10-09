Shader "FT/Effect/Dif_Rj_Rd" {
	Properties{
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src(Add:SrcAlpha Blend:SrcAlpha)", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst(Add:One Blend:OneMinusSrcAlpha)", Float) = 1
		[Space(20)] _TintColor("Color", Vector) = (1,1,1,1)
		_Intensity("Intensity", Float) = 1
		[Space(20)] _MainTex("Texture", 2D) = "white" {}
		[Space(20)][Toggle] _Animation_Main("Animation", Float) = 0
		[Toggle] _Custom1xy("Custom1xy", Float) = 0
		_Animation1stX("SpeedX", Float) = 0.5
		_Animation1stY("SpeedY", Float) = 0.5
		[Space(20)] _RongJie("RongJie", 2D) = "White" {}
		_ruanying("ruanying", Range(0.5, 1)) = 1
		_qiangdu("qiangdu", Range(0, 2)) = 0
		[Toggle] _Animation_RongJie("Animation", Float) = 0
		[Toggle] _Custom1zw("Custom1zw", Float) = 0
		_UvSpeed3edX("SpeedX", Float) = 0
		_UvSpeed3edY("SpeedY", Float) = 0
		[Space(20)][Toggle] _RaoDongMainKey("RaoDongMainKey", Float) = 0
		_RaoDongMain("RaoDong(Maintex)", 2D) = "white" {}
		_UvSpeed2edX("SpeedX", Float) = 0
		_UvSpeed2edY("SpeedY", Float) = 0
		_Force1stX("Power X", Range(-1, 1)) = 0
		_Force1stY("Power Y", Range(-1, 1)) = 0
		_MaskTex("MaskTex", 2D) = "white" {}
		[Toggle] _RGBScal("RGB_Scal", Float) = 0
		[Toggle] _AScal("A_Scal", Float) = 0
		[Space(20)][Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 0
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2
		[HideInInspector] _SrcAlphaBlend("", Float) = 0
	}

		SubShader{
			Tags {
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
			}

			Blend[_SrcBlend][_DstBlend]
			Cull[_Cull]
			ZWrite[_ZWrite]

			Pass {
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				fixed4 _TintColor;
				half _Intensity;

				sampler2D _MainTex;
				float4 _MainTex_ST;
				fixed _Animation_Main;
				fixed _Custom1xy;
				half _Animation1stX;
				half _Animation1stY;

				sampler2D _RongJie;
				float4 _RongJie_ST;
				half _ruanying;
				half _qiangdu;
				fixed _Animation_RongJie;
				fixed _Custom1zw;
				half _UvSpeed3edX;
				half _UvSpeed3edY;

				fixed _RaoDongMainKey;
				sampler2D _RaoDongMain;
				float4 _RaoDongMain_ST;
				half _UvSpeed2edX;
				half _UvSpeed2edY;
				half _Force1stX;
				half _Force1stY;

				sampler2D _MaskTex;
				float4 _MaskTex_ST;
				fixed _RGBScal;
				fixed _AScal;

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
					float2 uvRj : TEXCOORD1;
					float2 uvRd : TEXCOORD2;
					float2 uvMask : TEXCOORD3;
				};

				v2f vert(appdata v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.color = v.color;

					// UV Texture chính
					float2 uvMain = TRANSFORM_TEX(v.uv, _MainTex);
					if (_Animation_Main > 0.5) {
						uvMain += _Time.y * float2(_Animation1stX, _Animation1stY);
					}
					if (_Custom1xy > 0.5) {
						uvMain += v.custom1.xy;
					}
					o.uvMain = uvMain;

					// UV RongJie (Tan biến)
					float2 uvRj = TRANSFORM_TEX(v.uv, _RongJie);
					if (_Animation_RongJie > 0.5) {
						uvRj += _Time.y * float2(_UvSpeed3edX, _UvSpeed3edY);
					}
					if (_Custom1zw > 0.5) {
						uvRj += v.custom1.zw;
					}
					o.uvRj = uvRj;

					// UV RaoDong (Nhiễu/méo)
					float2 uvRd = TRANSFORM_TEX(v.uv, _RaoDongMain);
					uvRd += _Time.y * float2(_UvSpeed2edX, _UvSpeed2edY);
					o.uvRd = uvRd;

					// UV Mask
					o.uvMask = TRANSFORM_TEX(v.uv, _MaskTex);

					return o;
				}

				fixed4 frag(v2f i) : SV_Target {
					// 1. Tính toán méo UV (RaoDong)
					float2 mainUV = i.uvMain;
					if (_RaoDongMainKey > 0.5) {
						half4 rdTex = tex2D(_RaoDongMain, i.uvRd);
						half2 rdOffset = (rdTex.rg * 2.0 - 1.0) * float2(_Force1stX, _Force1stY);
						mainUV += rdOffset;
					}

					// 2. Sample texture chính và áp màu
					fixed4 col = tex2D(_MainTex, mainUV);
					col *= _TintColor;
					col.rgb *= _Intensity;
					col *= i.color;

					// 3. Xử lý tan biến (RongJie)
					half rj = tex2D(_RongJie, i.uvRj).r;
					half softWidth = max(1.0001 - _ruanying, 0.0001);
					half dissolve = saturate((rj - _qiangdu) / softWidth);
					col.a *= dissolve;

					// 4. Xử lý Mask
					half4 mask = tex2D(_MaskTex, i.uvMask);
					if (_RGBScal > 0.5) {
						col.rgb *= mask.rgb;
					}
					if (_AScal > 0.5) {
						half maskA = (mask.a < 0.999) ? mask.a : mask.r;
						col.a *= maskA;
					}

					return col;
				}
				ENDCG
			}
		}
			FallBack Off
}
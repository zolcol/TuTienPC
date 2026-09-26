Shader "FT/Effect/Dif_Rj_Rd" {
	Properties {
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src(Add:SrcAlpha Blend:SrcAlpha)", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst(Add:One Blend:OneMinusSrcAlpha)", Float) = 1
		[Space(20)] _TintColor ("Color", Vector) = (1,1,1,1)
		_Intensity ("Intensity", Float) = 1
		[Space(20)] _MainTex ("Texture", 2D) = "white" {}
		[Space(20)] [Toggle] _Animation_Main ("Animation", Float) = 0
		[Toggle] _Custom1xy ("Custom1xy", Float) = 0
		_Animation1stX ("SpeedX", Float) = 0.5
		_Animation1stY ("SpeedY", Float) = 0.5
		[Space(20)] _RongJie ("RongJie", 2D) = "White" {}
		_ruanying ("ruanying", Range(0.5, 1)) = 1
		_qiangdu ("qiangdu", Range(0, 2)) = 0
		[Toggle] _Animation_RongJie ("Animation", Float) = 0
		[Toggle] _Custom1zw ("Custom1zw", Float) = 0
		_UvSpeed3edX ("SpeedX", Float) = 0
		_UvSpeed3edY ("SpeedY", Float) = 0
		[Space(20)] [Toggle] _RaoDongMainKey ("RaoDongMainKey", Float) = 0
		_RaoDongMain ("RaoDong(Maintex)", 2D) = "white" {}
		_UvSpeed2edX ("SpeedX", Float) = 0
		_UvSpeed2edY ("SpeedY", Float) = 0
		_Force1stX ("Power X", Range(-1, 1)) = 0
		_Force1stY ("Power Y", Range(-1, 1)) = 0
		_MaskTex ("MaskTex", 2D) = "white" {}
		[Toggle] _RGBScal ("RGB_Scal", Float) = 0
		[Toggle] _AScal ("A_Scal", Float) = 0
		[Space(20)] [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 0
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
		[HideInInspector] _SrcAlphaBlend ("", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy);
			}

			ENDHLSL
		}
	}
}
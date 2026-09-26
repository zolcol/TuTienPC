Shader "effects/TextureDistortionBlend_mask_Area" {
	Properties {
		_TintColor ("Color", Vector) = (0.5,0.5,0.5,0.5)
		_MainTex ("mainTexture (RGB)", 2D) = "white" {}
		_MainTex_alpha ("mainTexture (R)", 2D) = "white" {}
		_MoveX ("Move X", Float) = 0
		_MoveY ("Move Y", Float) = 0
		_NoiseTex ("Distortion Texture (RG)", 2D) = "white" {}
		_MaskTex ("Distortion Mask Texture", 2D) = "white" {}
		_HeatTime ("Distortion Move Dirition", Float) = 0
		_ForceX ("Power X", Range(0, 1)) = 0.1
		_ForceY ("Power Y", Range(0, 1)) = 0.1
		_Brightness ("Brightness", Float) = 2
		_Area ("Area", Vector) = (0,0,1,1)
		[Enum(UnityEngine.Rendering.BlendMode)] _SourceBlend ("Source Blend Mode", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DestBlend ("Dest Blend Mode", Float) = 10
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
// Based on the original True Explosions shader, updated for URP.
Shader "Custom/Explosion"
{
	Properties
	{
		_RampTex ("Color Ramp", 2D) = "white" {}
		_DispTex ("Displacement Texture", 2D) = "gray" {}
		_Displacement ("Displacement", Range(0, 1.0)) = 0.1
		_ChannelFactor ("ChannelFactor (r,g,b)", Vector) = (1,0,0,1)
		_Range ("Range (min,max)", Vector) = (0,0.5,0,1)
		_ClipRange ("ClipRange [0,1]", Float) = 0.8
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Opaque"
			"Queue" = "Geometry"
		}

		Cull Off
		LOD 300

		Pass
		{
			Name "ForwardUnlit"
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex Vert
			#pragma fragment Frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_RampTex);
			SAMPLER(sampler_RampTex);
			TEXTURE2D(_DispTex);
			SAMPLER(sampler_DispTex);

			CBUFFER_START(UnityPerMaterial)
				float4 _RampTex_ST;
				float4 _DispTex_ST;
				float _Displacement;
				float4 _ChannelFactor;
				float4 _Range;
				float _ClipRange;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			float SampleDisplacement(float2 uv)
			{
				float3 displacementColor = SAMPLE_TEXTURE2D_LOD(_DispTex, sampler_DispTex, uv, 0).rgb;
				return dot(displacementColor, _ChannelFactor.rgb);
			}

			Varyings Vert(Attributes input)
			{
				Varyings output;
				float2 uv = TRANSFORM_TEX(input.uv, _DispTex);
				float displacement = SampleDisplacement(uv);
				float3 displacedPositionOS = input.positionOS.xyz + normalize(input.normalOS) * displacement * _Displacement;

				output.positionCS = TransformObjectToHClip(displacedPositionOS);
				output.uv = uv;
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				float displacement = SampleDisplacement(input.uv) * (_Range.y - _Range.x) + _Range.x;
				clip(_ClipRange - displacement);

				half4 ramp = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(displacement, 0.5));
				return half4(ramp.rgb + ramp.rgb * ramp.a, 1.0);
			}
			ENDHLSL
		}
	}

	Fallback Off
}

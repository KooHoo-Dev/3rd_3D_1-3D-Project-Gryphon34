// 사실적인 크리처 텍스처를 POLYGON(로우폴리) 화풍에 맞추는 URP 셰이더.
// 원본 텍스처를 그대로 쓰되 디테일을 덜어 냅니다: 흐리게 읽기 → 명도 단계 줄이기 → 면 단위 셰이딩.
Shader "BossRaid/StylizedCreature"
{
	Properties
	{
		[Header(Color)]
		[MainTexture] _BaseMap("Base Map", 2D) = "white" {}
		[MainColor] _Tint("Tint", Color) = (1,1,1,1)
		_HueShift("Hue Shift", Range(-0.5, 0.5)) = 0
		_Saturation("Saturation", Range(0, 2)) = 1
		_Brightness("Brightness", Range(0, 2)) = 1

		[Header(Stylize)]
		_DetailBlur("Detail Blur (Mip Bias)", Range(0, 8)) = 4
		_PosterizeSteps("Posterize Steps", Range(2, 16)) = 6
		_PosterizeAmount("Posterize Amount", Range(0, 1)) = 0.8
		_FlatShading("Flat Shading", Range(0, 1)) = 1

		[Header(Surface)]
		_Smoothness("Smoothness", Range(0, 1)) = 0.05
		_Metallic("Metallic", Range(0, 1)) = 0
		_OcclusionMap("Occlusion", 2D) = "white" {}
		_OcclusionStrength("Occlusion Strength", Range(0, 1)) = 0.5

		[Header(Emission)]
		_EmissionMap("Emission Map", 2D) = "black" {}
		[HDR] _EmissionColor("Emission Color", Color) = (1,1,1,1)
	}

	SubShader
	{
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" "UniversalMaterialType"="Lit" }
		Cull Back

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
			float4 _BaseMap_ST;
			float4 _OcclusionMap_ST;
			float4 _EmissionMap_ST;
			half4 _Tint;
			half4 _EmissionColor;
			half _HueShift;
			half _Saturation;
			half _Brightness;
			half _DetailBlur;
			half _PosterizeSteps;
			half _PosterizeAmount;
			half _FlatShading;
			half _Smoothness;
			half _Metallic;
			half _OcclusionStrength;
		CBUFFER_END

		TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
		TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
		TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
		ENDHLSL

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode"="UniversalForward" }

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex Vert
			#pragma fragment Frag

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
			#pragma multi_compile _ _FORWARD_PLUS
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS   : NORMAL;
				float2 uv         : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv         : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float3 normalWS   : TEXCOORD2;
				half   fogFactor  : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			Varyings Vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);

				VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = posInputs.positionCS;
				output.positionWS = posInputs.positionWS;
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.uv = input.uv;
				output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
				return output;
			}

			// 색조는 유지하고 명도만 계단으로 끊어야 팔레트처럼 보이고 색이 튀지 않습니다.
			half3 Stylize(half3 color)
			{
				half3 hsv = RgbToHsv(color);
				hsv.x = frac(hsv.x + _HueShift);
				hsv.y = saturate(hsv.y * _Saturation);

				half steps = max(_PosterizeSteps, 2.0);
				half posterized = floor(hsv.z * steps + 0.5) / steps;
				hsv.z = lerp(hsv.z, posterized, _PosterizeAmount);

				return HsvToRgb(hsv) * _Brightness;
			}

			// 화면 공간 미분으로 삼각형 면의 노멀을 구합니다. 플랫폼마다 부호가 달라서 원래 노멀 쪽으로 맞춥니다.
			float3 GetFlatNormal(float3 positionWS, float3 smoothNormalWS)
			{
				float3 flatNormal = normalize(cross(ddy(positionWS), ddx(positionWS)));
				return dot(flatNormal, smoothNormalWS) < 0 ? -flatNormal : flatNormal;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);

				float2 uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
				half3 baseColor = SAMPLE_TEXTURE2D_BIAS(_BaseMap, sampler_BaseMap, uv, _DetailBlur).rgb;
				half3 albedo = Stylize(baseColor) * _Tint.rgb;

				half occlusion = SAMPLE_TEXTURE2D_BIAS(_OcclusionMap, sampler_OcclusionMap, uv, _DetailBlur).g;
				occlusion = lerp(1.0, occlusion, _OcclusionStrength);

				half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb;

				float3 smoothNormal = normalize(input.normalWS);
				float3 normalWS = normalize(lerp(smoothNormal, GetFlatNormal(input.positionWS, smoothNormal), _FlatShading));

				InputData inputData = (InputData)0;
				inputData.positionWS = input.positionWS;
				inputData.positionCS = input.positionCS;
				inputData.normalWS = normalWS;
				inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
				inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				inputData.fogCoord = input.fogFactor;
				inputData.bakedGI = SampleSH(normalWS);
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
				inputData.shadowMask = half4(1, 1, 1, 1);

				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = albedo;
				surfaceData.metallic = _Metallic;
				surfaceData.smoothness = _Smoothness;
				surfaceData.occlusion = occlusion;
				surfaceData.emission = emission;
				surfaceData.alpha = 1.0;

				half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);
				finalColor.rgb = MixFog(finalColor.rgb, inputData.fogCoord);
				finalColor.a = 1.0;
				return finalColor;
			}
			ENDHLSL
		}

		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }
			ZWrite On
			ZTest LEqual
			ColorMask 0

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex ShadowVert
			#pragma fragment ShadowFrag
			#pragma multi_compile_instancing
			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

			float3 _LightDirection;
			float3 _LightPosition;

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS   : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			float4 ShadowVert(Attributes input) : SV_POSITION
			{
				UNITY_SETUP_INSTANCE_ID(input);
				float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
				float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

			#if _CASTING_PUNCTUAL_LIGHT_SHADOW
				float3 lightDirectionWS = normalize(_LightPosition - positionWS);
			#else
				float3 lightDirectionWS = _LightDirection;
			#endif

				float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
				positionCS = ApplyShadowClamping(positionCS);
				return positionCS;
			}

			half4 ShadowFrag() : SV_Target
			{
				return 0;
			}
			ENDHLSL
		}

		Pass
		{
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }
			ZWrite On
			ColorMask R

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex DepthVert
			#pragma fragment DepthFrag
			#pragma multi_compile_instancing

			struct Attributes
			{
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			float4 DepthVert(Attributes input) : SV_POSITION
			{
				UNITY_SETUP_INSTANCE_ID(input);
				return TransformObjectToHClip(input.positionOS.xyz);
			}

			half DepthFrag() : SV_Target
			{
				return 0;
			}
			ENDHLSL
		}

		Pass
		{
			Name "DepthNormals"
			Tags { "LightMode"="DepthNormals" }
			ZWrite On

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex DepthNormalsVert
			#pragma fragment DepthNormalsFrag
			#pragma multi_compile_instancing

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS   : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 normalWS   : TEXCOORD0;
			};

			Varyings DepthNormalsVert(Attributes input)
			{
				Varyings output;
				UNITY_SETUP_INSTANCE_ID(input);
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				return output;
			}

			half4 DepthNormalsFrag(Varyings input) : SV_Target
			{
				return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
			}
			ENDHLSL
		}
	}
	FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

// URP port of Synty's Amplify "SyntyStudios/CustomCharacter" (original was a Built-in surface shader).
// Same shader name, property names and masking order, so existing materials keep their colors.
Shader "SyntyStudios/CustomCharacter"
{
	Properties
	{
		_Color_Primary("Color_Primary", Color) = (0.2431373,0.4196079,0.6196079,0)
		_Color_Secondary("Color_Secondary", Color) = (0.8196079,0.6431373,0.2980392,0)
		_Color_Leather_Primary("Color_Leather_Primary", Color) = (0.282353,0.2078432,0.1647059,0)
		_Color_Metal_Primary("Color_Metal_Primary", Color) = (0.5960785,0.6117647,0.627451,0)
		_Color_Leather_Secondary("Color_Leather_Secondary", Color) = (0.372549,0.3294118,0.2784314,0)
		_Color_Metal_Dark("Color_Metal_Dark", Color) = (0.1764706,0.1960784,0.2156863,0)
		_Color_Metal_Secondary("Color_Metal_Secondary", Color) = (0.345098,0.3764706,0.3960785,0)
		_Color_Hair("Color_Hair", Color) = (0.2627451,0.2117647,0.1333333,0)
		_Color_Skin("Color_Skin", Color) = (1,0.8000001,0.682353,1)
		_Color_Stubble("Color_Stubble", Color) = (0.8039216,0.7019608,0.6313726,1)
		_Color_Scar("Color_Scar", Color) = (0.9294118,0.6862745,0.5921569,1)
		_Color_BodyArt("Color_BodyArt", Color) = (0.2283196,0.5822246,0.7573529,1)
		_Color_Eyes("Color_Eyes", Color) = (0.2283196,0.5822246,0.7573529,1)
		_Texture("Texture", 2D) = "white" {}
		_Metallic("Metallic", Range( 0 , 1)) = 0
		_Smoothness("Smoothness", Range( 0 , 1)) = 0
		_Emission("Emission", Range( 0 , 1)) = 0
		_BodyArt_Amount("BodyArt_Amount", Range( 0 , 1)) = 0
		[HideInInspector]_Mask_01("Mask_01", 2D) = "white" {}
		[HideInInspector]_Mask_02("Mask_02", 2D) = "white" {}
		[HideInInspector]_Mask_03("Mask_03", 2D) = "white" {}
		[HideInInspector]_Mask_04("Mask_04", 2D) = "white" {}
		[HideInInspector]_Mask_05("Mask_05", 2D) = "white" {}
	}

	SubShader
	{
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" "UniversalMaterialType"="Lit" }
		Cull Back

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
			float4 _Texture_ST;
			float4 _Mask_01_ST;
			float4 _Mask_02_ST;
			float4 _Mask_03_ST;
			float4 _Mask_04_ST;
			float4 _Mask_05_ST;
			half4 _Color_Primary;
			half4 _Color_Secondary;
			half4 _Color_Leather_Primary;
			half4 _Color_Leather_Secondary;
			half4 _Color_Metal_Primary;
			half4 _Color_Metal_Secondary;
			half4 _Color_Metal_Dark;
			half4 _Color_Hair;
			half4 _Color_Skin;
			half4 _Color_Stubble;
			half4 _Color_Scar;
			half4 _Color_BodyArt;
			half4 _Color_Eyes;
			half _Metallic;
			half _Smoothness;
			half _Emission;
			half _BodyArt_Amount;
		CBUFFER_END

		TEXTURE2D(_Texture); SAMPLER(sampler_Texture);
		TEXTURE2D(_Mask_01); SAMPLER(sampler_Mask_01);
		TEXTURE2D(_Mask_02); SAMPLER(sampler_Mask_02);
		TEXTURE2D(_Mask_03); SAMPLER(sampler_Mask_03);
		TEXTURE2D(_Mask_04); SAMPLER(sampler_Mask_04);
		TEXTURE2D(_Mask_05); SAMPLER(sampler_Mask_05);
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
			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			#pragma multi_compile _ SHADOWS_SHADOWMASK
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

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

			// Masks paint a region where the channel is dark (step(mask, 0.5) == 1).
			half4 Paint(half4 baseColor, half4 regionColor, half maskChannel)
			{
				return lerp(baseColor, regionColor, step(maskChannel, 0.5));
			}

			half4 Frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);

				float2 uv = input.uv;
				half4 mask1 = SAMPLE_TEXTURE2D(_Mask_01, sampler_Mask_01, uv * _Mask_01_ST.xy + _Mask_01_ST.zw);
				half4 mask2 = SAMPLE_TEXTURE2D(_Mask_02, sampler_Mask_02, uv * _Mask_02_ST.xy + _Mask_02_ST.zw);
				half4 mask3 = SAMPLE_TEXTURE2D(_Mask_03, sampler_Mask_03, uv * _Mask_03_ST.xy + _Mask_03_ST.zw);
				half4 mask4 = SAMPLE_TEXTURE2D(_Mask_04, sampler_Mask_04, uv * _Mask_04_ST.xy + _Mask_04_ST.zw);
				half4 mask5 = SAMPLE_TEXTURE2D(_Mask_05, sampler_Mask_05, uv * _Mask_05_ST.xy + _Mask_05_ST.zw);

				half4 color = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, uv * _Texture_ST.xy + _Texture_ST.zw);
				color = Paint(color, _Color_Primary, mask1.r);
				color = Paint(color, _Color_Secondary, mask1.g);
				color = Paint(color, _Color_Leather_Primary, mask4.r);
				color = Paint(color, _Color_Leather_Secondary, mask4.g);
				color = Paint(color, _Color_Metal_Primary, mask2.r);
				color = Paint(color, _Color_Metal_Secondary, mask2.g);
				color = Paint(color, _Color_Metal_Dark, mask2.b);
				color = Paint(color, _Color_Hair, mask4.b);
				color = Paint(color, _Color_Skin, mask3.r);
				color = Paint(color, _Color_Stubble, mask3.b);
				color = Paint(color, _Color_Scar, mask3.g);
				color = lerp(_Color_Eyes, color, mask5.r);

				half bodyArtMask = lerp(mask1.b, 1.0, 1.0 - _BodyArt_Amount);
				color = lerp(_Color_BodyArt, color, bodyArtMask);

				InputData inputData = (InputData)0;
				inputData.positionWS = input.positionWS;
				inputData.positionCS = input.positionCS;
				inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
				inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
				inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				inputData.fogCoord = input.fogFactor;
				inputData.bakedGI = SampleSH(inputData.normalWS);
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
				inputData.shadowMask = half4(1, 1, 1, 1);

				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = color.rgb;
				surfaceData.metallic = _Metallic;
				surfaceData.smoothness = _Smoothness;
				surfaceData.occlusion = 1.0;
				surfaceData.emission = (1.0 - mask5.r) * _Emission;
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

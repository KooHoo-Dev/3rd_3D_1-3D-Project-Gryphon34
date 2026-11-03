// 가려진 플레이어 실루엣. 보스·벽 뒤에 가려진 부분만 단색 + 윤곽 테두리로 덧그립니다.
// 플레이어 머티리얼에 넣는 셰이더가 아니라, PC_Renderer의 Render Objects가 덮어쓰기(Override Material)로 씁니다.
// 가려진 곳만 고르는 건 깊이 테스트(ZTest Greater)이고, 이 셰이더는 그 픽셀의 색과 알파만 정합니다.
// 플레이어 본체보다 먼저 그려야 자기 몸에 가려진 팔·다리가 실루엣이 되지 않습니다 (Render Objects 순서).
Shader "BossRaid/OccludedSilhouette"
{
	Properties
	{
		_SilhouetteColor("Silhouette Color", Color) = (0.1843, 0.549, 1, 1)
		_Opacity("Opacity", Range(0, 1)) = 0.5          // 안쪽이 덮는 정도 (가린 물체가 비침)
		_RimStrength("Rim Strength", Range(0, 3)) = 1.5 // 윤곽 테두리 세기
		_RimWidth("Rim Width", Range(0.05, 1)) = 0.6    // 반지름 중 바깥 몇 할을 테두리로 칠할지
	}

	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

		Pass
		{
			Name "Occluded"
			Tags { "LightMode" = "SRPDefaultUnlit" }
			ZTest Greater // 이미 더 가까운 것(보스, 벽)이 그려진 픽셀만
			ZWrite Off    // 깊이를 안 써서 뒤에 그리는 플레이어 본체를 망치지 않음
			Blend SrcAlpha OneMinusSrcAlpha

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			// SRP Batcher: 머티리얼 값은 모두 이 버퍼 안에
			CBUFFER_START(UnityPerMaterial)
				float4 _SilhouetteColor;
				float _Opacity;
				float _RimStrength;
				float _RimWidth;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes v)
			{
				Varyings o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
				o.positionCS = TransformWorldToHClip(o.positionWS);
				o.normalWS = TransformObjectToWorldNormal(v.normalOS);
				return o;
			}

			// 프레넬 테두리: 윤곽일수록 1. 단색만 칠하면 덩어리로 보여서 윤곽을 진하게 해 팔다리가 읽히게 함
			// 1 - facing 대신 sin(시선과 법선 사이 각)을 써서, 둥근 몸에서 테두리가 실처럼 가늘어지지 않음
			float Rim(float3 normalWS, float3 viewWS)
			{
				float facing = saturate(dot(normalWS, viewWS));
				float side = sqrt(saturate(1.0 - facing * facing));
				return smoothstep(1.0 - _RimWidth, 1.0, side);
			}

			float4 frag(Varyings i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float3 normalWS = normalize(i.normalWS);
				float3 viewWS = GetWorldSpaceNormalizeViewDir(i.positionWS);

				float edge = saturate(Rim(normalWS, viewWS) * _RimStrength);
				float alpha = lerp(_Opacity, 1.0, edge);                                 // 안쪽은 불투명도만큼, 윤곽은 꽉
				float3 color = lerp(_SilhouetteColor.rgb, float3(1.0, 1.0, 1.0), edge * 0.4); // 윤곽은 살짝 희게
				return float4(color, alpha);
			}
			ENDHLSL
		}
	}
}

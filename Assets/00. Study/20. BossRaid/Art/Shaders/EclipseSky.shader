// 일식이 일어난 어두운 하늘을 그리는 URP 스카이박스 셰이더. 텍스처 없이 시선 방향만으로 계산합니다.
// 그리는 순서: 하늘 그라데이션 → 별 → 빛무리 → 아래로 떨어지는 빛기둥 → 코로나 → 검은 원판 → 지평선 연기
// 스카이박스에는 씬 안개가 먹지 않으므로, 안개 색을 Horizon Color와 같게 맞춰야 경계가 안 보입니다.
Shader "BossRaid/EclipseSky"
{
	Properties
	{
		[Header(Sky)]
		_ZenithColor("Zenith Color", Color) = (0.008, 0.008, 0.01, 1)
		_HorizonColor("Horizon Color", Color) = (0.11, 0.1, 0.1, 1)
		_GroundColor("Ground Color", Color) = (0.04, 0.035, 0.035, 1)
		_HorizonSharpness("Horizon Sharpness", Range(0.5, 8)) = 2.5

		[Header(Eclipse)]
		_EclipseAzimuth("Azimuth", Range(0, 360)) = 20
		_EclipseAltitude("Altitude", Range(0, 85)) = 32
		_EclipseSize("Disc Size (deg)", Range(0.5, 20)) = 5
		_DiscColor("Disc Color", Color) = (0, 0, 0, 1)
		[HDR] _CoronaColor("Corona Color", Color) = (12, 1.6, 0.35, 1)
		_CoronaWidth("Corona Width (radii)", Range(0.01, 0.5)) = 0.08
		_CoronaNoise("Corona Noise", Range(0, 1)) = 0.6
		_CoronaSpeed("Corona Speed", Range(0, 2)) = 0.3
		[HDR] _GlowColor("Glow Color", Color) = (1.2, 0.12, 0.04, 1)
		_GlowSize("Glow Size (radii)", Range(0.2, 10)) = 2.5

		[Header(Light Pillar)]
		[HDR] _BeamColor("Beam Color", Color) = (5, 0.5, 0.12, 1)
		_BeamWidth("Beam Width (radii)", Range(0, 0.5)) = 0.06
		_BeamSpread("Beam Spread", Range(0, 0.05)) = 0.006
		_BeamLength("Beam Length (deg)", Range(0, 90)) = 40

		[Header(Clouds)]
		_CloudColor("Cloud Color", Color) = (0.16, 0.14, 0.14, 1)
		_CloudCoverage("Coverage", Range(0, 1)) = 0.55
		_CloudScale("Scale", Range(0.5, 12)) = 4
		_CloudHeight("Height", Range(0.02, 1)) = 0.3
		_CloudSpeed("Speed", Range(0, 0.1)) = 0.01
		_CloudEclipseTint("Eclipse Tint", Range(0, 4)) = 1

		[Header(Stars)]
		_StarDensity("Density", Range(0, 1)) = 0.3
		_StarBrightness("Brightness", Range(0, 4)) = 0.8
	}

	SubShader
	{
		Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
		Cull Off
		ZWrite Off

		Pass
		{
			Name "EclipseSky"

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
				half4 _ZenithColor;
				half4 _HorizonColor;
				half4 _GroundColor;
				half _HorizonSharpness;
				float _EclipseAzimuth;
				float _EclipseAltitude;
				float _EclipseSize;
				half4 _DiscColor;
				half4 _CoronaColor;
				half _CoronaWidth;
				half _CoronaNoise;
				half _CoronaSpeed;
				half4 _GlowColor;
				half _GlowSize;
				half4 _BeamColor;
				half _BeamWidth;
				half _BeamSpread;
				half _BeamLength;
				half4 _CloudColor;
				half _CloudCoverage;
				half _CloudScale;
				half _CloudHeight;
				half _CloudSpeed;
				half _CloudEclipseTint;
				half _StarDensity;
				half _StarBrightness;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 direction : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float Hash31(float3 p)
			{
				p = frac(p * 0.1031);
				p += dot(p, p.zyx + 31.32);
				return frac((p.x + p.y) * p.z);
			}

			float3 Hash33(float3 p)
			{
				p = frac(p * float3(0.1031, 0.1030, 0.0973));
				p += dot(p, p.yxz + 33.33);
				return frac((p.xxy + p.yxx) * p.zyx);
			}

			float ValueNoise(float3 p)
			{
				float3 i = floor(p);
				float3 f = frac(p);
				f = f * f * (3.0 - 2.0 * f);

				float n000 = Hash31(i);
				float n100 = Hash31(i + float3(1, 0, 0));
				float n010 = Hash31(i + float3(0, 1, 0));
				float n110 = Hash31(i + float3(1, 1, 0));
				float n001 = Hash31(i + float3(0, 0, 1));
				float n101 = Hash31(i + float3(1, 0, 1));
				float n011 = Hash31(i + float3(0, 1, 1));
				float n111 = Hash31(i + float3(1, 1, 1));

				float nx00 = lerp(n000, n100, f.x);
				float nx10 = lerp(n010, n110, f.x);
				float nx01 = lerp(n001, n101, f.x);
				float nx11 = lerp(n011, n111, f.x);
				return lerp(lerp(nx00, nx10, f.y), lerp(nx01, nx11, f.y), f.z);
			}

			// 0~1 범위의 4옥타브 노이즈
			float Fbm(float3 p)
			{
				float sum = 0;
				float amplitude = 0.5;
				for (int k = 0; k < 4; k++)
				{
					sum += amplitude * ValueNoise(p);
					p = p * 2.03 + 17.1;
					amplitude *= 0.5;
				}
				return sum / 0.9375;
			}

			Varyings Vert(Attributes input)
			{
				Varyings output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.direction = input.positionOS.xyz;
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				float3 dir = normalize(input.direction);
				float time = _Time.y;

				// 하늘 그라데이션: 지평선 → 천정, 지평선 아래는 땅 색
				float up = max(dir.y, 1e-4);
				float3 color = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(up, 1.0 / _HorizonSharpness));
				color = lerp(color, _GroundColor.rgb, saturate(-dir.y * 8));

				// 일식 기준 좌표계: forward는 원판 중심, right·upE는 원판 위의 가로·세로
				float azimuth = radians(_EclipseAzimuth);
				float altitude = radians(_EclipseAltitude);
				float3 eclipseDir = float3(cos(altitude) * sin(azimuth), sin(altitude), cos(altitude) * cos(azimuth));
				float3 right = normalize(cross(float3(0, 1, 0), eclipseDir));
				float3 upE = cross(eclipseDir, right);

				float forward = dot(dir, eclipseDir);
				float localX = dot(dir, right);
				float localY = dot(dir, upE);
				float front = step(0, forward);
				float frontSoft = smoothstep(-0.1, 0.3, forward); // 빛기둥용: front처럼 뒤쪽 반구를 지우되 경계가 뚝 끊기지 않게

				float radius = radians(_EclipseSize * 0.5);
				float angle = acos(clamp(forward, -1, 1));
				float d = angle / radius;      // 원판 반지름을 1로 둔 거리
				float outside = max(d - 1, 0);

				// 별: 방향 벡터를 격자로 나눠 일부 칸에만 점을 찍고 깜빡이게
				float3 starPos = dir * 220;
				float3 cell = floor(starPos);
				float cellHash = Hash31(cell);
				float3 starOffset = (Hash33(cell) - 0.5) * 0.6;
				float starDist = length(frac(starPos) - 0.5 - starOffset);
				float star = step(1 - _StarDensity * 0.01, cellHash) * saturate(1 - starDist * 4);
				star *= 0.6 + 0.4 * sin(time * (2 + cellHash * 3) + cellHash * 100);
				color += star * star * _StarBrightness * saturate(dir.y * 4);

				// 빛무리: 원판 주변의 붉은 번짐 + 하늘 전체로 퍼지는 아주 옅은 붉은 기
				float glow = exp(-outside * 3 / _GlowSize) + 0.08 * exp(-angle * 1.5);
				color += _GlowColor.rgb * glow; // angle로 이미 서서히 옅어짐. front를 곱하면 90°에서 하늘이 반으로 갈림

				// 빛기둥: 원판 아래로 가늘게 떨어지고, 내려갈수록 퍼지면서 옅어짐
				float below = degrees(asin(clamp(-localY, -1, 1)));
				float beamX = abs(localX) / sin(radius);
				float beamWidth = max(_BeamWidth + _BeamSpread * below, 1e-4);
				float beam = exp(-(beamX * beamX) / (beamWidth * beamWidth));
				beam *= step(0, below) * pow(saturate(1 - below / max(_BeamLength, 1e-3)), 1.5);
				beam *= 0.85 + 0.15 * ValueNoise(float3(time * 2, 0, 0));
				beam *= frontSoft * saturate(1 + dir.y * 10);
				color += _BeamColor.rgb * beam;

				// 코로나: 원판 테두리에서 바깥으로 빠르게 사라지는 고리. 각도마다 두께가 출렁임
				float phi = atan2(localY, localX);
				float ringNoise = Fbm(float3(cos(phi) * 3, sin(phi) * 3, time * _CoronaSpeed));
				float coronaWidth = max(_CoronaWidth * lerp(1, 0.4 + 1.2 * ringNoise, _CoronaNoise), 1e-4);
				float corona = exp(-outside / coronaWidth) * front;

				float edge = max(fwidth(d), 1e-4);
				float disc = (1 - smoothstep(1 - edge, 1 + edge, d)) * front;
				color += _CoronaColor.rgb * corona * (1 - disc);
				color = lerp(color, _DiscColor.rgb, disc);

				// 지평선 연기: 하늘을 평면으로 펴서 노이즈를 흘리고, 위로 갈수록 걷힘
				float2 cloudUV = dir.xz / (abs(dir.y) + 0.25);
				float2 drift = time * _CloudSpeed * float2(1, 0.3);
				float cloudNoise = Fbm(float3(cloudUV * _CloudScale * 0.3 + drift, time * _CloudSpeed * 0.5));
				float cloudMask = 1 - smoothstep(0, _CloudHeight, dir.y);
				float threshold = 1 - _CloudCoverage;
				float cloud = smoothstep(threshold, threshold + 0.35, cloudNoise) * cloudMask;

				float3 cloudColor = _CloudColor.rgb;
				cloudColor += _GlowColor.rgb * _CloudEclipseTint * exp(-angle * 2.5); // 빛무리와 같은 이유로 front 뺌
				cloudColor += _BeamColor.rgb * beam * 0.25;
				color = lerp(color, cloudColor, cloud * 0.85);

				return half4(color, 1);
			}
			ENDHLSL
		}
	}

	Fallback Off
}

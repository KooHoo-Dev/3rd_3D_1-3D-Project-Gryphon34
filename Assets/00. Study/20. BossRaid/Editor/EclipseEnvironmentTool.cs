using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BossRaid.Tools
{
    // 열린 씬의 환경을 일식 하늘(BossRaid/EclipseSky)에 맞추는 메뉴
    // : 스카이박스, 안개, 환경광, 주 조명, 후처리 볼륨(Bloom·톤매핑·채도·비네트), 카메라 후처리 켜기
    // 머티리얼과 볼륨 프로필은 처음 한 번만 만들고, 다시 실행하면 만들어 둔 것(직접 고친 값 포함)을 그대로 씁니다.
    // 안개·환경광(RenderSettings)은 Undo가 안 되므로, 마음에 안 들면 씬을 저장하지 말고 다시 여세요.
    public static class EclipseEnvironmentTool
    {
        private const string ShaderName = "BossRaid/EclipseSky";
        private const string MaterialPath = "Assets/00. Study/20. BossRaid/Art/Materials/M_EclipseSky.mat";
        private const string ProfilePath = "Assets/00. Study/20. BossRaid/Art/Settings/VP_EclipseNight.asset";
        private const string VolumeName = "Eclipse Volume";
        private const float FogDensity = 0.015f;

        [MenuItem("BossRaid/Environment/Apply Eclipse Night")]
        private static void Apply()
        {
            var material = LoadOrCreateSkyMaterial();
            if (material == null) return;

            ApplyRenderSettings(material);
            ApplySun(material);
            ApplyVolume();
            EnableCameraPostProcessing();

            DynamicGI.UpdateEnvironment();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"일식 환경을 적용했습니다. 하늘 값은 {MaterialPath}, 후처리는 {ProfilePath}에서 조절하세요");
        }

        private static Material LoadOrCreateSkyMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) return material;

            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"{ShaderName} 셰이더를 찾지 못했습니다. EclipseSky.shader 컴파일 에러를 먼저 확인하세요");
                return null;
            }

            material = new Material(shader) { name = Path.GetFileNameWithoutExtension(MaterialPath) };
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static void ApplyRenderSettings(Material material)
        {
            RenderSettings.skybox = material;

            // 스카이박스엔 안개가 안 먹으므로 안개 색을 지평선 색과 같게 맞춰 경계를 숨깁니다
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = material.GetColor("_HorizonColor");
            RenderSettings.fogDensity = FogDensity;

            // 어두운 하늘에서 환경광을 뽑으면 너무 까매지므로 직접 색을 지정
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.16f, 0.13f, 0.14f);
            RenderSettings.ambientEquatorColor = new Color(0.11f, 0.09f, 0.09f);
            RenderSettings.ambientGroundColor = new Color(0.04f, 0.035f, 0.035f);
            RenderSettings.reflectionIntensity = 0.4f;
        }

        // 주 조명을 일식 방향에서 비추는 어둡고 탁한 빛으로
        private static void ApplySun(Material material)
        {
            var sun = RenderSettings.sun != null ? RenderSettings.sun : FindDirectionalLight();
            if (sun == null)
            {
                var go = new GameObject("Directional Light");
                Undo.RegisterCreatedObjectUndo(go, "Create Eclipse Light");
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
            }

            Undo.RecordObjects(new Object[] { sun, sun.transform }, "Apply Eclipse Light");
            sun.color = new Color(0.7f, 0.58f, 0.55f);
            sun.intensity = 0.4f;
            sun.transform.rotation = Quaternion.LookRotation(-GetEclipseDirection(material));
            RenderSettings.sun = sun;
        }

        private static Light FindDirectionalLight()
        {
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional) return light;
            }
            return null;
        }

        // 셰이더의 eclipseDir 계산과 같은 식
        private static Vector3 GetEclipseDirection(Material material)
        {
            float azimuth = material.GetFloat("_EclipseAzimuth") * Mathf.Deg2Rad;
            float altitude = material.GetFloat("_EclipseAltitude") * Mathf.Deg2Rad;
            return new Vector3(
                Mathf.Cos(altitude) * Mathf.Sin(azimuth),
                Mathf.Sin(altitude),
                Mathf.Cos(altitude) * Mathf.Cos(azimuth));
        }

        private static void ApplyVolume()
        {
            var volumeObject = GameObject.Find(VolumeName);
            if (volumeObject == null)
            {
                volumeObject = new GameObject(VolumeName);
                Undo.RegisterCreatedObjectUndo(volumeObject, "Create Eclipse Volume");
            }

            var volume = volumeObject.GetComponent<Volume>();
            if (volume == null) volume = Undo.AddComponent<Volume>(volumeObject);

            Undo.RecordObject(volume, "Apply Eclipse Volume");
            volume.isGlobal = true;
            volume.priority = 10;
            volume.sharedProfile = LoadOrCreateProfile();
        }

        private static VolumeProfile LoadOrCreateProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile != null) return profile;

            string folder = Path.GetDirectoryName(ProfilePath).Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder) == false)
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(folder).Replace('\\', '/'), Path.GetFileName(folder));
            }

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            // 코로나·빛기둥의 HDR 색이 번지도록
            var bloom = AddComponent<Bloom>(profile);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(1.2f);
            bloom.scatter.Override(0.75f);

            var tonemapping = AddComponent<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);

            // 채도를 빼서 잿빛으로, 붉은 빛만 남게
            var colorAdjustments = AddComponent<ColorAdjustments>(profile);
            colorAdjustments.postExposure.Override(-0.2f);
            colorAdjustments.contrast.Override(18f);
            colorAdjustments.saturation.Override(-20f);

            var vignette = AddComponent<Vignette>(profile);
            vignette.intensity.Override(0.32f);
            vignette.smoothness.Override(0.5f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        // 볼륨 컴포넌트는 프로필 에셋의 하위 에셋으로 저장해야 씬을 다시 열어도 남습니다
        private static T AddComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var component = profile.Add<T>();
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        private static void EnableCameraPostProcessing()
        {
            var camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogWarning("씬에 카메라가 없어 후처리를 켜지 못했습니다. 카메라의 Rendering > Post Processing을 직접 켜세요");
                return;
            }

            var cameraData = camera.GetUniversalAdditionalCameraData();
            Undo.RecordObject(cameraData, "Enable Post Processing");
            cameraData.renderPostProcessing = true;
        }
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BossRaid.EditorTools
{
    // 날개뼈마다 "그 뼈를 따라 움직이는 정점들"을 감싸는 BoxCollider를 자동으로 붙이는 에디터 도구입니다.
    // 사용법: Project 창에서 드래곤 프리팹 선택 → 메뉴 BossRaid > Fit Wing Colliders > Left / Right
    // - 정점의 본 웨이트(가장 크게 영향받는 뼈)로 정점을 뼈별로 나누고, 뼈 로컬 좌표에서 범위를 잽니다.
    // - 이미 콜라이더가 있는 뼈는 건너뜁니다. (손으로 맞춘 콜라이더를 덮어쓰지 않음, 여러 번 실행해도 안전)
    // - 프리팹 모드로 열어둔 상태라면 저장하고 닫은 뒤 실행하세요.
    public static class DragonColliderFitter
    {
        private const string MenuRoot = "BossRaid/Fit Wing Colliders/";
        private const float MinWeight = 0.5f;     // 이 뼈의 영향이 이 값 이상인 정점만 센다
        private const int MinVertexCount = 8;     // 정점이 이보다 적은 뼈는 건너뛴다 (너무 작은 조각)
        private const float Padding = 0.05f;      // 크기를 5% 키워서 살짝 여유를 준다
        private const float MinThickness = 0.15f; // 날개 막처럼 얇은 곳도 최소 이 두께는 준다

        [MenuItem(MenuRoot + "Left")]
        private static void FitLeft() => Fit("WingL");

        [MenuItem(MenuRoot + "Right")]
        private static void FitRight() => Fit("WingR");

        [MenuItem(MenuRoot + "Left", true)]
        [MenuItem(MenuRoot + "Right", true)]
        private static bool ValidateFit()
        {
            return Selection.activeObject is GameObject go && PrefabUtility.IsPartOfPrefabAsset(go);
        }

        private static void Fit(string boneKeyword)
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                Dictionary<Transform, Bounds> boundsByBone = new Dictionary<Transform, Bounds>();
                Dictionary<Transform, int> countByBone = new Dictionary<Transform, int>();

                foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    CollectBounds(renderer, boneKeyword, boundsByBone, countByBone);
                }

                int added = 0;
                foreach (KeyValuePair<Transform, Bounds> pair in boundsByBone)
                {
                    Transform bone = pair.Key;

                    if (bone.GetComponent<Collider>() != null)
                    {
                        Debug.Log($"[ColliderFitter] {bone.name}: 이미 콜라이더가 있어서 건너뜀");
                        continue;
                    }

                    if (countByBone[bone] < MinVertexCount)
                    {
                        Debug.Log($"[ColliderFitter] {bone.name}: 정점 {countByBone[bone]}개라 건너뜀");
                        continue;
                    }

                    Vector3 size = pair.Value.size * (1f + Padding);
                    size = Vector3.Max(size, Vector3.one * MinThickness);

                    BoxCollider box = bone.gameObject.AddComponent<BoxCollider>();
                    box.center = pair.Value.center;
                    box.size = size;

                    added++;
                    Debug.Log($"[ColliderFitter] {bone.name}: 정점 {countByBone[bone]}개 → Box {size}");
                }

                if (added > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[ColliderFitter] {boneKeyword} {added}개 추가 완료: {path}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 정점마다 가장 크게 영향받는 뼈를 찾아서, 그 뼈 로컬 좌표로 바꾼 위치를 뼈별 범위에 넣는다
        private static void CollectBounds(SkinnedMeshRenderer renderer, string boneKeyword,
            Dictionary<Transform, Bounds> boundsByBone, Dictionary<Transform, int> countByBone)
        {
            Mesh mesh = renderer.sharedMesh;
            if (mesh == null) return;

            Vector3[] vertices = mesh.vertices;
            BoneWeight[] weights = mesh.boneWeights;
            Matrix4x4[] bindposes = mesh.bindposes;
            Transform[] bones = renderer.bones;

            if (weights.Length != vertices.Length)
            {
                Debug.LogWarning($"[ColliderFitter] {renderer.name}: 본 웨이트를 읽지 못했습니다.");
                return;
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                BoneWeight weight = weights[i];
                if (weight.weight0 < MinWeight) continue;

                Transform bone = bones[weight.boneIndex0];
                if (bone == null || bone.name.Contains(boneKeyword) == false) continue;

                // bindpose는 "메시 좌표 → 그 뼈의 로컬 좌표" 변환이다
                Vector3 local = bindposes[weight.boneIndex0].MultiplyPoint3x4(vertices[i]);

                if (boundsByBone.TryGetValue(bone, out Bounds bounds))
                {
                    bounds.Encapsulate(local);
                    boundsByBone[bone] = bounds;
                    countByBone[bone]++;
                }
                else
                {
                    boundsByBone[bone] = new Bounds(local, Vector3.zero);
                    countByBone[bone] = 1;
                }
            }
        }
    }
}

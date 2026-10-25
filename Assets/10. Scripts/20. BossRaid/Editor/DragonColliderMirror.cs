using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BossRaid.EditorTools
{
    // 왼쪽 뼈에 붙인 콜라이더를 오른쪽 뼈에 좌우대칭으로 복사하는 에디터 도구입니다.
    // 사용법: Project 창에서 프리팹 선택 → 메뉴 BossRaid > Mirror Colliders L → R
    // - 뼈 이름의 "WingL" → "WingR", " L " → " R " 로 짝을 찾습니다.
    // - 오른쪽 뼈에 이미 같은 종류의 콜라이더가 있으면 건너뜁니다. (여러 번 실행해도 안전)
    // - 프리팹 모드로 열어둔 상태라면 저장하고 닫은 뒤 실행하세요.
    public static class DragonColliderMirror
    {
        private const string MenuPath = "BossRaid/Mirror Colliders L → R";
        private const float SymmetryTolerance = 0.1f; // 좌우 뼈 위치가 이만큼 넘게 어긋나면 경고

        [MenuItem(MenuPath, true)]
        private static bool ValidateMirror()
        {
            return Selection.activeObject is GameObject go && PrefabUtility.IsPartOfPrefabAsset(go);
        }

        [MenuItem(MenuPath)]
        private static void Mirror()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
                foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
                {
                    bones.TryAdd(bone.name, bone);
                }

                int added = 0;
                foreach (Collider source in root.GetComponentsInChildren<Collider>(true))
                {
                    string targetName = GetRightName(source.name);
                    if (targetName == null) continue;

                    if (bones.TryGetValue(targetName, out Transform target) == false)
                    {
                        Debug.LogWarning($"[ColliderMirror] {source.name}의 짝 뼈({targetName})를 찾지 못했습니다.");
                        continue;
                    }

                    if (target.GetComponent(source.GetType()) != null) continue;

                    float gap = Vector3.Distance(MirrorPoint(root.transform, source.transform.position), target.position);
                    if (gap > SymmetryTolerance)
                    {
                        Debug.LogWarning($"[ColliderMirror] {source.name} ↔ {targetName} 위치가 대칭이 아닙니다 ({gap:F2}). 결과를 확인하세요.");
                    }

                    switch (source)
                    {
                        case BoxCollider box:
                            MirrorBox(root.transform, box, target);
                            break;
                        case CapsuleCollider capsule:
                            MirrorCapsule(root.transform, capsule, target);
                            break;
                        case SphereCollider sphere:
                            MirrorSphere(root.transform, sphere, target);
                            break;
                        default:
                            Debug.LogWarning($"[ColliderMirror] {source.GetType().Name}은 지원하지 않습니다: {source.name}");
                            continue;
                    }

                    added++;
                    Debug.Log($"[ColliderMirror] {source.name} → {targetName}");
                }

                if (added > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[ColliderMirror] {added}개 추가 완료: {path}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 왼쪽 뼈 이름이면 오른쪽 뼈 이름을, 아니면 null을 돌려준다
        private static string GetRightName(string leftName)
        {
            if (leftName.Contains("WingL")) return leftName.Replace("WingL", "WingR");
            if (leftName.Contains(" L ")) return leftName.Replace(" L ", " R ");
            return null;
        }

        // 프리팹 루트의 좌우(X축) 기준으로 뒤집는다
        private static Vector3 MirrorPoint(Transform root, Vector3 worldPoint)
        {
            Vector3 local = root.InverseTransformPoint(worldPoint);
            local.x = -local.x;
            return root.TransformPoint(local);
        }

        private static Vector3 MirrorVector(Transform root, Vector3 worldVector)
        {
            Vector3 local = root.InverseTransformVector(worldVector);
            local.x = -local.x;
            return root.TransformVector(local);
        }

        // 왼쪽 뼈 기준 로컬 값을 → 월드 → 좌우 반전 → 오른쪽 뼈 기준 로컬 값으로 바꾼다
        private static Vector3 MirrorCenter(Transform root, Collider source, Vector3 center, Transform target)
        {
            return target.InverseTransformPoint(MirrorPoint(root, source.transform.TransformPoint(center)));
        }

        private static Vector3 MirrorAxis(Transform root, Collider source, Vector3 localAxis, Transform target)
        {
            return target.InverseTransformVector(MirrorVector(root, source.transform.TransformVector(localAxis)));
        }

        private static void MirrorBox(Transform root, BoxCollider box, Transform target)
        {
            Vector3 size = Vector3.zero;
            for (int i = 0; i < 3; i++)
            {
                Vector3 axis = Vector3.zero;
                axis[i] = box.size[i];
                Vector3 mirrored = MirrorAxis(root, box, axis, target);
                size += new Vector3(Mathf.Abs(mirrored.x), Mathf.Abs(mirrored.y), Mathf.Abs(mirrored.z));
            }

            BoxCollider newBox = target.gameObject.AddComponent<BoxCollider>();
            newBox.center = MirrorCenter(root, box, box.center, target);
            newBox.size = size;
            CopyCommon(box, newBox);
        }

        private static void MirrorCapsule(Transform root, CapsuleCollider capsule, Transform target)
        {
            Vector3 dirAxis = Vector3.zero;
            dirAxis[capsule.direction] = 1f;
            Vector3 mirroredDir = MirrorAxis(root, capsule, dirAxis, target);

            Vector3 sideAxis = Vector3.zero;
            sideAxis[(capsule.direction + 1) % 3] = 1f;
            Vector3 mirroredSide = MirrorAxis(root, capsule, sideAxis, target);

            CapsuleCollider newCapsule = target.gameObject.AddComponent<CapsuleCollider>();
            newCapsule.center = MirrorCenter(root, capsule, capsule.center, target);
            newCapsule.direction = LargestAxis(mirroredDir);
            newCapsule.height = capsule.height * mirroredDir.magnitude;
            newCapsule.radius = capsule.radius * mirroredSide.magnitude;
            CopyCommon(capsule, newCapsule);
        }

        private static void MirrorSphere(Transform root, SphereCollider sphere, Transform target)
        {
            Vector3 mirroredAxis = MirrorAxis(root, sphere, Vector3.right, target);

            SphereCollider newSphere = target.gameObject.AddComponent<SphereCollider>();
            newSphere.center = MirrorCenter(root, sphere, sphere.center, target);
            newSphere.radius = sphere.radius * mirroredAxis.magnitude;
            CopyCommon(sphere, newSphere);
        }

        private static int LargestAxis(Vector3 v)
        {
            float x = Mathf.Abs(v.x), y = Mathf.Abs(v.y), z = Mathf.Abs(v.z);
            if (x >= y && x >= z) return 0;
            return y >= z ? 1 : 2;
        }

        private static void CopyCommon(Collider source, Collider target)
        {
            target.isTrigger = source.isTrigger;
            target.sharedMaterial = source.sharedMaterial;
            target.enabled = source.enabled;
        }
    }
}

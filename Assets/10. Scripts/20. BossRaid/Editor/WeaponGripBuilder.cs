using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BossRaid.EditorTools
{
    // Grruzam 애니메이션은 데모 캐릭터의 ik_hand_gun 본에 칼을 붙인 상태로 만들어졌습니다.
    // 그 손잡이 위치/각도를 플레이어(Synty) 손으로 옮기고, Synty 대검의 코등이를 Grruzam 칼의 코등이에 맞춰 쥐여주는 툴입니다.
    // 상단 메뉴 > Tools > BossRaid > Attach Greatsword To Hand
    //
    // 결과 구조
    // Hand_R
    //  └ WeaponSocket_R                    (손잡이 위치. 스케일 1 = 1m 로 맞춰둠)
    //     └ SM_Wep_Greatsword_Straight_01  (코등이 정렬만 들어간 칼)
    public static class WeaponGripBuilder
    {
        private const string ReferencePath =
            "Assets/99. External Assets/Grruzam Powerful Sword Animation(Great Sword, Katana)/Prefebs/Unity_Grruzam_BaseModeling_GreatSword.prefab";
        private const string ReferenceSocketName = "ik_hand_gun";
        private const string SwordPath = "Assets/99. External Assets/Synty 1/PolygonDungeon/Models/SM_Wep_Greatsword_Straight_01.fbx";
        private const string SocketName = "WeaponSocket_R";
        private const int SliceCount = 64; // 칼을 길이 방향으로 잘라 코등이(가장 넓은 단면)를 찾을 때 쓰는 칸 수

        // 칼 메시를 분석한 결과입니다. 모두 주어진 기준 공간의 값입니다.
        private struct BladeShape
        {
            public Vector3 Blade;  // 코등이 -> 칼끝 방향
            public Vector3 Width;  // 코등이가 뻗은 방향 (칼날 폭)
            public Vector3 Guard;  // 코등이 중심
            public float Length;
        }

        [MenuItem("Tools/BossRaid/Attach Greatsword To Hand")]
        private static void Attach()
        {
            ThirdPersonController player = Object.FindAnyObjectByType<ThirdPersonController>();
            Animator animator = player != null ? player.GetComponentInChildren<Animator>() : null;
            if (animator == null || !animator.isHuman)
            {
                Debug.LogWarning("[WeaponGripBuilder] 씬에서 Humanoid Animator를 가진 플레이어를 찾지 못했습니다.");
                return;
            }

            GameObject referencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ReferencePath);
            GameObject swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwordPath);
            if (referencePrefab == null || swordPrefab == null)
            {
                Debug.LogError("[WeaponGripBuilder] 기준 프리팹 또는 칼 FBX를 찾을 수 없습니다.");
                return;
            }

            Undo.SetCurrentGroupName("Attach Greatsword To Hand");
            int undoGroup = Undo.GetCurrentGroup();

            // 계산용으로 Grruzam 데모 캐릭터를 잠깐 꺼냅니다. 씬에는 저장되지 않습니다.
            GameObject reference = Object.Instantiate(referencePrefab);
            reference.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Animator refAnimator = reference.GetComponentInChildren<Animator>();
                Transform refHand = refAnimator.GetBoneTransform(HumanBodyBones.RightHand);
                Transform refSocket = FindDeep(refHand, ReferenceSocketName);
                MeshFilter refSword = refSocket != null ? refSocket.GetComponentInChildren<MeshFilter>() : null;
                if (refSword == null)
                {
                    Debug.LogError($"[WeaponGripBuilder] 기준 프리팹에서 {ReferenceSocketName} / 칼 메시를 찾지 못했습니다.");
                    return;
                }

                // 두 캐릭터를 같은 Humanoid 자세로 맞추면 손의 실제 방향이 같아집니다.
                // (본의 로컬 축은 리그마다 달라서 로컬 값을 그대로 복사하면 칼이 엉뚱한 방향을 봅니다)
                MatchPose(animator, refAnimator);

                Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                float armRatio = ArmLength(animator) / ArmLength(refAnimator);

                Transform socket = GetOrCreateSocket(hand);
                Undo.RecordObject(socket, "Attach Greatsword To Hand");
                socket.SetPositionAndRotation(
                    hand.position + (refSocket.position - refHand.position) * armRatio,
                    refSocket.rotation);
                // 손 본에 걸린 스케일(cm 리그면 0.01)을 상쇄해서 소켓 밑은 1 = 1m 가 되게 합니다
                socket.localScale = Vector3.one / hand.lossyScale.x;

                Transform sword = GetOrCreateSword(hand, socket, swordPrefab);
                AlignSword(sword, refSword, refSocket);
            }
            finally
            {
                Object.DestroyImmediate(reference);
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        }

        // 플레이어의 지금 자세(머슬 값)를 기준 캐릭터에 그대로 적용합니다.
        private static void MatchPose(Animator source, Animator target)
        {
            target.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);

            HumanPose pose = new HumanPose();
            using (HumanPoseHandler sourceHandler = new HumanPoseHandler(source.avatar, source.transform))
                sourceHandler.GetHumanPose(ref pose);
            using (HumanPoseHandler targetHandler = new HumanPoseHandler(target.avatar, target.transform))
                targetHandler.SetHumanPose(ref pose);
        }

        // 손잡이 오프셋을 캐릭터 체격에 맞게 늘리거나 줄이는 데 씁니다.
        private static float ArmLength(Animator animator)
        {
            Vector3 upper = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position;
            Vector3 lower = animator.GetBoneTransform(HumanBodyBones.RightLowerArm).position;
            Vector3 hand = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
            return Vector3.Distance(upper, lower) + Vector3.Distance(lower, hand);
        }

        private static Transform GetOrCreateSocket(Transform hand)
        {
            Transform socket = hand.Find(SocketName);
            if (socket != null) return socket;

            GameObject go = new GameObject(SocketName);
            Undo.RegisterCreatedObjectUndo(go, "Attach Greatsword To Hand");
            Undo.SetTransformParent(go.transform, hand, "Attach Greatsword To Hand");
            return go.transform;
        }

        // 손 밑에 이미 넣어둔 칼이 있으면 소켓 밑으로 옮기고, 없으면 새로 만듭니다.
        private static Transform GetOrCreateSword(Transform hand, Transform socket, GameObject swordPrefab)
        {
            foreach (Transform child in hand.GetComponentsInChildren<Transform>(true))
            {
                if (PrefabUtility.GetCorrespondingObjectFromOriginalSource(child.gameObject) != swordPrefab) continue;
                if (child.parent != socket) Undo.SetTransformParent(child, socket, "Attach Greatsword To Hand");
                return child;
            }

            GameObject sword = (GameObject)PrefabUtility.InstantiatePrefab(swordPrefab, socket);
            Undo.RegisterCreatedObjectUndo(sword, "Attach Greatsword To Hand");
            return sword.transform;
        }

        // Synty 칼의 날 방향 / 폭 방향 / 코등이 위치를 Grruzam 칼과 똑같이 맞춥니다.
        private static void AlignSword(Transform sword, MeshFilter refSword, Transform refSocket)
        {
            MeshFilter swordMesh = sword.GetComponentInChildren<MeshFilter>();
            if (swordMesh == null)
            {
                Debug.LogError("[WeaponGripBuilder] 칼에서 MeshFilter를 찾지 못했습니다.");
                return;
            }

            // 기준 칼은 ik_hand_gun 기준(1 = 1m)으로 봅니다.
            Quaternion toRefSocket = Quaternion.Inverse(refSocket.rotation);
            BladeShape target = Analyze(refSword, p => toRefSocket * (p - refSocket.position));

            // Synty 칼은 소켓 밑에서 초기화한 뒤 자기 로컬 기준으로 봅니다.
            Undo.RecordObject(sword, "Attach Greatsword To Hand");
            sword.localPosition = Vector3.zero;
            sword.localRotation = Quaternion.identity;
            sword.localScale = Vector3.one;
            BladeShape source = Analyze(swordMesh, sword.InverseTransformPoint);

            Quaternion rotation = Quaternion.LookRotation(target.Blade, target.Width)
                                  * Quaternion.Inverse(Quaternion.LookRotation(source.Blade, source.Width));
            sword.localRotation = rotation;
            sword.localPosition = target.Guard - rotation * source.Guard;

            Debug.Log($"[WeaponGripBuilder] 칼 장착 완료. 길이: Synty {source.Length:F2}m / Grruzam 기준 {target.Length:F2}m. " +
                      "세부 각도는 WeaponSocket_R 을 돌려서 조정하세요. 씬을 저장(Ctrl+S)해 주세요.");
        }

        // 메시 버텍스로 칼 모양을 분석합니다.
        // 가장 긴 축 = 칼 길이, 그 축으로 잘랐을 때 가장 넓은 단면 = 코등이, 코등이에서 더 멀리 뻗은 쪽 = 칼날.
        private static BladeShape Analyze(MeshFilter meshFilter, Func<Vector3, Vector3> toSpace)
        {
            Vector3[] vertices = meshFilter.sharedMesh.vertices;
            Vector3[] points = new Vector3[vertices.Length];
            Bounds bounds = new Bounds(toSpace(meshFilter.transform.TransformPoint(vertices[0])), Vector3.zero);
            for (int i = 0; i < vertices.Length; i++)
            {
                points[i] = toSpace(meshFilter.transform.TransformPoint(vertices[i]));
                bounds.Encapsulate(points[i]);
            }

            Vector3 size = bounds.size;
            int lengthAxis = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
            int a = (lengthAxis + 1) % 3, b = (lengthAxis + 2) % 3;
            int widthAxis = size[a] >= size[b] ? a : b;

            float min = bounds.min[lengthAxis];
            float length = size[lengthAxis];
            float[] sliceMin = new float[SliceCount];
            float[] sliceMax = new float[SliceCount];
            for (int s = 0; s < SliceCount; s++)
            {
                sliceMin[s] = float.MaxValue;
                sliceMax[s] = float.MinValue;
            }

            foreach (Vector3 p in points)
            {
                int s = Mathf.Clamp((int)((p[lengthAxis] - min) / length * SliceCount), 0, SliceCount - 1);
                sliceMin[s] = Mathf.Min(sliceMin[s], p[widthAxis]);
                sliceMax[s] = Mathf.Max(sliceMax[s], p[widthAxis]);
            }

            int guardSlice = 0;
            for (int s = 1; s < SliceCount; s++)
            {
                if (sliceMax[s] - sliceMin[s] > sliceMax[guardSlice] - sliceMin[guardSlice]) guardSlice = s;
            }

            float guard = min + (guardSlice + 0.5f) / SliceCount * length;
            float bladeSign = bounds.max[lengthAxis] - guard > guard - min ? 1f : -1f;

            Vector3 guardPoint = bounds.center;
            guardPoint[lengthAxis] = guard;

            Vector3 blade = Vector3.zero;
            blade[lengthAxis] = bladeSign;
            Vector3 width = Vector3.zero;
            width[widthAxis] = 1f;

            return new BladeShape { Blade = blade, Width = width, Guard = guardPoint, Length = length };
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child;
            }
            return null;
        }
    }
}

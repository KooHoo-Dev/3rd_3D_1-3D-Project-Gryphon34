using System.Collections.Generic;
using BossRaid.Boss;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BossRaid.Tools
{
    // 할퀴기 판정(L_Hand / R_Hand)을 발톱 궤도에 맞춘 고정 상자로 바꾸는 메뉴
    // : 할퀴기 클립을 HitboxOn ~ HitboxOff 구간만 돌려 보며 손·발톱 끝 위치를 모으고,
    //   그 점들을 감싸는 기울인 상자를 용 루트 밑에 놓습니다 (Variant 프리팹에 저장)
    // 다시 실행하면 같은 결과로 덮어씁니다. 상자를 손으로 고친 뒤에는 실행하지 마세요.
    public static class ClawHitboxFitter
    {
        private const string VariantPath = "Assets/00. Study/20. BossRaid/MountainDragon_Stylized Variant.prefab";
        private const string ControllerPath = "Assets/00. Study/20. BossRaid/DragonBoss.controller";

        private const int SampleCount = 40;   // 구간 안에서 몇 번 멈춰 볼지
        private const float Padding = 0.25f;  // 점들 바깥으로 더 두를 두께 (발톱 굵기)
        private const float TipExtend = 1f;   // 마지막 손가락 마디 길이만큼 더 나간 곳 = 발톱 끝

        private struct BoxFit
        {
            public Vector3 center;   // 루트 기준 위치
            public Quaternion rotation;
            public Vector3 size;
        }

        [MenuItem("BossRaid/Boss/Fit Claw Hitboxes")]
        private static void Fit()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (asset == null || controller == null)
            {
                Debug.LogError("Variant 프리팹이나 DragonBoss.controller를 찾지 못했습니다");
                return;
            }

            // 1. 잠깐 쓸 복사본에서 클립을 돌려 점을 모읍니다 (프리팹 자체의 뼈 자세는 건드리지 않게)
            GameObject temp = Object.Instantiate(asset);
            temp.hideFlags = HideFlags.HideAndDontSave;

            BoxFit right, left;
            List<Vector3> comboRight, comboLeft;
            try
            {
                List<Vector3> rightPoints = SampleState(temp, controller, "ClawsAttackRight", "R_Hand", "R");
                List<Vector3> leftPoints = SampleState(temp, controller, "ClawsAttackLeft", "L_Hand", "L");
                comboRight = SampleState(temp, controller, "ClawsAttack2HitComboForward", "R_Hand", "R");
                comboLeft = SampleState(temp, controller, "ClawsAttack2HitComboForward", "L_Hand", "L");

                if (rightPoints.Count == 0 || leftPoints.Count == 0)
                {
                    Debug.LogError("할퀴기 클립에서 점을 모으지 못했습니다 (칸 이름이나 HitboxOn/Off 이벤트 확인)");
                    return;
                }

                right = FitBox(rightPoints);
                left = FitBox(leftPoints);
            }
            finally
            {
                Object.DestroyImmediate(temp);
            }

            // 2. Variant를 열어 L_Hand / R_Hand를 상자로 바꾸고 저장합니다
            GameObject contents = PrefabUtility.LoadPrefabContents(VariantPath);
            try
            {
                foreach (BossHitbox hitbox in contents.GetComponentsInChildren<BossHitbox>(true))
                {
                    if (hitbox.id == "R_Hand") ApplyBox(hitbox, contents.transform, right);
                    if (hitbox.id == "L_Hand") ApplyBox(hitbox, contents.transform, left);
                }
                PrefabUtility.SaveAsPrefabAsset(contents, VariantPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            // 3. 2연타가 같은 상자 안으로 지나가는지 (클립을 이어 붙인 거라면 거의 100%)
            Debug.Log($"[ClawHitboxFitter] 2연타 오른발 점 중 상자 안: {InsideRate(comboRight, right):P0}, 왼발: {InsideRate(comboLeft, left):P0}");
        }

        // stateName 칸의 클립을 parameter(R_Hand 등)의 HitboxOn ~ HitboxOff 구간만 돌리며 손·발톱 끝 위치를 모읍니다 (루트 기준)
        private static List<Vector3> SampleState(GameObject root, AnimatorController controller, string stateName, string parameter, string side)
        {
            List<Vector3> points = new List<Vector3>();

            AnimatorState state = FindState(controller.layers[0].stateMachine, stateName);
            AnimationClip clip = state != null ? state.motion as AnimationClip : null;
            if (clip == null)
            {
                Debug.LogWarning($"[ClawHitboxFitter] '{stateName}' 칸이나 클립이 없습니다");
                return points;
            }

            List<Vector2> windows = GetHitboxWindows(state, parameter);
            Transform hand = FindDeep(root.transform, $"MountainDragon_ {side} Hand");
            Animator animator = root.GetComponentInChildren<Animator>();
            if (hand == null || animator == null) return points;

            foreach (Vector2 window in windows)
            {
                for (int i = 0; i <= SampleCount; i++)
                {
                    float normalized = Mathf.Lerp(window.x, window.y, i / (float)SampleCount);
                    clip.SampleAnimation(animator.gameObject, normalized * clip.length);

                    points.Add(root.transform.InverseTransformPoint(hand.position));
                    for (int f = 0; f < 4; f++)
                    {
                        Transform finger = FindDeep(hand, $"MountainDragon_ {side} Finger{f}");
                        Transform fingerEnd = FindDeep(hand, $"MountainDragon_ {side} Finger{f}1");
                        if (finger == null || fingerEnd == null) continue;

                        Vector3 tip = fingerEnd.position + (fingerEnd.position - finger.position) * TipExtend;
                        points.Add(root.transform.InverseTransformPoint(fingerEnd.position));
                        points.Add(root.transform.InverseTransformPoint(tip));
                    }
                }
            }
            return points;
        }

        // 칸에 붙은 AnimStateEventSender에서 parameter가 같은 HitboxOn → HitboxOff 짝을 (켬, 끔) 진행률로 꺼냅니다
        internal static List<Vector2> GetHitboxWindows(AnimatorState state, string parameter)
        {
            List<Vector2> windows = new List<Vector2>();
            foreach (StateMachineBehaviour behaviour in state.behaviours)
            {
                if (behaviour is not AnimStateEventSender) continue;

                SerializedProperty events = new SerializedObject(behaviour).FindProperty("events");
                float onTime = -1f;
                for (int i = 0; i < events.arraySize; i++)
                {
                    SerializedProperty e = events.GetArrayElementAtIndex(i);
                    if (e.FindPropertyRelative("parameter").stringValue != parameter) continue;

                    string name = e.FindPropertyRelative("name").stringValue;
                    float time = e.FindPropertyRelative("time").floatValue;
                    if (name == "HitboxOn") onTime = time;
                    if (name == "HitboxOff" && onTime >= 0f)
                    {
                        windows.Add(new Vector2(onTime, time));
                        onTime = -1f;
                    }
                }
            }
            return windows;
        }

        // 점들이 가장 길게 퍼진 방향(주성분)을 상자의 z, 그 다음을 y로 삼아 감쌉니다
        private static BoxFit FitBox(List<Vector3> points)
        {
            Vector3 mean = Vector3.zero;
            foreach (Vector3 p in points) mean += p;
            mean /= points.Count;

            float[,] cov = new float[3, 3];
            foreach (Vector3 p in points)
            {
                Vector3 d = p - mean;
                for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 3; c++)
                        cov[r, c] += d[r] * d[c];
            }

            Vector3 axisLong = PowerIteration(cov, Vector3.forward);
            float lambda = Vector3.Dot(axisLong, Multiply(cov, axisLong));
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    cov[r, c] -= lambda * axisLong[r] * axisLong[c];
            Vector3 axisMid = PowerIteration(cov, Vector3.up - Vector3.Dot(Vector3.up, axisLong) * axisLong);
            axisMid = (axisMid - Vector3.Dot(axisMid, axisLong) * axisLong).normalized;

            Quaternion rotation = Quaternion.LookRotation(axisLong, axisMid);
            Quaternion inverse = Quaternion.Inverse(rotation);

            Vector3 min = Vector3.one * float.MaxValue;
            Vector3 max = Vector3.one * float.MinValue;
            foreach (Vector3 p in points)
            {
                Vector3 local = inverse * p;
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }

            return new BoxFit
            {
                center = rotation * ((min + max) / 2f),
                rotation = rotation,
                size = max - min + Vector3.one * (Padding * 2f),
            };
        }

        private static Vector3 PowerIteration(float[,] m, Vector3 start)
        {
            Vector3 v = start.sqrMagnitude > 0.0001f ? start.normalized : Vector3.right;
            for (int i = 0; i < 64; i++)
            {
                Vector3 next = Multiply(m, v);
                if (next.sqrMagnitude < 1e-12f) break;
                v = next.normalized;
            }
            return v;
        }

        private static Vector3 Multiply(float[,] m, Vector3 v)
        {
            return new Vector3(
                m[0, 0] * v.x + m[0, 1] * v.y + m[0, 2] * v.z,
                m[1, 0] * v.x + m[1, 1] * v.y + m[1, 2] * v.z,
                m[2, 0] * v.x + m[2, 1] * v.y + m[2, 2] * v.z);
        }

        private static void ApplyBox(BossHitbox hitbox, Transform root, BoxFit fit)
        {
            Transform t = hitbox.transform;
            Debug.Log($"[ClawHitboxFitter] {hitbox.id} 전 값: 부모 {t.parent.name}, 위치 {t.localPosition}, 회전 {t.localEulerAngles}, 크기 {t.localScale}");

            t.SetParent(root, false);
            t.localPosition = fit.center;
            t.localRotation = fit.rotation;
            t.localScale = Vector3.one;

            foreach (Collider old in t.GetComponents<Collider>())
                if (old is not BoxCollider) Object.DestroyImmediate(old);

            BoxCollider box = t.GetComponent<BoxCollider>();
            if (box == null) box = t.gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = fit.size;

            Debug.Log($"[ClawHitboxFitter] {hitbox.id} 새 상자: 위치 {fit.center}, 회전 {fit.rotation.eulerAngles}, 크기 {fit.size}");
        }

        private static float InsideRate(List<Vector3> points, BoxFit fit)
        {
            if (points.Count == 0) return 0f;
            Quaternion inverse = Quaternion.Inverse(fit.rotation);
            Vector3 half = fit.size / 2f;
            int inside = 0;
            foreach (Vector3 p in points)
            {
                Vector3 local = inverse * (p - fit.center);
                if (Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z) inside++;
            }
            return inside / (float)points.Count;
        }

        private static AnimatorState FindState(AnimatorStateMachine machine, string stateName)
        {
            foreach (ChildAnimatorState child in machine.states)
                if (child.state.name == stateName) return child.state;
            foreach (ChildAnimatorStateMachine sub in machine.stateMachines)
            {
                AnimatorState found = FindState(sub.stateMachine, stateName);
                if (found != null) return found;
            }
            return null;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                Transform found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}

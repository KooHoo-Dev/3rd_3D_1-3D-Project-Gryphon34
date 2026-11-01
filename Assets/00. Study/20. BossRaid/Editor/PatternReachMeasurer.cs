using System.Collections.Generic;
using System.Text;
using BossRaid.Boss;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BossRaid.Tools
{
    // 패턴마다 "판정이 켜진 동안 판정이 용 기준 어디까지 닿나"를 재서 Console에 표로 찍는 메뉴 (Decide 값 정하기용)
    // : 애니메이터 칸마다 HitboxOn ~ HitboxOff 구간을 돌려 보며, 판정 콜라이더 겉면의 점들을 용 루트 기준 거리·각도로 모읍니다
    // 거리·각도는 Decide와 같은 방식 (용 앞 = 0°, 오른쪽 = +)
    // 줄마다 두 번: "전체" = 높이 상관없이 / "플레이어 높이" = 땅 ~ PlayerTop 사이에 있는 판정만 (머리 위로 지나가는 건 뺌)
    // 주의: HitboxOn 전까지 플레이어 쪽으로 도는 회전(Attack)은 안 들어감 → 실제 각도는 이것보다 넓음
    public static class PatternReachMeasurer
    {
        private const string VariantPath = "Assets/00. Study/20. BossRaid/MountainDragon_Stylized Variant.prefab";
        private const string ControllerPath = "Assets/00. Study/20. BossRaid/DragonBoss.controller";
        private const int SampleCount = 30;
        private const float PlayerTop = 1.85f; // 플레이어 캡슐 꼭대기 높이 (ModularCharacters: 중심 0.88 + 높이 1.91의 절반)

        [MenuItem("BossRaid/Boss/Measure Pattern Reach")]
        private static void Measure()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (asset == null || controller == null)
            {
                Debug.LogError("Variant 프리팹이나 DragonBoss.controller를 찾지 못했습니다");
                return;
            }

            // 잠깐 쓸 복사본에서 돌립니다 (프리팹 자체의 뼈 자세는 건드리지 않게)
            GameObject temp = Object.Instantiate(asset);
            temp.hideFlags = HideFlags.HideAndDontSave;

            StringBuilder table = new StringBuilder("[PatternReach] 칸 | 판정 id | 구간 | 전체: 거리 (가까운 끝 ~ 먼 끝), 각도 | 플레이어 높이: 거리, 각도\n");
            try
            {
                Animator animator = temp.GetComponentInChildren<Animator>();
                BossHitbox[] hitboxes = temp.GetComponentsInChildren<BossHitbox>(true);

                List<AnimatorState> states = new List<AnimatorState>();
                CollectStates(controller.layers[0].stateMachine, states);

                foreach (AnimatorState state in states)
                {
                    AnimationClip clip = state.motion as AnimationClip;
                    if (clip == null) continue;

                    foreach (string id in GetHitboxIds(state))
                    {
                        Collider collider = FindCollider(hitboxes, id);
                        if (collider == null)
                        {
                            table.AppendLine($"{state.name} | {id} | 판정 오브젝트 없음");
                            continue;
                        }

                        foreach (Vector2 window in ClawHitboxFitter.GetHitboxWindows(state, id))
                        {
                            List<Vector3> points = new List<Vector3>();
                            List<Vector3> playerHeightPoints = new List<Vector3>();
                            for (int i = 0; i <= SampleCount; i++)
                            {
                                float normalized = Mathf.Lerp(window.x, window.y, i / (float)SampleCount);
                                clip.SampleAnimation(animator.gameObject, normalized * clip.length);
                                foreach (Vector3 p in VolumePoints(collider))
                                {
                                    Vector3 local = temp.transform.InverseTransformPoint(p);
                                    points.Add(local);
                                    if (local.y >= 0f && local.y <= PlayerTop) playerHeightPoints.Add(local);
                                }
                            }
                            table.AppendLine($"{state.name} | {id} | {window.x:0.000}~{window.y:0.000} (켜질 때까지 {window.x * clip.length:0.00}초) | 전체: {Describe(points)} | 플레이어 높이: {Describe(playerHeightPoints)}");
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(temp);
            }

            table.AppendLine("(브레스는 다 뻗은 길이 기준. 불덩이는 판정이 투사체라 표에 없음. 뒤 돌아 할퀴기 _Claw 칸은 이미 돈 뒤의 용 기준)");
            table.AppendLine("(켜질 때까지 초 × 그 상태의 Rotation Speed = 판정 전에 플레이어 쪽으로 더 돌 수 있는 최대 각도)");
            Debug.Log(table.ToString());
        }

        // Death 클립을 진행률 0~1로 멈춰 보며 머리·골반 높이를 찍음 → 높이가 확 떨어지는 구간 = 쓰러지는 동작 (지침 일어나기 Offset·Speed 정하기용)
        [MenuItem("BossRaid/Boss/Measure Death Clip Height")]
        private static void MeasureDeathHeight()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            List<AnimatorState> states = new List<AnimatorState>();
            CollectStates(controller.layers[0].stateMachine, states);
            AnimationClip clip = null;
            foreach (AnimatorState state in states)
                if (state.name == "Death") clip = state.motion as AnimationClip;
            if (asset == null || clip == null)
            {
                Debug.LogError("Variant 프리팹이나 Death 클립을 찾지 못했습니다");
                return;
            }

            GameObject temp = Object.Instantiate(asset);
            temp.hideFlags = HideFlags.HideAndDontSave;
            StringBuilder table = new StringBuilder($"[DeathHeight] Death 클립 {clip.length:0.00}초 | 진행률 | 초 | 머리 높이 | 골반 높이\n");
            try
            {
                Animator animator = temp.GetComponentInChildren<Animator>();
                Transform head = null, pelvis = null;
                foreach (Transform t in temp.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "MountainDragon_ Head") head = t;
                    if (t.name == "MountainDragon_ Pelvis") pelvis = t;
                }
                for (int i = 0; i <= 30; i++)
                {
                    float normalized = i / 30f;
                    clip.SampleAnimation(animator.gameObject, normalized * clip.length);
                    string headY = head != null ? temp.transform.InverseTransformPoint(head.position).y.ToString("0.00") : "-";
                    string pelvisY = pelvis != null ? temp.transform.InverseTransformPoint(pelvis.position).y.ToString("0.00") : "-";
                    table.AppendLine($"{normalized:0.00} | {normalized * clip.length:0.00} | {headY} | {pelvisY}");
                }
            }
            finally
            {
                Object.DestroyImmediate(temp);
            }
            Debug.Log(table.ToString());
        }

        private static string Describe(List<Vector3> points)
        {
            if (points.Count == 0) return "안 닿음";

            float near = float.MaxValue, far = 0f;
            List<float> angles = new List<float>();
            foreach (Vector3 p in points)
            {
                Vector3 flat = new Vector3(p.x, 0f, p.z);
                float distance = flat.magnitude;
                near = Mathf.Min(near, distance);
                far = Mathf.Max(far, distance);
                if (distance > 0.01f) angles.Add(Vector3.SignedAngle(Vector3.forward, flat, Vector3.up));
            }

            float min = float.MaxValue, max = float.MinValue;
            foreach (float a in angles) { min = Mathf.Min(min, a); max = Mathf.Max(max, a); }

            // 뒤쪽(±180)을 걸치면 -170~170처럼 거꾸로 넓게 나오므로 0~360으로 다시 봅니다
            if (max - min > 180f)
            {
                float min360 = float.MaxValue, max360 = float.MinValue;
                foreach (float a in angles)
                {
                    float b = a < 0f ? a + 360f : a;
                    min360 = Mathf.Min(min360, b);
                    max360 = Mathf.Max(max360, b);
                }
                if (max360 - min360 < max - min)
                    return $"{near:0.0} ~ {far:0.0}m | {min360:0}° ~ {max360:0}° (180 넘는 쪽 = 왼쪽 뒤, -360 해서 읽기)";
            }
            return $"{near:0.0} ~ {far:0.0}m | {min:0}° ~ {max:0}°";
        }

        // 콜라이더 안을 촘촘히 채운 점들 (월드 좌표). 높이로 거를 때 겉면만 보면 띠 사이를 놓쳐서 안쪽까지 채웁니다
        private static IEnumerable<Vector3> VolumePoints(Collider collider)
        {
            Transform t = collider.transform;
            if (collider is BoxCollider box)
            {
                const int n = 6; // 축마다 7칸
                Vector3 half = box.size / 2f;
                for (int x = 0; x <= n; x++)
                    for (int y = 0; y <= n; y++)
                        for (int z = 0; z <= n; z++)
                        {
                            Vector3 r = new Vector3(x, y, z) * (2f / n) - Vector3.one;
                            yield return t.TransformPoint(box.center + Vector3.Scale(half, r));
                        }
            }
            else if (collider is CapsuleCollider capsule)
            {
                Vector3 axis = capsule.direction == 0 ? Vector3.right : capsule.direction == 1 ? Vector3.up : Vector3.forward;
                Vector3 side1 = capsule.direction == 0 ? Vector3.up : Vector3.right;
                Vector3 side2 = Vector3.Cross(axis, side1);
                float segment = Mathf.Max(0f, capsule.height / 2f - capsule.radius);

                // 가운데 원기둥: 축 방향 9칸 × 반지름 3겹 × 12방향
                for (int i = -4; i <= 4; i++)
                {
                    Vector3 c = capsule.center + axis * (segment * i / 4f);
                    yield return t.TransformPoint(c);
                    for (int layer = 1; layer <= 3; layer++)
                        for (int k = 0; k < 12; k++)
                        {
                            float rad = k * Mathf.PI / 6f;
                            yield return t.TransformPoint(c + (side1 * Mathf.Cos(rad) + side2 * Mathf.Sin(rad)) * (capsule.radius * layer / 3f));
                        }
                }
                // 양 끝 반구: 위·아래 몇 단
                for (int end = -1; end <= 1; end += 2)
                    for (int ring = 1; ring <= 3; ring++)
                    {
                        float a = ring * Mathf.PI / 8f; // 22.5°, 45°, 67.5°
                        Vector3 c = capsule.center + axis * end * (segment + capsule.radius * Mathf.Sin(a));
                        for (int k = 0; k < 12; k++)
                        {
                            float rad = k * Mathf.PI / 6f;
                            yield return t.TransformPoint(c + (side1 * Mathf.Cos(rad) + side2 * Mathf.Sin(rad)) * (capsule.radius * Mathf.Cos(a)));
                        }
                    }
            }
            else
            {
                Bounds b = collider.bounds;
                yield return b.min;
                yield return b.max;
            }
        }

        private static List<string> GetHitboxIds(AnimatorState state)
        {
            List<string> ids = new List<string>();
            foreach (StateMachineBehaviour behaviour in state.behaviours)
            {
                if (behaviour is not AnimStateEventSender) continue;
                SerializedProperty events = new SerializedObject(behaviour).FindProperty("events");
                for (int i = 0; i < events.arraySize; i++)
                {
                    SerializedProperty e = events.GetArrayElementAtIndex(i);
                    string id = e.FindPropertyRelative("parameter").stringValue;
                    if (e.FindPropertyRelative("name").stringValue == "HitboxOn" && ids.Contains(id) == false) ids.Add(id);
                }
            }
            return ids;
        }

        private static Collider FindCollider(BossHitbox[] hitboxes, string id)
        {
            foreach (BossHitbox hitbox in hitboxes)
                if (hitbox.id == id) return hitbox.GetComponent<Collider>();
            return null;
        }

        private static void CollectStates(AnimatorStateMachine machine, List<AnimatorState> states)
        {
            foreach (ChildAnimatorState child in machine.states) states.Add(child.state);
            foreach (ChildAnimatorStateMachine sub in machine.stateMachines) CollectStates(sub.stateMachine, states);
        }
    }
}

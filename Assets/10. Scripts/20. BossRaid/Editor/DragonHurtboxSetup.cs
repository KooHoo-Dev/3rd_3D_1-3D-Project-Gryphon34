using System.Collections.Generic;
using BossRaid.Boss;
using UnityEditor;
using UnityEngine;

namespace BossRaid.EditorTools
{
    // 드래곤 프리팹의 콜라이더들에 Hurtbox를 달고, 부위별 BossPart를 붙여 연결하는 에디터 도구입니다.
    // 사용법: Project 창에서 드래곤 프리팹 선택 → 메뉴 BossRaid > Setup Dragon Hurtboxes
    // - 부위 뼈(PartBones)에 BossPart가 없으면 붙인다
    // - Trigger가 아닌 콜라이더마다 Hurtbox를 달고, 레이어를 BossHurtbox로 바꾼다
    // - 각 Hurtbox는 위쪽에서 가장 가까운 BossPart에 연결한다. 없으면(예: 목) "Body" 부위에 연결한다
    // - 이미 있는 건 건드리지 않으므로 여러 번 실행해도 안전합니다. 콜라이더를 더 붙인 뒤 다시 실행하면 됩니다.
    // - 프리팹 모드로 열어둔 상태라면 저장하고 닫은 뒤 실행하세요.
    public static class DragonHurtboxSetup
    {
        private const string MenuPath = "BossRaid/Setup Dragon Hurtboxes";
        private const string HurtboxLayerName = "BossHurtbox";
        private const string BodyObjectName = "Body"; // 어느 부위에도 속하지 않는 콜라이더가 연결될 부위

        // 부위의 가장 위쪽 뼈 → 부위 이름
        private static readonly Dictionary<string, string> PartBones = new Dictionary<string, string>
        {
            { "MountainDragon_ L UpperArm", "FrontLeg_L" },
            { "MountainDragon_ R UpperArm", "FrontLeg_R" },
            { "MountainDragon_ L Thigh", "HindLeg_L" },
            { "MountainDragon_ R Thigh", "HindLeg_R" },
            { "MountainDragon_WingL_Arm", "Wing_L" },
            { "MountainDragon_WingR_Arm", "Wing_R" },
            { "MountainDragon_ Tail", "Tail" },
        };

        [MenuItem(MenuPath, true)]
        private static bool ValidateSetup()
        {
            return Selection.activeObject is GameObject go && PrefabUtility.IsPartOfPrefabAsset(go);
        }

        [MenuItem(MenuPath)]
        private static void Setup()
        {
            int hurtboxLayer = LayerMask.NameToLayer(HurtboxLayerName);
            if (hurtboxLayer < 0)
            {
                Debug.LogError($"[HurtboxSetup] {HurtboxLayerName} 레이어가 없습니다.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                Boss.Boss boss = root.GetComponentInChildren<Boss.Boss>(true);
                if (boss == null)
                {
                    Debug.LogError("[HurtboxSetup] Boss 컴포넌트가 없는 프리팹입니다.");
                    return;
                }

                AddPartsToBones(root);

                BossPart bodyPart = FindBodyPart(root);
                if (bodyPart == null)
                {
                    Debug.LogWarning($"[HurtboxSetup] '{BodyObjectName}' 부위를 찾지 못했습니다. 부위가 없는 콜라이더는 연결되지 않습니다.");
                }

                Dictionary<BossPart, int> countByPart = new Dictionary<BossPart, int>();
                int added = 0;

                foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
                {
                    if (collider.isTrigger) continue;                             // 공격 판정 등은 제외
                    if (collider.gameObject == boss.gameObject) continue;        // 보스 본체(이동용 콜라이더 등)는 제외

                    GameObject target = collider.gameObject;

                    Hurtbox hurtbox = target.GetComponent<Hurtbox>();
                    if (hurtbox == null)
                    {
                        hurtbox = target.AddComponent<Hurtbox>();
                        added++;
                    }

                    target.layer = hurtboxLayer;

                    BossPart part = target.GetComponentInParent<BossPart>(true);
                    if (part == null) part = bodyPart;
                    if (part == null) continue;

                    SerializedObject serialized = new SerializedObject(hurtbox);
                    serialized.FindProperty("part").objectReferenceValue = part;
                    serialized.ApplyModifiedPropertiesWithoutUndo();

                    countByPart.TryGetValue(part, out int count);
                    countByPart[part] = count + 1;
                }

                foreach (KeyValuePair<BossPart, int> pair in countByPart)
                {
                    Debug.Log($"[HurtboxSetup] {GetPartName(pair.Key)} ({pair.Key.name}): Hurtbox {pair.Value}개");
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[HurtboxSetup] 완료. 새 Hurtbox {added}개: {path}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AddPartsToBones(GameObject root)
        {
            foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
            {
                if (PartBones.TryGetValue(bone.name, out string partName) == false) continue;
                if (bone.GetComponent<BossPart>() != null) continue;

                BossPart part = bone.gameObject.AddComponent<BossPart>();
                SerializedObject serialized = new SerializedObject(part);
                serialized.FindProperty("partName").stringValue = partName;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Debug.Log($"[HurtboxSetup] BossPart 추가: {partName} → {bone.name}");
            }
        }

        private static BossPart FindBodyPart(GameObject root)
        {
            foreach (BossPart part in root.GetComponentsInChildren<BossPart>(true))
            {
                if (part.name == BodyObjectName) return part;
            }
            return null;
        }

        private static string GetPartName(BossPart part)
        {
            return new SerializedObject(part).FindProperty("partName").stringValue;
        }
    }
}

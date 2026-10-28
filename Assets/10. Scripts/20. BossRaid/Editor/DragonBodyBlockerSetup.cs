using UnityEditor;
using UnityEngine;

namespace BossRaid.EditorTools
{
    // 플레이어가 용의 뼈 콜라이더(37개)에 걸려 튕겨 나가지 않게, 몸에 부딪히는 건 캡슐 하나로만 하게 만드는 에디터 도구입니다.
    // 사용법: Project 창에서 드래곤 프리팹(Boss가 붙은 Variant) 선택 → 메뉴 BossRaid > Setup Dragon Body Blocker
    // - 충돌 표에서 BossHurtbox ↔ PlayerHurtbox 충돌을 끈다 (뼈 콜라이더는 칼 판정에만 쓰임)
    // - Boss가 붙은 루트에 CapsuleCollider가 없으면 루트를 Dragon 레이어로 바꾸고 캡슐을 단 뒤, 어깨·엉덩이 뼈 위치로 크기를 맞춘다
    // - 이미 캡슐이 있으면 크기를 건드리지 않으므로, 인스펙터에서 손으로 맞춘 값은 그대로 남습니다.
    // - 루트 콜라이더는 DragonHurtboxSetup에서 제외되므로 Hurtbox로 바뀌지 않습니다.
    // - 프리팹 모드로 열어둔 상태라면 저장하고 닫은 뒤 실행하세요.
    public static class DragonBodyBlockerSetup
    {
        private const string MenuPath = "BossRaid/Setup Dragon Body Blocker";
        private const string BossHurtboxLayerName = "BossHurtbox";
        private const string PlayerHurtboxLayerName = "PlayerHurtbox";
        private const string BodyLayerName = "Dragon"; // 몸통 캡슐이 붙는 루트의 레이어. 카메라 충돌 마스크에서 빼 두세요

        private const string ShoulderL = "MountainDragon_ L UpperArm";
        private const string ShoulderR = "MountainDragon_ R UpperArm";
        private const string HipL = "MountainDragon_ L Thigh";
        private const string HipR = "MountainDragon_ R Thigh";

        [MenuItem(MenuPath, true)]
        private static bool ValidateSetup()
        {
            return Selection.activeObject is GameObject go && PrefabUtility.IsPartOfPrefabAsset(go);
        }

        [MenuItem(MenuPath)]
        private static void Setup()
        {
            int bossHurtboxLayer = LayerMask.NameToLayer(BossHurtboxLayerName);
            int playerHurtboxLayer = LayerMask.NameToLayer(PlayerHurtboxLayerName);
            int bodyLayer = LayerMask.NameToLayer(BodyLayerName);
            if (bossHurtboxLayer < 0 || playerHurtboxLayer < 0 || bodyLayer < 0)
            {
                Debug.LogError($"[BodyBlocker] {BossHurtboxLayerName} / {PlayerHurtboxLayerName} / {BodyLayerName} 레이어 중 없는 게 있습니다.");
                return;
            }

            // 에디터에서 부르면 Project Settings > Physics의 충돌 표가 바뀝니다
            Physics.IgnoreLayerCollision(bossHurtboxLayer, playerHurtboxLayer, true);
            Debug.Log($"[BodyBlocker] 충돌 끔: {BossHurtboxLayerName} ↔ {PlayerHurtboxLayerName}");

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                Boss.Boss boss = root.GetComponentInChildren<Boss.Boss>(true);
                if (boss == null)
                {
                    Debug.LogError("[BodyBlocker] Boss 컴포넌트가 없는 프리팹입니다.");
                    return;
                }

                if (boss.GetComponent<CapsuleCollider>() != null)
                {
                    Debug.Log("[BodyBlocker] 몸통 캡슐이 이미 있어서 크기는 그대로 둡니다.");
                }
                else
                {
                    boss.gameObject.layer = bodyLayer; // 루트만 바꿉니다. 자식(뼈 콜라이더)은 BossHurtbox 그대로
                    AddBodyCapsule(boss.transform);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
        }

        private static void AddBodyCapsule(Transform bossRoot)
        {
            Transform shoulderL = FindBone(bossRoot, ShoulderL);
            Transform shoulderR = FindBone(bossRoot, ShoulderR);
            Transform hipL = FindBone(bossRoot, HipL);
            Transform hipR = FindBone(bossRoot, HipR);
            if (shoulderL == null || shoulderR == null || hipL == null || hipR == null)
            {
                Debug.LogError("[BodyBlocker] 어깨·엉덩이 뼈를 찾지 못했습니다. 캡슐을 달지 않았습니다.");
                return;
            }

            // 루트 기준 좌표로 바꿔서 계산합니다 (용은 루트의 +Z를 바라봄)
            Vector3 shoulderMid = bossRoot.InverseTransformPoint((shoulderL.position + shoulderR.position) * 0.5f);
            Vector3 hipMid = bossRoot.InverseTransformPoint((hipL.position + hipR.position) * 0.5f);
            float shoulderWidth = Vector3.Distance(
                bossRoot.InverseTransformPoint(shoulderL.position),
                bossRoot.InverseTransformPoint(shoulderR.position));

            CapsuleCollider capsule = bossRoot.gameObject.AddComponent<CapsuleCollider>();
            capsule.direction = 2; // Z축 (머리-꼬리 방향)
            capsule.radius = shoulderWidth * 0.5f;
            capsule.height = Mathf.Abs(shoulderMid.z - hipMid.z) + capsule.radius * 2f;
            capsule.center = (shoulderMid + hipMid) * 0.5f;

            Debug.Log($"[BodyBlocker] 몸통 캡슐 추가: center {capsule.center}, radius {capsule.radius:F2}, height {capsule.height:F2}");
        }

        private static Transform FindBone(Transform root, string boneName)
        {
            foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == boneName) return bone;
            }
            return null;
        }
    }
}

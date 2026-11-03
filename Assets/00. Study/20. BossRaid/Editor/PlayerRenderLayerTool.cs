using UnityEditor;
using UnityEngine;

namespace BossRaid.Tools
{
    // 플레이어 몸 메시를 PlayerRender 레이어로 옮기는 메뉴 (가려진 실루엣용: PC_Renderer의 Render Objects가 이 레이어만 따로 그림)
    // : MeshRenderer·SkinnedMeshRenderer가 있고 Collider가 없는 오브젝트만 옮깁니다
    //   루트에서 "자식까지 바꾸기"를 하면 히트박스·허트박스 레이어까지 바뀌어 맞는 판정이 깨지기 때문
    //   파티클·트레일은 안 옮김 (반투명이라 평소대로 그려야 함)
    // 다시 실행해도 같은 결과. 갈아입기용으로 꺼 둔 메시도 같이 옮깁니다
    public static class PlayerRenderLayerTool
    {
        private const string PrefabPath = "Assets/00. Study/20. BossRaid/Prefab/ModularCharacters.prefab";
        private const string LayerName = "PlayerRender";

        [MenuItem("BossRaid/Player/Move Meshes To PlayerRender Layer")]
        private static void MoveMeshes()
        {
            int layer = LayerMask.NameToLayer(LayerName);
            if (layer < 0)
            {
                Debug.LogError($"{LayerName} 레이어가 없습니다. Project Settings → Tags and Layers에서 먼저 만드세요");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Debug.LogError($"{PrefabPath}가 없습니다");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                int moved = 0;
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer) continue;
                    GameObject go = renderer.gameObject;
                    if (go.GetComponent<Collider>() != null) continue; // 판정용 오브젝트는 그대로
                    if (go.layer == layer) continue;
                    go.layer = layer;
                    moved++;
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"플레이어 메시 {moved}개를 {LayerName} 레이어로 옮겼습니다");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}

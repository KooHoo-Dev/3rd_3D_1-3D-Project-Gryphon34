using BossRaid.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BossRaid.Tools
{
    // 다크소울식 보스 바 프리팹을 만들고 열린 씬에 놓는 메뉴
    // : 화면 아래 가운데에 이름, HP 바, 그 아래 얇은 피로도 바
    // 프리팹은 처음 한 번만 만들고, 다시 실행하면 만들어 둔 것(직접 고친 값 포함)을 씬에 놓기만 합니다 (PlayerHUDBuilder와 같은 방식)
    // 이미 만든 프리팹에 HP 잔상만 붙이려면 Add Damage Trail 메뉴 (없으면 붙이고 나머지는 안 건드림)
    public static class BossHUDBuilder
    {
        private const string Folder = "Assets/00. Study/20. BossRaid/UI";
        private const string PrefabPath = Folder + "/BossHUD.prefab";
        private const string WhiteSpritePath = Folder + "/HUD_White.png"; // PlayerHUDBuilder가 만든 흰색 스프라이트
        private const string KoreanFontPath = "Assets/99. External Assets/Font/Noto_Sans_KR/static/NotoSansKR-Regular SDF.asset";

        private const float BarWidth = 1000f;

        private static readonly Color BarBackColor = new Color(0.05f, 0.05f, 0.05f, 0.8f);
        private static readonly Color HealthColor = new Color(0.55f, 0.08f, 0.08f);
        private static readonly Color FatigueColor = new Color(0.85f, 0.7f, 0.3f);
        private static readonly Color HealthDelayColor = new Color(0.95f, 0.8f, 0.35f);

        [MenuItem("BossRaid/UI/Create Boss HUD")]
        private static void Create()
        {
            BossHUD existing = Object.FindAnyObjectByType<BossHUD>();
            if (existing != null)
            {
                Debug.Log("이 씬에는 이미 BossHUD가 있습니다");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) prefab = BuildPrefab();
            if (prefab == null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Create Boss HUD");
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"BossHUD를 씬에 놓았습니다. 모양은 {PrefabPath}에서 고치세요");
        }

        [MenuItem("BossRaid/UI/Rebuild Boss HUD Prefab")]
        private static void Rebuild()
        {
            bool ok = EditorUtility.DisplayDialog("BossHUD 다시 만들기",
                $"{PrefabPath}를 처음 모양으로 다시 만듭니다. 프리팹에서 직접 고친 값은 사라집니다.", "다시 만들기", "취소");
            if (ok == false) return;

            if (BuildPrefab() != null) Debug.Log("BossHUD 프리팹을 다시 만들었습니다");
        }

        // 만들어 둔 프리팹을 열어서 HP 바에 잔상(Delay)이 없으면 붙이고 BossHUD에 연결. 나머지는 안 건드림
        [MenuItem("BossRaid/UI/Add Damage Trail To Boss HUD")]
        private static void AddDamageTrail()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Debug.LogWarning($"{PrefabPath}가 없습니다. Create Boss HUD를 먼저 실행하세요");
                return;
            }
            var white = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
            if (white == null)
            {
                Debug.LogError($"{WhiteSpritePath}가 없습니다. BossRaid/UI/Create Player HUD를 먼저 한 번 실행하세요");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform healthBar = root.transform.Find("BossBar/HealthBar");
                if (healthBar == null) Debug.LogWarning("BossHUD 안에 BossBar/HealthBar가 없어서 잔상을 못 붙였습니다");
                else if (healthBar.Find("Delay") != null) Debug.Log("BossHUD에 이미 HP 잔상이 있습니다");
                else
                {
                    Image delayFill = CreateDelayFill(healthBar, white);
                    var so = new SerializedObject(root.GetComponent<BossHUD>());
                    so.FindProperty("healthDelayFill").objectReferenceValue = delayFill;
                    so.ApplyModifiedPropertiesWithoutUndo();

                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                    Debug.Log("BossHUD에 HP 잔상을 붙였습니다");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject BuildPrefab()
        {
            var white = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
            if (white == null)
            {
                Debug.LogError($"{WhiteSpritePath}가 없습니다. BossRaid/UI/Create Player HUD를 먼저 한 번 실행하세요");
                return null;
            }
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (font == null)
            {
                Debug.LogError($"한글 폰트가 없습니다: {KoreanFontPath}");
                return null;
            }

            GameObject root = BuildHierarchy(white, font);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildHierarchy(Sprite white, TMP_FontAsset font)
        {
            // 캔버스
            var root = new GameObject("BossHUD", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var hud = root.AddComponent<BossHUD>();

            // 아래 가운데: 이름, HP, 피로도 (보이기·숨기기는 CanvasGroup 투명도로)
            RectTransform panel = CreateRect("BossBar", root.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 110), new Vector2(BarWidth, 60));
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            var nameText = CreateText("Name", panel, font, new Vector2(4, 0), new Vector2(BarWidth, 30));
            CreateBar("HealthBar", panel, new Vector2(0, -32), 14, white, HealthColor, out Image healthFill, out _);
            CreateBar("FatigueBar", panel, new Vector2(0, -50), 7, white, FatigueColor, out Image fatigueFill, out Image fatigueBack);
            Image healthDelayFill = CreateDelayFill(panel.Find("HealthBar"), white);

            // BossHUD의 private 필드 연결
            var so = new SerializedObject(hud);
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("healthFill").objectReferenceValue = healthFill;
            so.FindProperty("fatigueFill").objectReferenceValue = fatigueFill;
            so.FindProperty("fatigueBack").objectReferenceValue = fatigueBack;
            so.FindProperty("healthDelayFill").objectReferenceValue = healthDelayFill;
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        // 배경 + 안쪽을 채우는 Filled 이미지. position은 panel 왼쪽 위 기준
        private static void CreateBar(string name, RectTransform parent, Vector2 position, float height,
            Sprite white, Color fillColor, out Image fill, out Image back)
        {
            RectTransform bar = CreateRect(name, parent, new Vector2(0, 1), new Vector2(0, 1), position, new Vector2(BarWidth, height));
            back = bar.gameObject.AddComponent<Image>();
            back.sprite = white;
            back.color = BarBackColor;
            back.raycastTarget = false;

            RectTransform fillRect = CreateStretch("Fill", bar, 2);
            fill = fillRect.gameObject.AddComponent<Image>();
            fill.sprite = white; // Filled는 스프라이트가 있어야 동작함
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;
        }

        // HP 바 안 채우기(Fill)와 같은 크기의 Filled 이미지를 맨 뒤(첫 자식)에 깔아서 Fill 뒤로 보이게 함
        private static Image CreateDelayFill(Transform healthBar, Sprite white)
        {
            RectTransform rect = CreateStretch("Delay", healthBar, 2);
            rect.SetAsFirstSibling();
            var delay = rect.gameObject.AddComponent<Image>();
            delay.sprite = white;
            delay.color = HealthDelayColor;
            delay.type = Image.Type.Filled;
            delay.fillMethod = Image.FillMethod.Horizontal;
            delay.fillOrigin = (int)Image.OriginHorizontal.Left;
            delay.fillAmount = 1f;
            delay.raycastTarget = false;
            return delay;
        }

        private static TextMeshProUGUI CreateText(string name, RectTransform parent, TMP_FontAsset font, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0, 1), new Vector2(0, 1), position, size);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = "보스"; // 실제 이름은 BossHUD.bossName
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.BottomLeft;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        // anchor와 pivot을 같은 점에 두고, 그 점에서 position만큼 떨어진 곳에 size 크기로 놓습니다
        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 pivot,
            Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        // 부모를 꽉 채우되 사방으로 inset만큼 안쪽으로 들어갑니다
        private static RectTransform CreateStretch(string name, Transform parent, float inset)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }
    }
}

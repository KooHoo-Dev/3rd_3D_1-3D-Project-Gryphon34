using System.IO;
using BossRaid.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BossRaid.Tools
{
    // 몬스터헌터 월드식 플레이어 HUD 프리팹을 만들고 열린 씬에 놓는 메뉴
    // : 왼쪽 위 HP·스태미나 바, 오른쪽 아래 아이템 칸 (큰 칸 = 고른 것, 작은 칸 = 휠로 바꿀 것)
    // 프리팹은 처음 한 번만 만들고, 다시 실행하면 만들어 둔 것(직접 고친 값 포함)을 씬에 놓기만 합니다.
    // 처음 모양으로 되돌리고 싶으면 Rebuild 메뉴를 쓰세요. (직접 고친 값은 사라짐)
    public static class PlayerHUDBuilder
    {
        private const string Folder = "Assets/00. Study/20. BossRaid/UI";
        private const string PrefabPath = Folder + "/PlayerHUD.prefab";
        private const string WhiteSpritePath = Folder + "/HUD_White.png";

        private static readonly Color BarBackColor = new Color(0.05f, 0.05f, 0.05f, 0.8f);
        private static readonly Color HealthColor = new Color(0.42f, 0.85f, 0.35f);
        private static readonly Color StaminaColor = new Color(0.95f, 0.78f, 0.25f);
        private static readonly Color SlotBackColor = new Color(0.08f, 0.08f, 0.08f, 0.75f);
        private static readonly Color PotionColor = new Color(0.8f, 0.2f, 0.25f);
        private static readonly Color HumanityColor = new Color(0.85f, 0.7f, 0.3f);

        [MenuItem("BossRaid/UI/Create Player HUD")]
        private static void Create()
        {
            PlayerHUD existing = Object.FindAnyObjectByType<PlayerHUD>();
            if (existing != null)
            {
                Debug.Log("이 씬에는 이미 PlayerHUD가 있습니다");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) prefab = BuildPrefab();

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Create Player HUD");
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"PlayerHUD를 씬에 놓았습니다. 모양은 {PrefabPath}에서 고치세요");
        }

        [MenuItem("BossRaid/UI/Rebuild Player HUD Prefab")]
        private static void Rebuild()
        {
            bool ok = EditorUtility.DisplayDialog("PlayerHUD 다시 만들기",
                $"{PrefabPath}를 처음 모양으로 다시 만듭니다. 프리팹에서 직접 고친 값은 사라집니다.", "다시 만들기", "취소");
            if (ok == false) return;

            BuildPrefab();
            Debug.Log("PlayerHUD 프리팹을 다시 만들었습니다");
        }

        private static GameObject BuildPrefab()
        {
            if (AssetDatabase.IsValidFolder(Folder) == false)
                AssetDatabase.CreateFolder("Assets/00. Study/20. BossRaid", "UI");

            Sprite white = LoadOrCreateWhiteSprite();
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            GameObject root = BuildHierarchy(white, rounded);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildHierarchy(Sprite white, Sprite rounded)
        {
            // 캔버스
            var root = new GameObject("PlayerHUD", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var hud = root.AddComponent<PlayerHUD>();

            // 왼쪽 위: HP, 스태미나 바
            RectTransform status = CreateRect("Status", root.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(70, -60), new Vector2(400, 40));
            RectTransform healthBar = CreateBar("HealthBar", status, new Vector2(0, 0), 18, white, HealthColor, out Image healthFill);
            RectTransform staminaBar = CreateBar("StaminaBar", status, new Vector2(0, -28), 12, white, StaminaColor, out Image staminaFill);

            // 오른쪽 아래: 아이템 칸. 오른쪽 큰 칸 = 고른 것(R), 그 왼쪽 작은 칸 = 휠로 바꿀 것 (색·개수는 PlayerHUD가 칸마다 바꿈)
            RectTransform items = CreateRect("Items", root.transform, new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-70, 70), new Vector2(176, 100));
            CanvasGroup mainSlot = CreateSlot("MainSlot", items, new Vector2(0, 0), 100, rounded, PotionColor, "R", "3/3",
                out Image mainIcon, out TextMeshProUGUI mainCount);
            CanvasGroup subSlot = CreateSlot("SubSlot", items, new Vector2(-112, 0), 64, rounded, HumanityColor, null, "0",
                out Image subIcon, out TextMeshProUGUI subCount);

            // PlayerHUD의 private 필드 연결
            var so = new SerializedObject(hud);
            so.FindProperty("healthBar").objectReferenceValue = healthBar;
            so.FindProperty("healthFill").objectReferenceValue = healthFill;
            so.FindProperty("staminaBar").objectReferenceValue = staminaBar;
            so.FindProperty("staminaFill").objectReferenceValue = staminaFill;
            so.FindProperty("mainSlot").objectReferenceValue = mainSlot;
            so.FindProperty("mainIcon").objectReferenceValue = mainIcon;
            so.FindProperty("mainCountText").objectReferenceValue = mainCount;
            so.FindProperty("subSlot").objectReferenceValue = subSlot;
            so.FindProperty("subIcon").objectReferenceValue = subIcon;
            so.FindProperty("subCountText").objectReferenceValue = subCount;
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        // 배경(길이는 PlayerHUD가 최대치에 맞춰 바꿈) + 안쪽을 채우는 Filled 이미지
        private static RectTransform CreateBar(string name, RectTransform parent, Vector2 position, float height,
            Sprite white, Color fillColor, out Image fill)
        {
            RectTransform bar = CreateRect(name, parent, new Vector2(0, 1), new Vector2(0, 1), position, new Vector2(400, height));
            var back = bar.gameObject.AddComponent<Image>();
            back.sprite = white;
            back.color = BarBackColor;

            RectTransform fillRect = CreateStretch("Fill", bar, 3);
            fill = fillRect.gameObject.AddComponent<Image>();
            fill.sprite = white; // Filled는 스프라이트가 있어야 동작함
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            return bar;
        }

        // 칸 하나: 배경 + 색 도형(아이콘 자리) + 키 글자(없으면 생략) + 개수. size는 정사각형 한 변
        private static CanvasGroup CreateSlot(string name, RectTransform parent, Vector2 position, float size, Sprite rounded,
            Color iconColor, string key, string count, out Image icon, out TextMeshProUGUI countText)
        {
            float scale = size / 100f; // 100짜리 칸 기준으로 안쪽 여백·글자 크기를 줄임
            RectTransform slot = CreateRect(name, parent, new Vector2(1, 0), new Vector2(1, 0), position, new Vector2(size, size));
            var back = slot.gameObject.AddComponent<Image>();
            back.sprite = rounded;
            back.type = Image.Type.Sliced;
            back.color = SlotBackColor;
            var group = slot.gameObject.AddComponent<CanvasGroup>();

            RectTransform iconRect = CreateStretch("Icon", slot, 24 * scale);
            icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = rounded;
            icon.type = Image.Type.Sliced;
            icon.color = iconColor;

            if (string.IsNullOrEmpty(key) == false)
            {
                CreateText("Key", slot, key, 22 * scale, TextAlignmentOptions.TopLeft,
                    new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -4) * scale, new Vector2(40, 30) * scale);
            }
            countText = CreateText("Count", slot, count, 24 * Mathf.Max(scale, 0.75f), TextAlignmentOptions.BottomRight,
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(-8, 4) * scale, new Vector2(80, 30) * scale);
            return group;
        }

        private static TextMeshProUGUI CreateText(string name, RectTransform parent, string text, float size,
            TextAlignmentOptions alignment, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 rectSize)
        {
            RectTransform rect = CreateRect(name, parent, anchor, pivot, position, rectSize);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
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

        // Image Type: Filled는 스프라이트가 있어야 동작해서, 흰색 4x4 스프라이트를 한 번 만들어 둡니다
        private static Sprite LoadOrCreateWhiteSprite()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
            if (sprite != null) return sprite;

            var texture = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            File.WriteAllBytes(WhiteSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(WhiteSpritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(WhiteSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
        }
    }
}

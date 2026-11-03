using System.IO;
using BossRaid.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BossRaid.Tools
{
    // 다크소울식 플레이어 HUD 프리팹을 만들고 열린 씬에 놓는 메뉴
    // : 왼쪽 위 HP·스태미나 바, 왼쪽 아래 아이템 칸 (큰 칸 = 고른 것, 작은 칸 = 휠로 바꿀 것), 오른쪽 아래 소울
    // 프리팹은 처음 한 번만 만들고, 다시 실행하면 만들어 둔 것(직접 고친 값 포함)을 씬에 놓기만 합니다.
    // 처음 모양으로 되돌리고 싶으면 Rebuild 메뉴를 쓰세요. (직접 고친 값은 사라짐)
    // 이미 만든 프리팹을 다크소울식 배치로만 바꾸려면 Apply Dark Souls Layout 메뉴 (직접 고친 값은 그대로)
    // 소울 +N·소재 획득 알림만 붙이려면 Add Gain Popups 메뉴 (없는 것만 붙이고 나머지는 안 건드림)
    public static class PlayerHUDBuilder
    {
        private const string Folder = "Assets/00. Study/20. BossRaid/UI";
        private const string PrefabPath = Folder + "/PlayerHUD.prefab";
        private const string WhiteSpritePath = Folder + "/HUD_White.png";
        private const string KoreanFontPath = "Assets/99. External Assets/Font/Noto_Sans_KR/static/NotoSansKR-Regular SDF.asset";

        private static readonly Color BarBackColor = new Color(0.05f, 0.05f, 0.05f, 0.8f);
        private static readonly Color HealthColor = new Color(0.42f, 0.85f, 0.35f);
        private static readonly Color HealthDelayColor = new Color(1f, 1f, 1f, 0.85f); // 초록 HP·노랑 스태미나와 밝기로 구분
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

        // 만들어 둔 프리팹을 열어서 아이템 칸만 왼쪽 아래로 옮기고, 소울 칸이 없으면 오른쪽 아래에 붙임. 나머지는 안 건드림
        [MenuItem("BossRaid/UI/Apply Dark Souls Layout To Player HUD")]
        private static void ApplyDarkSoulsLayout()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Debug.LogWarning($"{PrefabPath}가 없습니다. Create Player HUD를 먼저 실행하세요");
                return;
            }
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (font == null)
            {
                Debug.LogError($"한글 폰트가 없습니다: {KoreanFontPath}");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var items = root.transform.Find("Items") as RectTransform;
                if (items != null) PlaceItems(items);
                else Debug.LogWarning("PlayerHUD 안에 Items가 없어서 아이템 칸은 못 옮겼습니다");

                if (root.transform.Find("Souls") == null)
                {
                    TextMeshProUGUI soulsText = CreateSouls(root.transform, LoadOrCreateWhiteSprite(), font);
                    var so = new SerializedObject(root.GetComponent<PlayerHUD>());
                    so.FindProperty("soulsText").objectReferenceValue = soulsText;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("PlayerHUD를 다크소울식 배치로 바꿨습니다 (아이템 왼쪽 아래, 소울 오른쪽 아래)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 만들어 둔 프리팹을 열어서 소울 칸 위 +N 글자와 소재 획득 알림(Toasts)을 없으면 붙임. 나머지는 안 건드림
        [MenuItem("BossRaid/UI/Add Gain Popups To Player HUD")]
        private static void AddGainPopups()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Debug.LogWarning($"{PrefabPath}가 없습니다. Create Player HUD를 먼저 실행하세요");
                return;
            }
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (font == null)
            {
                Debug.LogError($"한글 폰트가 없습니다: {KoreanFontPath}");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var souls = root.transform.Find("Souls") as RectTransform;
                if (souls == null) Debug.LogWarning("PlayerHUD 안에 Souls가 없어서 +N 글자는 못 붙였습니다. Apply Dark Souls Layout을 먼저 실행하세요");
                else if (souls.Find("Gain") == null)
                {
                    TextMeshProUGUI gainText = CreateSoulsGain(souls, font);
                    var so = new SerializedObject(root.GetComponent<PlayerHUD>());
                    so.FindProperty("soulsGainText").objectReferenceValue = gainText;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                if (root.transform.Find("Toasts") == null)
                {
                    Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                    CreateToasts(root.transform, LoadOrCreateWhiteSprite(), rounded, font);
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("PlayerHUD에 소울 +N 글자와 소재 획득 알림을 붙였습니다");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 만들어 둔 프리팹을 열어서 HP 바에 잔상(Delay)이 없으면 붙이고 PlayerHUD에 연결. 나머지는 안 건드림
        [MenuItem("BossRaid/UI/Add Damage Trail To Player HUD")]
        private static void AddDamageTrail()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Debug.LogWarning($"{PrefabPath}가 없습니다. Create Player HUD를 먼저 실행하세요");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform healthBar = root.transform.Find("Status/HealthBar");
                if (healthBar == null) Debug.LogWarning("PlayerHUD 안에 Status/HealthBar가 없어서 잔상을 못 붙였습니다");
                else if (healthBar.Find("Delay") != null) Debug.Log("PlayerHUD에 이미 HP 잔상이 있습니다");
                else
                {
                    Image delayFill = CreateDelayFill(healthBar, LoadOrCreateWhiteSprite());
                    var so = new SerializedObject(root.GetComponent<PlayerHUD>());
                    so.FindProperty("healthDelayFill").objectReferenceValue = delayFill;
                    so.ApplyModifiedPropertiesWithoutUndo();

                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                    Debug.Log("PlayerHUD에 HP 잔상을 붙였습니다");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject BuildPrefab()
        {
            if (AssetDatabase.IsValidFolder(Folder) == false)
                AssetDatabase.CreateFolder("Assets/00. Study/20. BossRaid", "UI");

            Sprite white = LoadOrCreateWhiteSprite();
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (font == null) Debug.LogError($"한글 폰트가 없습니다: {KoreanFontPath} (소울 글자가 안 보일 수 있음)");

            GameObject root = BuildHierarchy(white, rounded, font);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildHierarchy(Sprite white, Sprite rounded, TMP_FontAsset font)
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
            Image healthDelayFill = CreateDelayFill(healthBar, white);

            // 왼쪽 아래: 아이템 칸. 오른쪽 큰 칸 = 고른 것(R), 그 왼쪽 작은 칸 = 휠로 바꿀 것 (색·개수는 PlayerHUD가 칸마다 바꿈)
            RectTransform items = CreateRect("Items", root.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(176, 100));
            PlaceItems(items);
            CanvasGroup mainSlot = CreateSlot("MainSlot", items, new Vector2(0, 0), 100, rounded, PotionColor, "R", "3/3",
                out Image mainIcon, out TextMeshProUGUI mainCount);
            CanvasGroup subSlot = CreateSlot("SubSlot", items, new Vector2(-112, 0), 64, rounded, HumanityColor, null, "0",
                out Image subIcon, out TextMeshProUGUI subCount);

            // 오른쪽 아래: 가진 소울, 그 위에 +N, 그 위에 소재 획득 알림
            TextMeshProUGUI soulsText = CreateSouls(root.transform, white, font);
            TextMeshProUGUI soulsGainText = CreateSoulsGain((RectTransform)soulsText.transform.parent, font);
            CreateToasts(root.transform, white, rounded, font);

            // PlayerHUD의 private 필드 연결
            var so = new SerializedObject(hud);
            so.FindProperty("healthBar").objectReferenceValue = healthBar;
            so.FindProperty("healthFill").objectReferenceValue = healthFill;
            so.FindProperty("healthDelayFill").objectReferenceValue = healthDelayFill;
            so.FindProperty("staminaBar").objectReferenceValue = staminaBar;
            so.FindProperty("staminaFill").objectReferenceValue = staminaFill;
            so.FindProperty("mainSlot").objectReferenceValue = mainSlot;
            so.FindProperty("mainIcon").objectReferenceValue = mainIcon;
            so.FindProperty("mainCountText").objectReferenceValue = mainCount;
            so.FindProperty("subSlot").objectReferenceValue = subSlot;
            so.FindProperty("subIcon").objectReferenceValue = subIcon;
            so.FindProperty("subCountText").objectReferenceValue = subCount;
            so.FindProperty("soulsText").objectReferenceValue = soulsText;
            so.FindProperty("soulsGainText").objectReferenceValue = soulsGainText;
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        // 아이템 칸 묶음을 화면 왼쪽 아래에 (새로 만들 때와 Apply Dark Souls Layout이 같이 씀)
        private static void PlaceItems(RectTransform items)
        {
            items.anchorMin = Vector2.zero;
            items.anchorMax = Vector2.zero;
            items.pivot = Vector2.zero;
            items.anchoredPosition = new Vector2(70, 70);
        }

        // 오른쪽 아래 소울 칸: 어두운 띠 + 왼쪽 "소울" + 오른쪽 숫자. 숫자 글자를 돌려줌 (PlayerHUD.soulsText)
        private static TextMeshProUGUI CreateSouls(Transform root, Sprite white, TMP_FontAsset font)
        {
            RectTransform souls = CreateRect("Souls", root, new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-70, 70), new Vector2(220, 44));
            var back = souls.gameObject.AddComponent<Image>();
            back.sprite = white;
            back.color = SlotBackColor;
            back.raycastTarget = false;

            TextMeshProUGUI label = CreateText("Label", souls, ItemDisplay.SoulName, 20, TextAlignmentOptions.Left,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(80, 40));
            label.color = HumanityColor;
            TextMeshProUGUI count = CreateText("Count", souls, "0", 28, TextAlignmentOptions.Right,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(130, 40));
            if (font != null)
            {
                label.font = font;
                count.font = font;
            }
            return count;
        }

        // 소울 칸 바로 위 오른쪽 정렬 +N (PlayerHUD.soulsGainText). 얻을 때만 PlayerHUD가 켬
        private static TextMeshProUGUI CreateSoulsGain(RectTransform souls, TMP_FontAsset font)
        {
            TextMeshProUGUI gain = CreateText("Gain", souls, "+0", 24, TextAlignmentOptions.BottomRight,
                new Vector2(1, 1), new Vector2(1, 0), new Vector2(-14, 4), new Vector2(200, 32));
            if (font != null) gain.font = font;
            gain.enabled = false;
            return gain;
        }

        // 소울 칸 위 소재 획득 알림 (MaterialToast). 새 줄이 아래에 붙고 위로 쌓임. 줄 틀은 꺼 둔 Row 하나
        private static void CreateToasts(Transform root, Sprite white, Sprite rounded, TMP_FontAsset font)
        {
            RectTransform toasts = CreateRect("Toasts", root, new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-70, 160), new Vector2(260, 300));
            var layout = toasts.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.LowerRight;
            layout.spacing = 6;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var toast = toasts.gameObject.AddComponent<MaterialToast>();

            // 줄 하나: 어두운 띠 + 색 도형(아이콘 자리) + 이름 + 개수. 사라질 때 CanvasGroup 투명도를 씀
            RectTransform row = CreateRect("Row", toasts, new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, new Vector2(240, 40));
            var back = row.gameObject.AddComponent<Image>();
            back.sprite = white;
            back.color = SlotBackColor;
            back.raycastTarget = false;
            row.gameObject.AddComponent<CanvasGroup>();

            RectTransform iconRect = CreateRect("Icon", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(26, 26));
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = rounded;
            icon.type = Image.Type.Sliced;
            icon.raycastTarget = false;

            TextMeshProUGUI nameText = CreateText("Name", row, "", 20, TextAlignmentOptions.Left,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(46, 0), new Vector2(130, 36));
            TextMeshProUGUI countText = CreateText("Count", row, "+1", 22, TextAlignmentOptions.Right,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(60, 36));
            if (font != null)
            {
                nameText.font = font;
                countText.font = font;
            }

            var itemRow = row.gameObject.AddComponent<ItemRow>();
            var rowSo = new SerializedObject(itemRow);
            rowSo.FindProperty("icon").objectReferenceValue = icon;
            rowSo.FindProperty("nameText").objectReferenceValue = nameText;
            rowSo.FindProperty("countText").objectReferenceValue = countText;
            rowSo.ApplyModifiedPropertiesWithoutUndo();
            row.gameObject.SetActive(false);

            var toastSo = new SerializedObject(toast);
            toastSo.FindProperty("rowTemplate").objectReferenceValue = itemRow;
            toastSo.ApplyModifiedPropertiesWithoutUndo();
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

        // HP 바 안 채우기(Fill)와 같은 크기의 Filled 이미지를 맨 뒤(첫 자식)에 깔아서 Fill 뒤로 보이게 함
        private static Image CreateDelayFill(Transform healthBar, Sprite white)
        {
            RectTransform rect = CreateStretch("Delay", healthBar, 3);
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

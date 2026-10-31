using BossRaid.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BossRaid.Tools
{
    // 사냥 결과 패널 프리팹을 만들고 열린 씬(투기장)에 놓는 메뉴
    // : 화면을 살짝 어둡게 + 가운데 반투명 패널 (제목 / 줄: [아이콘] 이름 … 개수 / "N초 뒤 마을로")
    // 프리팹은 처음 한 번만 만들고, 다시 실행하면 만들어 둔 것을 씬에 놓기만 합니다. (PlayerHUDBuilder와 같은 방식)
    public static class ResultPanelBuilder
    {
        private const string Folder = "Assets/00. Study/20. BossRaid/UI";
        private const string PrefabPath = Folder + "/ResultPanel.prefab";
        // TMP 기본 폰트(LiberationSans)에는 한글이 없어서 네모로 나옴 → 한글 폰트를 씀
        private const string KoreanFontPath = "Assets/99. External Assets/Font/Noto_Sans_KR/static/NotoSansKR-Regular SDF.asset";

        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.35f);
        private static readonly Color PanelColor = new Color(0.055f, 0.055f, 0.055f, 0.86f);
        private static readonly Color GoldLine = new Color(0.85f, 0.7f, 0.3f, 0.55f);
        private static readonly Color CountColor = new Color(0.98f, 0.78f, 0.46f);
        private static readonly Color FooterColor = new Color(0.7f, 0.7f, 0.66f);

        [MenuItem("BossRaid/UI/Create Result Panel")]
        private static void Create()
        {
            ResultPanel existing = Object.FindAnyObjectByType<ResultPanel>();
            if (existing != null)
            {
                Debug.Log("이 씬에는 이미 ResultPanel이 있습니다");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) prefab = BuildPrefab();
            if (prefab == null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Create Result Panel");
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"ResultPanel을 씬에 놓았습니다. 모양은 {PrefabPath}에서 고치세요");
        }

        [MenuItem("BossRaid/UI/Rebuild Result Panel Prefab")]
        private static void Rebuild()
        {
            bool ok = EditorUtility.DisplayDialog("ResultPanel 다시 만들기",
                $"{PrefabPath}를 처음 모양으로 다시 만듭니다. 프리팹에서 직접 고친 값은 사라집니다.", "다시 만들기", "취소");
            if (ok == false) return;

            BuildPrefab();
            Debug.Log("ResultPanel 프리팹을 다시 만들었습니다");
        }

        private static GameObject BuildPrefab()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (font == null)
            {
                Debug.LogError($"한글 폰트가 없습니다: {KoreanFontPath}");
                return null;
            }
            if (AssetDatabase.IsValidFolder(Folder) == false)
                AssetDatabase.CreateFolder("Assets/00. Study/20. BossRaid", "UI");

            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            GameObject root = BuildHierarchy(font, rounded);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildHierarchy(TMP_FontAsset font, Sprite rounded)
        {
            // 캔버스 (HUD보다 위에 그림)
            var root = new GameObject("ResultPanel", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            var panelScript = root.AddComponent<ResultPanel>();

            // 화면 전체를 살짝 어둡게
            RectTransform dim = CreateStretch("Dim", root.transform);
            AddImage(dim, null, DimColor);

            // 가운데 패널: 세로로 쌓고, 높이는 내용에 맞춤
            var panel = new GameObject("Panel", typeof(RectTransform));
            var panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(root.transform, false);
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(560, 0);
            Image panelImage = AddImage(panelRect, rounded, PanelColor);
            panelImage.type = Image.Type.Sliced;
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = GoldLine;
            outline.effectDistance = new Vector2(1, -1);
            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 26, 22);
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI title = CreateText("Title", panelRect, font, "토벌 완료", 44, TextAlignmentOptions.Center, Color.white);
            AddLayout(title.gameObject, height: 56);

            var divider = new GameObject("Divider", typeof(RectTransform));
            divider.transform.SetParent(panelRect, false);
            AddImage((RectTransform)divider.transform, null, GoldLine);
            AddLayout(divider, height: 2);

            // 줄 목록
            var rowsGo = new GameObject("Rows", typeof(RectTransform));
            rowsGo.transform.SetParent(panelRect, false);
            var rowsLayout = rowsGo.AddComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 8;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;

            ItemRow rowTemplate = CreateRowTemplate((RectTransform)rowsGo.transform, font, rounded);

            TextMeshProUGUI footer = CreateText("Footer", panelRect, font, "3초 뒤 마을로 돌아갑니다", 22, TextAlignmentOptions.Center, FooterColor);
            AddLayout(footer.gameObject, height: 32);

            // ResultPanel의 private 필드 연결
            var so = new SerializedObject(panelScript);
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("rowParent").objectReferenceValue = rowsGo.transform;
            so.FindProperty("rowTemplate").objectReferenceValue = rowTemplate;
            so.FindProperty("footerText").objectReferenceValue = footer;
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        // [아이콘] 이름 … 개수. 꺼 둔 채로 두고 ResultPanel이 복제해서 씁니다
        private static ItemRow CreateRowTemplate(RectTransform parent, TMP_FontAsset font, Sprite rounded)
        {
            var row = new GameObject("RowTemplate", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            AddLayout(row, height: 42);
            var itemRow = row.AddComponent<ItemRow>();

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(row.transform, false);
            Image icon = AddImage((RectTransform)iconGo.transform, rounded, Color.white);
            icon.type = Image.Type.Sliced;
            AddLayout(iconGo, width: 34, height: 34);

            TextMeshProUGUI nameText = CreateText("Name", (RectTransform)row.transform, font, "화룡 비늘", 28, TextAlignmentOptions.Left, Color.white);
            AddLayout(nameText.gameObject, height: 42, flexibleWidth: 1);

            TextMeshProUGUI countText = CreateText("Count", (RectTransform)row.transform, font, "+3", 28, TextAlignmentOptions.Right, CountColor);
            AddLayout(countText.gameObject, width: 110, height: 42);

            var so = new SerializedObject(itemRow);
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("countText").objectReferenceValue = countText;
            so.ApplyModifiedPropertiesWithoutUndo();

            row.SetActive(false);
            return itemRow;
        }

        private static TextMeshProUGUI CreateText(string name, RectTransform parent, TMP_FontAsset font, string text,
            float size, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Image AddImage(RectTransform rect, Sprite sprite, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        // 레이아웃 그룹 안에서의 크기 (0이면 정하지 않음)
        private static void AddLayout(GameObject go, float width = 0, float height = 0, float flexibleWidth = 0)
        {
            var element = go.AddComponent<LayoutElement>();
            if (width > 0) { element.minWidth = width; element.preferredWidth = width; }
            if (height > 0) { element.minHeight = height; element.preferredHeight = height; }
            if (flexibleWidth > 0) element.flexibleWidth = flexibleWidth;
        }

        private static RectTransform CreateStretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}

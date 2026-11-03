using BossRaid.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BossRaid.Tools
{
    // 튜토리얼 안내 프리팹을 만들고 열린 씬(투기장)에 놓는 메뉴
    // : 화면 아래 가운데 반투명 패널 (제목 / 본문). 결과 패널(10)보다 위에 그려서 사망 팁이 가려지지 않음
    // 프리팹은 처음 한 번만 만들고, 다시 실행하면 만들어 둔 것을 씬에 놓기만 합니다. (ResultPanelBuilder와 같은 방식)
    public static class TutorialHintBuilder
    {
        private const string Folder = "Assets/00. Study/20. BossRaid/UI";
        private const string PrefabPath = Folder + "/TutorialHint.prefab";
        // TMP 기본 폰트(LiberationSans)에는 한글이 없어서 네모로 나옴 → 한글 폰트를 씀
        private const string KoreanFontPath = "Assets/99. External Assets/Font/Noto_Sans_KR/static/NotoSansKR-Regular SDF.asset";

        private static readonly Color PanelColor = new Color(0.055f, 0.055f, 0.055f, 0.86f);
        private static readonly Color GoldLine = new Color(0.85f, 0.7f, 0.3f, 0.55f);
        private static readonly Color TitleColor = new Color(0.98f, 0.78f, 0.46f);

        [MenuItem("BossRaid/UI/Create Tutorial Hint")]
        private static void Create()
        {
            TutorialHint existing = Object.FindAnyObjectByType<TutorialHint>();
            if (existing != null)
            {
                Debug.Log("이 씬에는 이미 TutorialHint가 있습니다");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) prefab = BuildPrefab();
            if (prefab == null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Create Tutorial Hint");
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"TutorialHint를 씬에 놓았습니다. 모양은 {PrefabPath}에서 고치세요");
        }

        [MenuItem("BossRaid/UI/Rebuild Tutorial Hint Prefab")]
        private static void Rebuild()
        {
            bool ok = EditorUtility.DisplayDialog("TutorialHint 다시 만들기",
                $"{PrefabPath}를 처음 모양으로 다시 만듭니다. 프리팹에서 직접 고친 값은 사라집니다.", "다시 만들기", "취소");
            if (ok == false) return;

            BuildPrefab();
            Debug.Log("TutorialHint 프리팹을 다시 만들었습니다");
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
            // 캔버스 (결과 패널 10보다 위)
            var root = new GameObject("TutorialHint", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 11;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            var hintScript = root.AddComponent<TutorialHint>();

            // 아래 가운데 패널: 세로로 쌓고, 높이는 글 줄 수에 맞춤
            var panel = new GameObject("Panel", typeof(RectTransform));
            var panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(root.transform, false);
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0, 180);
            panelRect.sizeDelta = new Vector2(820, 0);
            Image panelImage = AddImage(panelRect, rounded, PanelColor);
            panelImage.type = Image.Type.Sliced;
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = GoldLine;
            outline.effectDistance = new Vector2(1, -1);
            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 18, 20);
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI title = CreateText("Title", panelRect, font, "공격이 온다!", 32, TitleColor);
            TextMeshProUGUI body = CreateText("Body", panelRect, font, "[Ctrl] 누르고 있으면 가드     [Space] 구르기", 26, Color.white);

            // TutorialHint의 private 필드 연결
            var so = new SerializedObject(hintScript);
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("bodyText").objectReferenceValue = body;
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        // 높이는 레이아웃 그룹이 글 줄 수(TMP preferred height)로 정함
        private static TextMeshProUGUI CreateText(string name, RectTransform parent, TMP_FontAsset font, string text,
            float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.richText = true;
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
    }
}

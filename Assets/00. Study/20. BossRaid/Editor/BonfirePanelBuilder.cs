using BossRaid.Flow;
using BossRaid.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BossRaid.Tools
{
    // 화톳불 메뉴 프리팹을 만들고 열린 씬(마을)에 놓는 메뉴 (ResultPanelBuilder와 같은 방식)
    // : 아래쪽 "[E] 화톳불" 안내 + 가운데 창 (제목 / 인간성 / 회복약 최대 / [인간성 태우기] / 이유 / [닫기])
    // 씬에 놓을 때 Bonfire의 panel 칸을 채우고, 버튼 클릭에 필요한 EventSystem이 없으면 같이 만듭니다
    public static class BonfirePanelBuilder
    {
        private const string Folder = "Assets/00. Study/20. BossRaid/UI";
        private const string PrefabPath = Folder + "/BonfirePanel.prefab";
        private const string KoreanFontPath = "Assets/99. External Assets/Font/Noto_Sans_KR/static/NotoSansKR-Regular SDF.asset";

        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.35f);
        private static readonly Color PanelColor = new Color(0.055f, 0.055f, 0.055f, 0.86f);
        private static readonly Color GoldLine = new Color(0.85f, 0.7f, 0.3f, 0.55f);
        private static readonly Color TitleColor = new Color(0.98f, 0.78f, 0.46f);
        private static readonly Color ButtonColor = new Color(0.32f, 0.16f, 0.08f, 1f);
        private static readonly Color CloseButtonColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        private static readonly Color ReasonColor = new Color(0.94f, 0.58f, 0.58f);
        private static readonly Color PromptColor = new Color(0.95f, 0.92f, 0.85f);

        [MenuItem("BossRaid/UI/Create Bonfire Panel")]
        private static void Create()
        {
            BonfirePanel existing = Object.FindAnyObjectByType<BonfirePanel>();
            if (existing != null)
            {
                Debug.Log("이 씬에는 이미 BonfirePanel이 있습니다");
                Selection.activeGameObject = existing.gameObject;
                LinkBonfire(existing);
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) prefab = BuildPrefab();
            if (prefab == null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Create Bonfire Panel");
            Selection.activeGameObject = instance;

            LinkBonfire(instance.GetComponent<BonfirePanel>());
            EnsureEventSystem();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"BonfirePanel을 씬에 놓았습니다. 씬을 저장하세요. 모양은 {PrefabPath}에서 고치세요");
        }

        [MenuItem("BossRaid/UI/Rebuild Bonfire Panel Prefab")]
        private static void Rebuild()
        {
            bool ok = EditorUtility.DisplayDialog("BonfirePanel 다시 만들기",
                $"{PrefabPath}를 처음 모양으로 다시 만듭니다. 프리팹에서 직접 고친 값은 사라집니다.", "다시 만들기", "취소");
            if (ok == false) return;

            BuildPrefab();
            Debug.Log("BonfirePanel 프리팹을 다시 만들었습니다");
        }

        // 씬의 Bonfire.panel 칸에 넣어 줌
        private static void LinkBonfire(BonfirePanel panel)
        {
            Bonfire bonfire = Object.FindAnyObjectByType<Bonfire>();
            if (bonfire == null)
            {
                Debug.LogWarning("씬에 Bonfire가 없습니다. Bonfire의 Panel 칸을 직접 채우세요");
                return;
            }

            var so = new SerializedObject(bonfire);
            so.FindProperty("panel").objectReferenceValue = panel;
            so.ApplyModifiedProperties();
            Debug.Log("Bonfire의 Panel 칸을 채웠습니다");
        }

        // 버튼을 마우스로 누르려면 씬에 EventSystem이 하나 있어야 함 (이 프로젝트는 Input System만 써서 InputSystemUIInputModule)
        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
            Debug.Log("EventSystem을 만들었습니다");
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
            var root = new GameObject("BonfirePanel", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>(); // 버튼 클릭을 받으려면 필요
            var panelScript = root.AddComponent<BonfirePanel>();

            // 아래쪽 안내 "[E] 화톳불"
            TextMeshProUGUI prompt = CreateText("Prompt", (RectTransform)root.transform, font, "[E]  화톳불", 30, TextAlignmentOptions.Center, PromptColor);
            var promptRect = (RectTransform)prompt.transform;
            promptRect.anchorMin = promptRect.anchorMax = promptRect.pivot = new Vector2(0.5f, 0f);
            promptRect.anchoredPosition = new Vector2(0, 160);
            promptRect.sizeDelta = new Vector2(400, 44);

            // 메뉴 창 (어둡게 + 가운데 패널). CanvasGroup으로 한 번에 켜고 끔
            RectTransform windowRect = CreateStretch("Window", root.transform);
            var window = windowRect.gameObject.AddComponent<CanvasGroup>();
            window.alpha = 0f;
            window.blocksRaycasts = false;
            window.interactable = false;

            RectTransform dim = CreateStretch("Dim", windowRect);
            AddImage(dim, null, DimColor);

            var panel = new GameObject("Panel", typeof(RectTransform));
            var panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(windowRect, false);
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(520, 0);
            Image panelImage = AddImage(panelRect, rounded, PanelColor);
            panelImage.type = Image.Type.Sliced;
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = GoldLine;
            outline.effectDistance = new Vector2(1, -1);
            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 26, 26);
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI title = CreateText("Title", panelRect, font, "화톳불", 40, TextAlignmentOptions.Center, TitleColor);
            AddLayout(title.gameObject, height: 52);

            var divider = new GameObject("Divider", typeof(RectTransform));
            divider.transform.SetParent(panelRect, false);
            AddImage((RectTransform)divider.transform, null, GoldLine);
            AddLayout(divider, height: 2);

            TextMeshProUGUI humanity = CreateText("Humanity", panelRect, font, "인간성  0", 28, TextAlignmentOptions.Left, Color.white);
            AddLayout(humanity.gameObject, height: 40);

            TextMeshProUGUI potion = CreateText("Potion", panelRect, font, "회복약 최대  3 / 5", 28, TextAlignmentOptions.Left, Color.white);
            AddLayout(potion.gameObject, height: 40);

            Button burn = CreateButton("BurnButton", panelRect, font, rounded, "인간성 태우기  (회복약 최대 +1)", ButtonColor);

            TextMeshProUGUI reason = CreateText("Reason", panelRect, font, "", 22, TextAlignmentOptions.Center, ReasonColor);
            AddLayout(reason.gameObject, height: 30);

            Button close = CreateButton("CloseButton", panelRect, font, rounded, "닫기  [E]", CloseButtonColor);

            // BonfirePanel의 private 필드 연결
            var so = new SerializedObject(panelScript);
            so.FindProperty("promptText").objectReferenceValue = prompt;
            so.FindProperty("window").objectReferenceValue = window;
            so.FindProperty("humanityText").objectReferenceValue = humanity;
            so.FindProperty("potionText").objectReferenceValue = potion;
            so.FindProperty("burnButton").objectReferenceValue = burn;
            so.FindProperty("reasonText").objectReferenceValue = reason;
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        // 둥근 배경 + 가운데 글자. 못 누를 때(interactable false)는 Button이 색을 어둡게 함
        private static Button CreateButton(string name, RectTransform parent, TMP_FontAsset font, Sprite rounded, string label, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = AddImage((RectTransform)go.transform, rounded, color);
            image.type = Image.Type.Sliced;
            image.raycastTarget = true; // 클릭을 받는 건 배경 이미지
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            AddLayout(go, height: 56);

            TextMeshProUGUI text = CreateText("Label", (RectTransform)go.transform, font, label, 26, TextAlignmentOptions.Center, Color.white);
            var textRect = (RectTransform)text.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
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
        private static void AddLayout(GameObject go, float width = 0, float height = 0)
        {
            var element = go.AddComponent<LayoutElement>();
            if (width > 0) { element.minWidth = width; element.preferredWidth = width; }
            if (height > 0) { element.minHeight = height; element.preferredHeight = height; }
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

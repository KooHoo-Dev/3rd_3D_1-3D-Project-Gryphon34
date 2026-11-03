using BossRaid.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BossRaid.Tools
{
    // 일시정지 창 프리팹을 만들고 열린 씬(마을·투기장)에 놓는 메뉴
    // : 화면 전체 어둡게 + 가운데 창 (제목 "일시정지" / [계속하기] / [나가기])
    // 모양은 BonfirePanelBuilder와 같은 색·버튼을 씀. 프리팹은 처음 한 번만 만들고, 다시 실행하면 씬에 놓기만 합니다
    public static class PauseMenuBuilder
    {
        private const string Folder = "Assets/00. Study/20. BossRaid/UI";
        private const string PrefabPath = Folder + "/PauseMenu.prefab";

        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f); // 화톳불 창보다 진하게 (멈췄다는 느낌)

        [MenuItem("BossRaid/UI/Create Pause Menu")]
        private static void Create()
        {
            PauseMenu existing = Object.FindAnyObjectByType<PauseMenu>();
            if (existing != null)
            {
                Debug.Log("이 씬에는 이미 PauseMenu가 있습니다");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) prefab = BuildPrefab();
            if (prefab == null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Create Pause Menu");
            Selection.activeGameObject = instance;
            BonfirePanelBuilder.EnsureEventSystem();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"PauseMenu를 씬에 놓았습니다. 씬을 저장하세요. 모양은 {PrefabPath}에서 고치세요");
        }

        [MenuItem("BossRaid/UI/Rebuild Pause Menu Prefab")]
        private static void Rebuild()
        {
            bool ok = EditorUtility.DisplayDialog("PauseMenu 다시 만들기",
                $"{PrefabPath}를 처음 모양으로 다시 만듭니다. 프리팹에서 직접 고친 값은 사라집니다.", "다시 만들기", "취소");
            if (ok == false) return;

            if (BuildPrefab() != null) Debug.Log("PauseMenu 프리팹을 다시 만들었습니다");
        }

        private static GameObject BuildPrefab()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BonfirePanelBuilder.KoreanFontPath);
            if (font == null)
            {
                Debug.LogError($"한글 폰트가 없습니다: {BonfirePanelBuilder.KoreanFontPath}");
                return null;
            }

            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            GameObject root = BuildHierarchy(font, rounded);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildHierarchy(TMP_FontAsset font, Sprite rounded)
        {
            // 캔버스 (HUD·화톳불 창·결과 패널보다 위에 그림)
            var root = new GameObject("PauseMenu", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            var menu = root.AddComponent<PauseMenu>();

            RectTransform windowRect = BonfirePanelBuilder.CreateStretch("Window", root.transform);
            var window = windowRect.gameObject.AddComponent<CanvasGroup>();
            window.alpha = 0f;
            window.blocksRaycasts = false;
            window.interactable = false;

            RectTransform dim = BonfirePanelBuilder.CreateStretch("Dim", windowRect);
            Image dimImage = BonfirePanelBuilder.AddImage(dim, null, DimColor);
            dimImage.raycastTarget = true; // 뒤의 화톳불 창 버튼 등이 눌리지 않게

            var panel = new GameObject("Panel", typeof(RectTransform));
            var panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(windowRect, false);
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(420, 0);
            Image panelImage = BonfirePanelBuilder.AddImage(panelRect, rounded, BonfirePanelBuilder.PanelColor);
            panelImage.type = Image.Type.Sliced;
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = BonfirePanelBuilder.GoldLine;
            outline.effectDistance = new Vector2(1, -1);
            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 26, 30);
            layout.spacing = 14;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI title = BonfirePanelBuilder.CreateText("Title", panelRect, font, "일시정지", 40, TextAlignmentOptions.Center, BonfirePanelBuilder.TitleColor);
            BonfirePanelBuilder.AddLayout(title.gameObject, height: 52);

            var divider = new GameObject("Divider", typeof(RectTransform));
            divider.transform.SetParent(panelRect, false);
            BonfirePanelBuilder.AddImage((RectTransform)divider.transform, null, BonfirePanelBuilder.GoldLine);
            BonfirePanelBuilder.AddLayout(divider, height: 2);

            Button resume = BonfirePanelBuilder.CreateButton("ResumeButton", panelRect, font, rounded, "계속하기  [Esc]", BonfirePanelBuilder.ButtonColor);
            Button quit = BonfirePanelBuilder.CreateButton("QuitButton", panelRect, font, rounded, "나가기  (메인메뉴로)", BonfirePanelBuilder.CloseButtonColor);

            // PauseMenu의 private 필드 연결
            var so = new SerializedObject(menu);
            so.FindProperty("window").objectReferenceValue = window;
            so.FindProperty("resumeButton").objectReferenceValue = resume;
            so.FindProperty("quitButton").objectReferenceValue = quit;
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }
    }
}

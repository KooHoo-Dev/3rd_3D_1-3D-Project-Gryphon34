using BossRaid.Flow;
using BossRaid.Player;
using BossRaid.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using B = BossRaid.Tools.BonfirePanelBuilder;

namespace BossRaid.Tools
{
    // 대장간·냄비 메뉴 프리팹을 만들고 열린 씬(마을)에 놓는 메뉴 (BonfirePanelBuilder와 같은 방식, 모양도 같은 색·폰트)
    // : 아래쪽 "[E] 대장간" 안내 + 가운데 창 (제목 / (냄비만) 지금 식사 / 가진 것 / 줄 목록 / [닫기])
    // 줄 목록 안에 꺼 둔 줄 틀(MenuRow) 하나 → 패널 코드가 장비·요리마다 복제
    // 씬에 놓을 때 Forge/CookingPot의 panel 칸(+ 비어 있으면 player 칸)을 채우고, EventSystem이 없으면 같이 만듭니다
    public static class VillageMenuBuilder
    {
        private const string Folder = "Assets/00. Study/20. BossRaid/UI";
        private const string ForgePrefabPath = Folder + "/ForgePanel.prefab";
        private const string CookingPrefabPath = Folder + "/CookingPanel.prefab";

        private static readonly Color RowColor = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color InfoColor = new Color(0.92f, 0.92f, 0.92f);
        private static readonly Color CostColor = new Color(0.7f, 0.7f, 0.7f);
        private static readonly Color EquipButtonColor = new Color(0.16f, 0.22f, 0.3f, 1f);

        private const float PanelWidth = 900f;
        private const float RowHeight = 130f;
        private const float RowButtonWidth = 140f;

        [MenuItem("BossRaid/UI/Create Forge Panel")]
        private static void CreateForge()
        {
            ForgePanel existing = Object.FindAnyObjectByType<ForgePanel>();
            if (existing != null)
            {
                Debug.Log("이 씬에는 이미 ForgePanel이 있습니다");
                Selection.activeGameObject = existing.gameObject;
                LinkForge(existing);
                return;
            }

            GameObject instance = PlacePrefab(ForgePrefabPath, false);
            if (instance == null) return;
            LinkForge(instance.GetComponent<ForgePanel>());
            Debug.Log($"ForgePanel을 씬에 놓았습니다. 씬을 저장하세요. 모양은 {ForgePrefabPath}에서 고치세요");
        }

        [MenuItem("BossRaid/UI/Create Cooking Panel")]
        private static void CreateCooking()
        {
            CookingPanel existing = Object.FindAnyObjectByType<CookingPanel>();
            if (existing != null)
            {
                Debug.Log("이 씬에는 이미 CookingPanel이 있습니다");
                Selection.activeGameObject = existing.gameObject;
                LinkCookingPot(existing);
                return;
            }

            GameObject instance = PlacePrefab(CookingPrefabPath, true);
            if (instance == null) return;
            LinkCookingPot(instance.GetComponent<CookingPanel>());
            Debug.Log($"CookingPanel을 씬에 놓았습니다. 씬을 저장하세요. 모양은 {CookingPrefabPath}에서 고치세요");
        }

        [MenuItem("BossRaid/UI/Rebuild Forge·Cooking Panel Prefabs")]
        private static void Rebuild()
        {
            bool ok = EditorUtility.DisplayDialog("대장간·냄비 패널 다시 만들기",
                $"{ForgePrefabPath}, {CookingPrefabPath}를 처음 모양으로 다시 만듭니다. 프리팹에서 직접 고친 값은 사라집니다.", "다시 만들기", "취소");
            if (ok == false) return;

            BuildPrefab(ForgePrefabPath, false);
            BuildPrefab(CookingPrefabPath, true);
            Debug.Log("대장간·냄비 패널 프리팹을 다시 만들었습니다");
        }

        // 프리팹이 없으면 만들고, 씬에 하나 놓음
        private static GameObject PlacePrefab(string path, bool cooking)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) prefab = BuildPrefab(path, cooking);
            if (prefab == null) return null;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, cooking ? "Create Cooking Panel" : "Create Forge Panel");
            Selection.activeGameObject = instance;

            B.EnsureEventSystem();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return instance;
        }

        // 씬의 Forge.panel 칸에 넣고, player 칸이 비어 있으면 씬의 플레이어를 넣음
        private static void LinkForge(ForgePanel panel)
        {
            Forge forge = Object.FindAnyObjectByType<Forge>(FindObjectsInactive.Include);
            if (forge == null)
            {
                Debug.LogWarning("씬에 Forge가 없습니다. 대장간 오브젝트에 Forge를 붙이고 이 메뉴를 한 번 더 누르면 칸을 채웁니다");
                return;
            }
            LinkOwner(forge, panel);
            Debug.Log($"{forge.name}의 Panel 칸을 채웠습니다");
        }

        private static void LinkCookingPot(CookingPanel panel)
        {
            CookingPot pot = Object.FindAnyObjectByType<CookingPot>(FindObjectsInactive.Include);
            if (pot == null)
            {
                Debug.LogWarning("씬에 CookingPot이 없습니다. 냄비 오브젝트에 CookingPot을 붙이고 이 메뉴를 한 번 더 누르면 칸을 채웁니다");
                return;
            }
            LinkOwner(pot, panel);
            Debug.Log($"{pot.name}의 Panel 칸을 채웠습니다");
        }

        private static void LinkOwner(Object owner, Object panel)
        {
            var so = new SerializedObject(owner);
            so.FindProperty("panel").objectReferenceValue = panel;

            SerializedProperty player = so.FindProperty("player");
            if (player.objectReferenceValue == null)
            {
                player.objectReferenceValue = Object.FindAnyObjectByType<PlayerController>();
                if (player.objectReferenceValue == null) Debug.LogWarning("씬에 PlayerController가 없어서 Player 칸은 직접 채우세요");
            }
            so.ApplyModifiedProperties();
        }

        private static GameObject BuildPrefab(string path, bool cooking)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(B.KoreanFontPath);
            if (font == null)
            {
                Debug.LogError($"한글 폰트가 없습니다: {B.KoreanFontPath}");
                return null;
            }
            if (AssetDatabase.IsValidFolder(Folder) == false)
                AssetDatabase.CreateFolder("Assets/00. Study/20. BossRaid", "UI");

            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            GameObject root = BuildHierarchy(cooking, font, rounded);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildHierarchy(bool cooking, TMP_FontAsset font, Sprite rounded)
        {
            string title = cooking ? "냄비" : "대장간";

            // 캔버스 (HUD보다 위에 그림)
            var root = new GameObject(cooking ? "CookingPanel" : "ForgePanel", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>(); // 버튼 클릭을 받으려면 필요
            MonoBehaviour panelScript = cooking ? root.AddComponent<CookingPanel>() : root.AddComponent<ForgePanel>();

            // 아래쪽 안내 "[E] 대장간"
            TextMeshProUGUI prompt = B.CreateText("Prompt", (RectTransform)root.transform, font, $"[E]  {title}", 30, TextAlignmentOptions.Center, B.PromptColor);
            var promptRect = (RectTransform)prompt.transform;
            promptRect.anchorMin = promptRect.anchorMax = promptRect.pivot = new Vector2(0.5f, 0f);
            promptRect.anchoredPosition = new Vector2(0, 160);
            promptRect.sizeDelta = new Vector2(400, 44);

            // 메뉴 창 (어둡게 + 가운데 패널). CanvasGroup으로 한 번에 켜고 끔
            RectTransform windowRect = B.CreateStretch("Window", root.transform);
            var window = windowRect.gameObject.AddComponent<CanvasGroup>();
            window.alpha = 0f;
            window.blocksRaycasts = false;
            window.interactable = false;

            RectTransform dim = B.CreateStretch("Dim", windowRect);
            B.AddImage(dim, null, B.DimColor);

            var panel = new GameObject("Panel", typeof(RectTransform));
            var panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(windowRect, false);
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(PanelWidth, 0);
            Image panelImage = B.AddImage(panelRect, rounded, B.PanelColor);
            panelImage.type = Image.Type.Sliced;
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = B.GoldLine;
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
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // 줄이 늘면 창도 늘어남

            TextMeshProUGUI titleText = B.CreateText("Title", panelRect, font, title, 40, TextAlignmentOptions.Center, B.TitleColor);
            B.AddLayout(titleText.gameObject, height: 52);

            var divider = new GameObject("Divider", typeof(RectTransform));
            divider.transform.SetParent(panelRect, false);
            B.AddImage((RectTransform)divider.transform, null, B.GoldLine);
            B.AddLayout(divider, height: 2);

            TextMeshProUGUI meal = null;
            if (cooking)
            {
                meal = B.CreateText("Meal", panelRect, font, "지금 식사: 없음", 26, TextAlignmentOptions.Left, Color.white);
                B.AddLayout(meal.gameObject, height: 36);
            }

            TextMeshProUGUI wallet = B.CreateText("Wallet", panelRect, font, "소울 0", 24, TextAlignmentOptions.Left, CostColor);
            B.AddLayout(wallet.gameObject, height: 34);

            // 줄 목록. 줄 틀은 꺼 둠 (꺼진 건 레이아웃이 무시, 패널 코드가 복제해서 켬)
            var rows = new GameObject("Rows", typeof(RectTransform));
            var rowsRect = (RectTransform)rows.transform;
            rowsRect.SetParent(panelRect, false);
            var rowsLayout = rows.AddComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 8;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;

            MenuRow rowTemplate = BuildRow(rowsRect, font, rounded, withSub: cooking == false);
            rowTemplate.gameObject.SetActive(false);

            Button close = B.CreateButton("CloseButton", panelRect, font, rounded, "닫기  [E]", B.CloseButtonColor);

            // 패널의 private 필드 연결 (ForgePanel·CookingPanel 칸 이름이 같음, mealText만 냄비에)
            var so = new SerializedObject(panelScript);
            so.FindProperty("promptText").objectReferenceValue = prompt;
            so.FindProperty("window").objectReferenceValue = window;
            so.FindProperty("walletText").objectReferenceValue = wallet;
            so.FindProperty("rowParent").objectReferenceValue = rowsRect;
            so.FindProperty("rowTemplate").objectReferenceValue = rowTemplate;
            so.FindProperty("closeButton").objectReferenceValue = close;
            if (cooking) so.FindProperty("mealText").objectReferenceValue = meal;
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        // 줄 하나: [이름 / 효과 / 비용] [주 버튼] [보조 버튼(대장간만)]
        private static MenuRow BuildRow(RectTransform parent, TMP_FontAsset font, Sprite rounded, bool withSub)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            Image background = B.AddImage(rect, rounded, RowColor);
            background.type = Image.Type.Sliced;
            var rowLayout = go.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(16, 16, 10, 10);
            rowLayout.spacing = 12;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            B.AddLayout(go, height: RowHeight);

            // 글자 칸 (남는 가로를 다 씀, 긴 글은 줄바꿈)
            var texts = new GameObject("Texts", typeof(RectTransform));
            var textsRect = (RectTransform)texts.transform;
            textsRect.SetParent(rect, false);
            var textsElement = texts.AddComponent<LayoutElement>();
            textsElement.flexibleWidth = 1;
            textsElement.minWidth = 0;
            var textsLayout = texts.AddComponent<VerticalLayoutGroup>();
            textsLayout.spacing = 2;
            textsLayout.childAlignment = TextAnchor.MiddleLeft;
            textsLayout.childControlWidth = true;
            textsLayout.childControlHeight = true;
            textsLayout.childForceExpandWidth = true;
            textsLayout.childForceExpandHeight = false;

            TextMeshProUGUI title = B.CreateText("Title", textsRect, font, "장비 이름", 28, TextAlignmentOptions.Left, B.TitleColor);
            TextMeshProUGUI info = B.CreateText("Info", textsRect, font, "효과", 22, TextAlignmentOptions.Left, InfoColor);
            TextMeshProUGUI cost = B.CreateText("Cost", textsRect, font, "비용", 20, TextAlignmentOptions.Left, CostColor);

            Button main = B.CreateButton("MainButton", rect, font, rounded, "제작", B.ButtonColor);
            SetButtonWidth(main);

            Button sub = null;
            if (withSub)
            {
                sub = B.CreateButton("SubButton", rect, font, rounded, "장착", EquipButtonColor);
                SetButtonWidth(sub);
            }

            var row = go.AddComponent<MenuRow>();
            var so = new SerializedObject(row);
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("infoText").objectReferenceValue = info;
            so.FindProperty("costText").objectReferenceValue = cost;
            so.FindProperty("mainButton").objectReferenceValue = main;
            so.FindProperty("mainLabel").objectReferenceValue = main.GetComponentInChildren<TMP_Text>();
            if (sub != null)
            {
                so.FindProperty("subButton").objectReferenceValue = sub;
                so.FindProperty("subLabel").objectReferenceValue = sub.GetComponentInChildren<TMP_Text>();
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return row;
        }

        // CreateButton은 높이만 정해서, 줄 안에서는 너비도 고정 (안 하면 글자 길이만큼 늘었다 줄었다 함)
        private static void SetButtonWidth(Button button)
        {
            var element = button.GetComponent<LayoutElement>();
            element.minWidth = RowButtonWidth;
            element.preferredWidth = RowButtonWidth;
        }
    }
}

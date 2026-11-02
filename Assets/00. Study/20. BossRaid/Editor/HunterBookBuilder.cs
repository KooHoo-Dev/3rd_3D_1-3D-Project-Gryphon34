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
    // 헌터북 프리팹을 만들고 열린 씬(마을)에 놓는 메뉴 (VillageMenuBuilder와 같은 방식, 모양도 같은 색·폰트)
    // : 오른쪽 아래 "[Tab] 헌터북" 힌트 + 위 가운데 알림 한 줄 + 가운데 창 (제목 / 왼쪽 페이지 목록 · 오른쪽 제목과 본문(스크롤) / [닫기])
    // 페이지 목록 안에 꺼 둔 줄 버튼 하나 → HunterBookPanel이 열린 페이지마다 복제
    // 페이지 초안 7개를 같이 넣습니다 (글은 프리팹의 Pages에서 고치세요)
    public static class HunterBookBuilder
    {
        private const string Folder = "Assets/00. Study/20. BossRaid/UI";
        private const string PrefabPath = Folder + "/HunterBookPanel.prefab";

        private static readonly Color RowColor = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color BodyColor = new Color(0.92f, 0.92f, 0.92f);

        private const float PanelWidth = 1300f;
        private const float PanelHeight = 780f;
        private const float ListWidth = 320f;

        [MenuItem("BossRaid/UI/Create Hunter Book")]
        private static void Create()
        {
            HunterBookPanel existing = Object.FindAnyObjectByType<HunterBookPanel>();
            if (existing != null)
            {
                Debug.Log("이 씬에는 이미 HunterBookPanel이 있습니다");
                Selection.activeGameObject = existing.gameObject;
                LinkPlayer(existing);
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) prefab = BuildPrefab();
            if (prefab == null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Create Hunter Book");
            Selection.activeGameObject = instance;
            LinkPlayer(instance.GetComponent<HunterBookPanel>());

            B.EnsureEventSystem();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"HunterBookPanel을 씬에 놓았습니다. 씬을 저장하세요. 모양·페이지 글은 {PrefabPath}에서 고치세요");
        }

        [MenuItem("BossRaid/UI/Rebuild Hunter Book Prefab")]
        private static void Rebuild()
        {
            bool ok = EditorUtility.DisplayDialog("헌터북 다시 만들기",
                $"{PrefabPath}를 처음 모양과 초안 글로 다시 만듭니다. 프리팹에서 직접 고친 값과 글은 사라집니다.", "다시 만들기", "취소");
            if (ok == false) return;

            BuildPrefab();
            Debug.Log("헌터북 프리팹을 다시 만들었습니다");
        }

        // 씬의 플레이어를 player 칸에 넣음 (프리팹에는 씬 오브젝트를 못 넣어서 씬에 놓을 때마다)
        private static void LinkPlayer(HunterBookPanel panel)
        {
            var so = new SerializedObject(panel);
            SerializedProperty player = so.FindProperty("player");
            if (player.objectReferenceValue == null)
            {
                player.objectReferenceValue = Object.FindAnyObjectByType<PlayerController>();
                if (player.objectReferenceValue == null) Debug.LogWarning("씬에 PlayerController가 없어서 Player 칸은 직접 채우세요");
            }
            so.ApplyModifiedProperties();
        }

        private static GameObject BuildPrefab()
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

            GameObject root = BuildHierarchy(font, rounded);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildHierarchy(TMP_FontAsset font, Sprite rounded)
        {
            // 캔버스 (HUD보다 위에 그림)
            var root = new GameObject("HunterBookPanel", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>(); // 버튼 클릭을 받으려면 필요
            var panelScript = root.AddComponent<HunterBookPanel>();

            // 오른쪽 아래 힌트 "[Tab] 헌터북"
            TextMeshProUGUI hint = B.CreateText("Hint", (RectTransform)root.transform, font, "[Tab] 헌터북", 26, TextAlignmentOptions.Right, B.PromptColor);
            var hintRect = (RectTransform)hint.transform;
            hintRect.anchorMin = hintRect.anchorMax = hintRect.pivot = new Vector2(1f, 0f);
            hintRect.anchoredPosition = new Vector2(-40, 40);
            hintRect.sizeDelta = new Vector2(420, 40);

            // 위 가운데 알림 한 줄
            TextMeshProUGUI notice = B.CreateText("Notice", (RectTransform)root.transform, font, "새 페이지: …  —  [Tab] 헌터북", 32, TextAlignmentOptions.Center, B.TitleColor);
            var noticeRect = (RectTransform)notice.transform;
            noticeRect.anchorMin = noticeRect.anchorMax = noticeRect.pivot = new Vector2(0.5f, 1f);
            noticeRect.anchoredPosition = new Vector2(0, -120);
            noticeRect.sizeDelta = new Vector2(1500, 50);
            notice.gameObject.SetActive(false);

            // 책 창 (어둡게 + 가운데 패널). CanvasGroup으로 한 번에 켜고 끔
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
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
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

            TextMeshProUGUI bookTitle = B.CreateText("Title", panelRect, font, "헌터북", 40, TextAlignmentOptions.Center, B.TitleColor);
            B.AddLayout(bookTitle.gameObject, height: 52);

            var divider = new GameObject("Divider", typeof(RectTransform));
            divider.transform.SetParent(panelRect, false);
            B.AddImage((RectTransform)divider.transform, null, B.GoldLine);
            B.AddLayout(divider, height: 2);

            // 가운데: 왼쪽 목록 | 오른쪽 페이지 (남는 높이를 다 씀)
            var content = new GameObject("Content", typeof(RectTransform));
            var contentRect = (RectTransform)content.transform;
            contentRect.SetParent(panelRect, false);
            content.AddComponent<LayoutElement>().flexibleHeight = 1;
            var contentLayout = content.AddComponent<HorizontalLayoutGroup>();
            contentLayout.spacing = 24;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = true;

            // 왼쪽 페이지 목록. 줄 버튼은 꺼 둠 (꺼진 건 레이아웃이 무시, 패널 코드가 복제해서 켬)
            var list = new GameObject("PageList", typeof(RectTransform));
            var listRect = (RectTransform)list.transform;
            listRect.SetParent(contentRect, false);
            B.AddLayout(list, width: ListWidth);
            var listLayout = list.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 8;
            listLayout.childAlignment = TextAnchor.UpperLeft;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            Button rowTemplate = B.CreateButton("PageRow", listRect, font, rounded, "페이지 제목", RowColor);
            var rowLabel = rowTemplate.GetComponentInChildren<TextMeshProUGUI>();
            rowLabel.alignment = TextAlignmentOptions.Left;
            rowLabel.fontSize = 24;
            ((RectTransform)rowLabel.transform).offsetMin = new Vector2(18, 0); // 왼쪽 여백
            rowTemplate.gameObject.SetActive(false);

            // 오른쪽 페이지: 제목 + 본문 (길면 스크롤)
            var view = new GameObject("PageView", typeof(RectTransform));
            var viewRect = (RectTransform)view.transform;
            viewRect.SetParent(contentRect, false);
            view.AddComponent<LayoutElement>().flexibleWidth = 1;
            var viewLayout = view.AddComponent<VerticalLayoutGroup>();
            viewLayout.spacing = 10;
            viewLayout.childControlWidth = true;
            viewLayout.childControlHeight = true;
            viewLayout.childForceExpandWidth = true;
            viewLayout.childForceExpandHeight = false;

            TextMeshProUGUI pageTitle = B.CreateText("PageTitle", viewRect, font, "페이지 제목", 32, TextAlignmentOptions.Left, B.TitleColor);
            B.AddLayout(pageTitle.gameObject, height: 44);

            ScrollRect scroll = BuildBodyScroll(viewRect, font, out TextMeshProUGUI body);

            Button close = B.CreateButton("CloseButton", panelRect, font, rounded, "닫기  [Tab]", B.CloseButtonColor);

            // 패널의 private 필드 연결 + 페이지 초안
            var so = new SerializedObject(panelScript);
            so.FindProperty("window").objectReferenceValue = window;
            so.FindProperty("hintText").objectReferenceValue = hint;
            so.FindProperty("noticeText").objectReferenceValue = notice;
            so.FindProperty("rowParent").objectReferenceValue = listRect;
            so.FindProperty("rowTemplate").objectReferenceValue = rowTemplate;
            so.FindProperty("titleText").objectReferenceValue = pageTitle;
            so.FindProperty("bodyText").objectReferenceValue = body;
            so.FindProperty("bodyScroll").objectReferenceValue = scroll;
            so.FindProperty("closeButton").objectReferenceValue = close;
            FillDraftPages(so.FindProperty("pages"));
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        // 본문 스크롤: ScrollRect > Viewport(RectMask2D) > Content(글 높이만큼 늘어남) > Body
        private static ScrollRect BuildBodyScroll(RectTransform parent, TMP_FontAsset font, out TextMeshProUGUI body)
        {
            var go = new GameObject("BodyScroll", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            go.AddComponent<LayoutElement>().flexibleHeight = 1;

            RectTransform viewport = B.CreateStretch("Viewport", rect);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image hitArea = B.AddImage(viewport, null, new Color(0, 0, 0, 0));
            hitArea.raycastTarget = true; // 휠·드래그를 받을 자리 (투명)

            var contentGo = new GameObject("Content", typeof(RectTransform));
            var contentRect = (RectTransform)contentGo.transform;
            contentRect.SetParent(viewport, false);
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 12, 0, 12);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            body = B.CreateText("Body", contentRect, font, "본문", 24, TextAlignmentOptions.TopLeft, BodyColor);
            body.lineSpacing = 12;

            var scroll = go.AddComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;
            return scroll;
        }

        // 초안. 지금 게임 규칙 기준 (11/02). 수치는 일부러 거의 안 씀 (밸런스 때 안 고쳐도 되게)
        private static void FillDraftPages(SerializedProperty pages)
        {
            (string id, string title, PageUnlock unlock, string body)[] drafts =
            {
                ("Controls", "조작", PageUnlock.Start,
                    "이동  WASD\n" +
                    "달리기  Left Shift (누르고 있는 동안, 스태미나를 씀)\n" +
                    "카메라  마우스\n" +
                    "타겟 카메라  Q (보스를 화면 가운데에 둠)\n\n" +
                    "<color=#F9C775>공격  좌클릭</color>\n" +
                    "누르면 칼을 들고, 누르고 있으면 힘을 모으고(차지), 떼면 내려칩니다.\n" +
                    "오래 모을수록 강하게 들어갑니다. 몸을 감싸는 빛의 색으로 차지 단계를 알 수 있습니다.\n" +
                    "이어서 누르면 3타까지 이어집니다.\n\n" +
                    "<color=#F9C775>구르기  Space</color>\n" +
                    "구르는 순간에는 맞지 않습니다. 큰 공격, 가드할 수 없는 공격은 굴러서 피하세요.\n\n" +
                    "<color=#F9C775>가드  Left Ctrl</color> (누르고 있는 동안)\n" +
                    "앞에서 오는 공격만 막습니다. 막으면 밀려나고 스태미나가 줄어듭니다.\n" +
                    "맞기 직전에 누르면 <color=#F9C775>퍼펙트 가드</color>: 밀리지 않고, 보스가 지치고, 다음 공격의 차지가 한 단계 앞서 시작합니다.\n" +
                    "가드할 수 없는 공격도 있습니다.\n\n" +
                    "<color=#F9C775>회복약  R</color>\n" +
                    "마우스 휠로 회복약과 인간성 중 쓸 것을 고릅니다."),

                ("Death", "죽음과 소울", PageUnlock.Start,
                    "보스에게 준 피해만큼 소울을 받습니다. 쓰러뜨리지 못하고 죽어도 받습니다.\n" +
                    "쓰러뜨리면 소울을 가장 많이 받습니다.\n\n" +
                    "대신 죽으면 그때 <color=#F9C775>들고 있던 소울은 투기장에 떨어집니다.</color>\n" +
                    "다음에 같은 단계의 투기장에 들어가 그 자리에 가면 다시 주울 수 있습니다.\n" +
                    "줍지 못하고 또 죽으면 절반만 화톳불로 돌아오고 나머지는 사라집니다.\n\n" +
                    "소울은 장비를 만들고 강화하고, 요리를 하는 데 씁니다.\n" +
                    "모아 둔 소울이 많을수록 잃을 것도 많습니다."),

                ("Parts", "부위파괴와 소재", PageUnlock.FirstDeath,
                    "용의 <color=#F9C775>머리(뿔), 꼬리, 양 앞발</color>은 따로 깨집니다. 한 부위를 계속 때리면 깨집니다.\n" +
                    "깨지는 순간 몸이 번쩍이고 금이 간 자국이 남습니다. 꼬리는 잘려 나갑니다.\n" +
                    "깨진 부위는 더 아프게 들어갑니다.\n\n" +
                    "부위가 깨지면 그 부위의 소재를 얻습니다.\n" +
                    "뿔 → 뿔 / 꼬리 → 꼬리 고기, 꼬리 가시 / 앞발 → 화룡 비늘\n" +
                    "보스의 체력을 많이 깎아도 화룡 비늘이 나옵니다.\n\n" +
                    "<color=#F9C775>이번 판에 얻은 소재는 죽어도 받습니다.</color>\n" +
                    "만들고 싶은 장비에 필요한 부위를 노리세요."),

                ("Forge", "대장간", PageUnlock.FirstDeath,
                    "마을의 대장간에서 [E]로 무기와 방어구를 만들고 강화합니다. 소울과 보스 소재가 필요합니다.\n\n" +
                    "<color=#F9C775>무기</color>\n" +
                    "철 대검: 가드가 강합니다. 막을 때 스태미나가 덜 듭니다.\n" +
                    "꼬리뼈 대검: 공격력이 가장 높습니다.\n" +
                    "용뿔 대검: 깨진 부위에 더 아프게 들어갑니다.\n\n" +
                    "<color=#F9C775>방어구</color>\n" +
                    "받는 피해를 줄입니다. 화룡 비늘 세트는 불 피해와 가드 스태미나도 줄여 줍니다.\n\n" +
                    "만든 장비는 모두 가지고 있고, 대장간에서 골라 낄 수 있습니다."),

                ("Humanity", "인간성과 화톳불", PageUnlock.Stage1Cleared,
                    "인간성은 보스를 처음 쓰러뜨리면 얻습니다. 부위를 깨거나 다시 쓰러뜨릴 때도 가끔 얻습니다.\n\n" +
                    "<color=#F9C775>태우기</color>  화톳불에서 [E]\n" +
                    "회복약의 최대 개수가 하나 늘어납니다. 영원히 남습니다.\n\n" +
                    "<color=#F9C775>사용</color>  마우스 휠로 인간성을 고르고 R\n" +
                    "체력을 전부 회복하고 사람 상태가 되어 최대 체력이 늘어납니다.\n" +
                    "사람 상태는 죽으면 사라집니다.\n\n" +
                    "지금 강해질지, 회복약을 영원히 늘릴지 고르세요."),

                ("Cooking", "요리", PageUnlock.Stage1Cleared,
                    "화톳불 옆 냄비에서 [E]로 요리합니다.\n\n" +
                    "출격 전에 <color=#F9C775>한 끼</color>만 먹을 수 있고, 먹자마자 효과가 생깁니다.\n" +
                    "사냥이 끝나면(쓰러뜨리든 죽든) 효과는 사라집니다.\n" +
                    "다른 요리로 바꿀 수 있지만 먹던 효과는 사라집니다.\n\n" +
                    "꼬리 고기, 화룡 심장을 처음 얻으면 새 요리가 열립니다.\n" +
                    "불이 무섭다면 화룡 심장 찜을 찾아보세요."),

                ("Stage2", "2단계: 붉은 용", PageUnlock.Stage1Cleared,
                    "마을의 두 번째 안개문으로 2단계에 도전합니다. 더 단단하고 더 아프게 때립니다.\n\n" +
                    "<color=#F9C775>불</color>\n" +
                    "브레스나 불덩이에 맞으면 몸에 불이 붙어 체력이 계속 닳습니다.\n" +
                    "구르면 꺼지고, 가만히 있어도 시간이 지나면 꺼집니다. 가드로 막으면 불이 붙지 않습니다.\n" +
                    "체력이 절반 아래로 떨어지면 포효하고, 그 뒤로는 앞발과 물기에도 불이 붙습니다.\n\n" +
                    "<color=#F9C775>하늘</color>\n" +
                    "날아올라 불덩이를 쏘거나 바닥에 불을 뿌립니다. 불바다 안에서는 굴러도 꺼지지 않으니 먼저 빠져나오세요.\n" +
                    "내려앉을 때 덮칩니다. 가드할 수 없으니 굴러서 피하세요.\n" +
                    "양쪽 날개를 모두 깨면 더 이상 날지 못합니다.\n\n" +
                    "불 저항(화룡 비늘 세트, 화룡 심장 찜)으로 대비하세요."),
            };

            pages.arraySize = drafts.Length;
            for (int i = 0; i < drafts.Length; i++)
            {
                SerializedProperty page = pages.GetArrayElementAtIndex(i);
                page.FindPropertyRelative("id").stringValue = drafts[i].id;
                page.FindPropertyRelative("title").stringValue = drafts[i].title;
                page.FindPropertyRelative("unlock").enumValueIndex = (int)drafts[i].unlock;
                page.FindPropertyRelative("body").stringValue = drafts[i].body;
            }
        }
    }
}

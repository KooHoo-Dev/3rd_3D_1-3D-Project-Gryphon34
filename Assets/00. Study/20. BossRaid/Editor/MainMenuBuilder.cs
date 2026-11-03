using BossRaid.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BossRaid.Tools
{
    // 메인메뉴 화면을 열린 씬(03. MainMenu)에 만드는 메뉴 (몬헌 월드 타이틀 형식)
    // : 배경 사진(꽉 채움) + 살짝 어둡게 + 가운데 제목 + 금색 줄 + 깜빡이는 "Press Any Button" + 아래 작은 글씨 + 페이드용 검은 화면
    // 씬에 한 번만 만드는 화면이라 프리팹 없이 씬에 바로 만듭니다. 이미 있으면 지우고 다시 만듦 (클릭 소리는 옮겨 줌)
    public static class MainMenuBuilder
    {
        // TMP 기본 폰트(LiberationSans)에는 한글이 없어서 네모로 나옴 → 한글 폰트를 씀
        private const string KoreanFontPath = "Assets/99. External Assets/Font/Noto_Sans_KR/static/NotoSansKR-Regular SDF.asset";
        // 배경 사진 (Texture Type = Sprite, Sprite Mode = Single). 없으면 단색 배경
        private const string BackgroundPath = "Assets/00. Study/20. BossRaid/Prefab/Gemini_Generated_Image_rsxo97rsxo97rsxo.png";

        private const string TitleLabel = "BOSS RAID";
        private const string PressLabel = "Press Any Button";
        private const string FooterLabel = "Gryphon34 · 2026";

        private static readonly Color BackgroundColor = new Color(0.08f, 0.08f, 0.09f); // 사진이 없을 때 빈 배경
        private static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.3f);         // 화면 전체를 살짝 어둡게 → 흰 글자가 묻히지 않게
        private static readonly Color TitleColor = new Color(0.93f, 0.92f, 0.88f);
        private static readonly Color GoldLine = new Color(0.85f, 0.7f, 0.3f, 0.8f);
        private static readonly Color PressColor = new Color(0.95f, 0.93f, 0.88f);
        private static readonly Color FooterColor = new Color(1f, 1f, 1f, 0.55f);

        [MenuItem("BossRaid/UI/Create Main Menu")]
        private static void Create()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (font == null)
            {
                Debug.LogError($"한글 폰트가 없습니다: {KoreanFontPath}");
                return;
            }

            // 이미 있으면 클릭 소리만 챙기고 지움 (예전 왼쪽 버튼 형식 → 새 형식으로 바꿀 때)
            AudioClip clickSound = null;
            MainMenu existing = Object.FindAnyObjectByType<MainMenu>();
            if (existing != null)
            {
                clickSound = new SerializedObject(existing).FindProperty("clickSound").objectReferenceValue as AudioClip;
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            GameObject root = BuildHierarchy(font, clickSound);
            Undo.RegisterCreatedObjectUndo(root, "Create Main Menu");
            BonfirePanelBuilder.EnsureEventSystem();
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log(existing != null ? "MainMenu를 새 형식으로 다시 만들었습니다. 씬을 저장하세요" : "MainMenu를 만들었습니다. 씬을 저장하세요");
        }

        private static GameObject BuildHierarchy(TMP_FontAsset font, AudioClip clickSound)
        {
            var root = new GameObject("MainMenu", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            var menu = root.AddComponent<MainMenu>();
            var audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D

            // 배경: 화면을 꽉 채우고 넘치는 쪽은 잘림. 비율은 사진에서 읽음
            var background = new GameObject("Background", typeof(RectTransform));
            var backgroundRect = (RectTransform)background.transform;
            backgroundRect.SetParent(root.transform, false);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            if (sprite == null) Debug.LogWarning($"배경 사진을 스프라이트로 못 읽었습니다 (Texture Type = Sprite, Sprite Mode = Single인지 보기): {BackgroundPath}");
            Image backgroundImage = AddImage(backgroundRect, sprite, sprite != null ? Color.white : BackgroundColor);
            backgroundImage.preserveAspect = false;
            var aspect = background.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = sprite != null ? sprite.rect.width / sprite.rect.height : 16f / 9f;

            RectTransform shade = CreateRect("Shade", root.transform, Vector2.zero, Vector2.one);
            AddImage(shade, null, ShadeColor);

            // 제목: 화면 가운데보다 조금 위. 글자 사이를 벌려서 로고처럼
            TextMeshProUGUI title = CreateText("Title", root.transform, font, TitleLabel, 150, TitleColor);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 12;
            Place(title.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(1600, 200));

            // 제목 밑 금색 줄
            RectTransform line = CreateRect("Line", root.transform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f));
            Place(line, new Vector2(0.5f, 0.62f), new Vector2(760, 3));
            line.anchoredPosition = new Vector2(0, -110);
            AddImage(line, null, GoldLine);

            // Press Any Button: 아래쪽 가운데, MainMenu가 깜빡임
            TextMeshProUGUI press = CreateText("PressText", root.transform, font, PressLabel, 40, PressColor);
            Place(press.rectTransform, new Vector2(0.5f, 0.27f), new Vector2(800, 60));

            // 맨 아래 작은 글씨
            TextMeshProUGUI footer = CreateText("Footer", root.transform, font, FooterLabel, 22, FooterColor);
            Place(footer.rectTransform, new Vector2(0.5f, 0.06f), new Vector2(800, 40));

            // 페이드: 맨 위에 그리는 검은 화면. 평소엔 투명 + 클릭 통과
            RectTransform fadeRect = CreateRect("Fade", root.transform, Vector2.zero, Vector2.one);
            Image fadeImage = AddImage(fadeRect, null, Color.black);
            fadeImage.raycastTarget = true;
            var fade = fadeRect.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = 0f;
            fade.blocksRaycasts = false;

            // MainMenu의 private 필드 연결
            var so = new SerializedObject(menu);
            so.FindProperty("pressText").objectReferenceValue = press;
            so.FindProperty("fade").objectReferenceValue = fade;
            so.FindProperty("audioSource").objectReferenceValue = audioSource;
            so.FindProperty("clickSound").objectReferenceValue = clickSound;
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        // 화면 비율 위치(anchor)에 가운데를 맞추고 크기를 정함
        private static void Place(RectTransform rect, Vector2 anchor, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, string text, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
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

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}

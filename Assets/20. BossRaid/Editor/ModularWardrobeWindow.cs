using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BossRaid.Tools
{
    // Synty Modular Fantasy Hero 파츠를 카테고리별 썸네일 격자로 보고, 클릭해서 씬 캐릭터에 입혀 보는 창
    public class ModularWardrobeWindow : EditorWindow
    {
        private enum Gender { Male, Female }

        private class Category
        {
            public readonly string Label;
            public readonly string[] GroupNames;
            public readonly bool AllowNone;
            public readonly bool ViewFromBack;
            public string MirrorLabel;

            public Category(string label, bool allowNone, bool viewFromBack, params string[] groupNames)
            {
                Label = label;
                AllowNone = allowNone;
                ViewFromBack = viewFromBack;
                GroupNames = groupNames;
            }
        }

        private const string PartPrefix = "Chr_";
        private const string ModelRootName = "Modular_Characters";
        private const int ThumbnailPixels = 256;
        private const int ThumbnailsPerFrame = 6;
        private const float CategoryPanelWidth = 170f;

        private static readonly Regex NumberSuffix = new Regex(@"_(\d+)$");

        [SerializeField] private GameObject target;
        [SerializeField] private Gender gender = Gender.Male;
        [SerializeField] private int categoryIndex;
        [SerializeField] private bool mirrorSides = true;
        [SerializeField] private float thumbnailSize = 128f;

        private List<Category> categories;
        private PreviewRenderUtility preview;
        private GameObject previewModel;
        private readonly Dictionary<string, Texture2D> thumbnails = new Dictionary<string, Texture2D>();
        private Vector2 categoryScroll;
        private Vector2 gridScroll;

        [MenuItem("BossRaid/모듈 캐릭터 옷장")]
        private static void Open()
        {
            GetWindow<ModularWardrobeWindow>("모듈 캐릭터 옷장");
        }

        private void OnEnable()
        {
            categories = BuildCategories(gender);
            if (target == null && Selection.activeGameObject != null && FindModelRoot(Selection.activeGameObject) != null)
                target = Selection.activeGameObject;
        }

        private void OnDisable()
        {
            CleanupPreview();
        }

        // ───────────────────────── 카테고리 정의 ─────────────────────────

        private static List<Category> BuildCategories(Gender gender)
        {
            string g = gender.ToString();
            var list = new List<Category>
            {
                new Category("머리", false, false, $"{g}_00_Head"),
                new Category("눈썹", true, false, $"{g}_01_Eyebrows"),
                new Category("수염", true, false, $"{g}_02_FacialHair"),
                new Category("몸통", false, false, $"{g}_03_Torso"),
                new Category("위팔 오른쪽", false, false, $"{g}_04_Arm_Upper_Right") { MirrorLabel = "위팔 왼쪽" },
                new Category("위팔 왼쪽", false, false, $"{g}_05_Arm_Upper_Left") { MirrorLabel = "위팔 오른쪽" },
                new Category("아래팔 오른쪽", false, false, $"{g}_06_Arm_Lower_Right") { MirrorLabel = "아래팔 왼쪽" },
                new Category("아래팔 왼쪽", false, false, $"{g}_07_Arm_Lower_Left") { MirrorLabel = "아래팔 오른쪽" },
                new Category("손 오른쪽", false, false, $"{g}_08_Hand_Right") { MirrorLabel = "손 왼쪽" },
                new Category("손 왼쪽", false, false, $"{g}_09_Hand_Left") { MirrorLabel = "손 오른쪽" },
                new Category("골반", false, false, $"{g}_10_Hips"),
                new Category("다리 오른쪽", false, false, $"{g}_11_Leg_Right") { MirrorLabel = "다리 왼쪽" },
                new Category("다리 왼쪽", false, false, $"{g}_12_Leg_Left") { MirrorLabel = "다리 오른쪽" },
                new Category("머리 장비", true, false, "HeadCoverings_No_Hair", "HeadCoverings_Base_Hair", "HeadCoverings_No_FacialHair"),
                new Category("헤어", true, false, "All_01_Hair"),
                new Category("투구 장식", true, false, "All_02_Head_Attachment"),
                new Category("가슴 장식", true, false, "All_03_Chest_Attachment"),
                new Category("등 장식", true, true, "All_04_Back_Attachment"),
                new Category("어깨 오른쪽", true, false, "All_05_Shoulder_Attachment_Right") { MirrorLabel = "어깨 왼쪽" },
                new Category("어깨 왼쪽", true, false, "All_06_Shoulder_Attachment_Left") { MirrorLabel = "어깨 오른쪽" },
                new Category("팔꿈치 오른쪽", true, false, "All_07_Elbow_Attachment_Right") { MirrorLabel = "팔꿈치 왼쪽" },
                new Category("팔꿈치 왼쪽", true, false, "All_08_Elbow_Attachment_Left") { MirrorLabel = "팔꿈치 오른쪽" },
                new Category("허리 장식", true, false, "All_09_Hips_Attachment"),
                new Category("무릎 오른쪽", true, false, "All_10_Knee_Attachement_Right") { MirrorLabel = "무릎 왼쪽" },
                new Category("무릎 왼쪽", true, false, "All_11_Knee_Attachement_Left") { MirrorLabel = "무릎 오른쪽" },
                new Category("기타", true, false, "All_12_Extra"),
                new Category("엘프 귀", true, false, "Elf_Ear"),
            };
            return list;
        }

        // ───────────────────────── 계층 탐색 ─────────────────────────

        private static Transform FindModelRoot(GameObject go)
        {
            if (go == null) return null;
            return go.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == ModelRootName);
        }

        private static Dictionary<string, Transform> BuildNameMap(GameObject root)
        {
            var map = new Dictionary<string, Transform>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!map.ContainsKey(t.name))
                    map.Add(t.name, t);
            }
            return map;
        }

        private static List<Transform> FindOptions(Dictionary<string, Transform> map, Category category)
        {
            var options = new List<Transform>();
            foreach (string groupName in category.GroupNames)
            {
                if (!map.TryGetValue(groupName, out Transform group)) continue;
                foreach (Transform t in group.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith(PartPrefix) && t.GetComponent<Renderer>() != null)
                        options.Add(t);
                }
            }
            options.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return options;
        }

        // 그룹 오브젝트는 컨테이너일 뿐이라, 켠 파츠가 보이도록 루트까지 부모를 모두 켭니다.
        private static void ActivateAncestors(Transform part, Transform stopAt, bool recordUndo)
        {
            for (Transform t = part.parent; t != null && t != stopAt; t = t.parent)
            {
                if (t.gameObject.activeSelf) continue;
                if (recordUndo) Undo.RecordObject(t.gameObject, "Equip Part");
                t.gameObject.SetActive(true);
                RecordPrefabModification(t.gameObject, recordUndo);
            }
        }

        private static void RecordPrefabModification(GameObject go, bool isSceneObject)
        {
            if (isSceneObject && PrefabUtility.IsPartOfPrefabInstance(go))
                PrefabUtility.RecordPrefabInstancePropertyModifications(go);
        }

        // ───────────────────────── 입히기 ─────────────────────────

        // chosenName이 null이면 이 카테고리를 모두 벗깁니다.
        private static void Equip(GameObject root, Category category, string chosenName, bool isSceneObject)
        {
            Dictionary<string, Transform> map = BuildNameMap(root);
            List<Transform> options = FindOptions(map, category);
            if (options.Count == 0) return;

            if (isSceneObject)
                Undo.RecordObjects(options.Select(o => (Object)o.gameObject).ToArray(), "Equip Part");

            Transform chosen = null;
            foreach (Transform option in options)
            {
                bool on = option.name == chosenName;
                if (on) chosen = option;
                if (option.gameObject.activeSelf == on) continue;
                option.gameObject.SetActive(on);
                RecordPrefabModification(option.gameObject, isSceneObject);
            }

            if (chosen != null)
                ActivateAncestors(chosen, root.transform, isSceneObject);
        }

        private void EquipEverywhere(Category category, string chosenName)
        {
            ApplyEquip(category, chosenName);

            if (!mirrorSides || string.IsNullOrEmpty(category.MirrorLabel)) return;
            Category mirror = categories.FirstOrDefault(c => c.Label == category.MirrorLabel);
            if (mirror == null) return;
            ApplyEquip(mirror, FindMirrorPartName(mirror, chosenName));
        }

        private void ApplyEquip(Category category, string chosenName)
        {
            if (target != null)
            {
                Equip(target, category, chosenName, true);
                EditorSceneManager.MarkSceneDirty(target.scene);
            }
            if (previewModel != null)
                Equip(previewModel, category, chosenName, false);
        }

        // 좌우 짝 파츠는 이름 끝 번호가 같습니다 (예: Chr_ShoulderAttachRight_05 ↔ Chr_ShoulderAttachLeft_05).
        private string FindMirrorPartName(Category mirror, string chosenName)
        {
            if (chosenName == null || previewModel == null) return null;
            Match match = NumberSuffix.Match(chosenName);
            if (!match.Success) return null;
            List<Transform> options = FindOptions(BuildNameMap(previewModel), mirror);
            Transform same = options.FirstOrDefault(o => NumberSuffix.Match(o.name).Groups[1].Value == match.Groups[1].Value);
            return same != null ? same.name : null;
        }

        // 모든 파츠를 벗기고 선택한 성별의 맨몸(카테고리마다 첫 번째 파츠)만 입힙니다.
        private void ApplyBaseOutfit(GameObject root, bool isSceneObject)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            if (isSceneObject)
                Undo.RecordObjects(all.Select(t => (Object)t.gameObject).ToArray(), "Base Outfit");

            foreach (Transform t in all)
            {
                if (!t.name.StartsWith(PartPrefix) || !t.gameObject.activeSelf) continue;
                t.gameObject.SetActive(false);
                RecordPrefabModification(t.gameObject, isSceneObject);
            }

            Dictionary<string, Transform> map = BuildNameMap(root);
            foreach (Category category in categories.Where(c => !c.AllowNone || c.Label == "눈썹"))
            {
                Transform first = FindOptions(map, category).FirstOrDefault();
                if (first == null) continue;
                first.gameObject.SetActive(true);
                RecordPrefabModification(first.gameObject, isSceneObject);
                ActivateAncestors(first, root.transform, isSceneObject);
            }

            if (isSceneObject)
                EditorSceneManager.MarkSceneDirty(root.scene);
        }

        // ───────────────────────── 미리보기 렌더 ─────────────────────────

        private void EnsurePreview()
        {
            if (preview != null && previewModel != null) return;
            CleanupPreview();

            GameObject source = target != null ? target : LoadModelAsset();
            if (source == null) return;

            preview = new PreviewRenderUtility();
            preview.cameraFieldOfView = 30f;
            preview.camera.nearClipPlane = 0.01f;
            preview.camera.farClipPlane = 50f;
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(0.2f, 0.21f, 0.23f);
            preview.lights[0].intensity = 1.3f;
            preview.lights[0].transform.rotation = Quaternion.Euler(35f, 200f, 0f);
            preview.lights[1].intensity = 0.5f;
            preview.lights[1].transform.rotation = Quaternion.Euler(-20f, 20f, 0f);
            preview.ambientColor = new Color(0.35f, 0.35f, 0.38f);

            previewModel = Instantiate(source);
            previewModel.hideFlags = HideFlags.HideAndDontSave;
            previewModel.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            preview.AddSingleGO(previewModel);

            if (target == null)
                ApplyBaseOutfit(previewModel, false);
        }

        private static GameObject LoadModelAsset()
        {
            string guid = AssetDatabase.FindAssets("ModularCharacters t:Model").FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private void CleanupPreview()
        {
            ClearThumbnails();
            if (previewModel != null) DestroyImmediate(previewModel);
            previewModel = null;
            if (preview != null) preview.Cleanup();
            preview = null;
        }

        private void ClearThumbnails()
        {
            foreach (Texture2D texture in thumbnails.Values)
            {
                if (texture != null) DestroyImmediate(texture);
            }
            thumbnails.Clear();
        }

        private static string ThumbnailKey(string partName) => partName;

        // 해당 파츠만 켠 채로 그 부위에 카메라를 맞춰 찍고, 카테고리 상태는 원래대로 되돌립니다.
        private Texture2D RenderThumbnail(Category category, List<Transform> options, Transform part)
        {
            var previous = options.ToDictionary(o => o, o => o.gameObject.activeSelf);
            var previousAncestors = new List<GameObject>();
            for (Transform t = part.parent; t != null && t != previewModel.transform; t = t.parent)
            {
                if (!t.gameObject.activeSelf) previousAncestors.Add(t.gameObject);
            }

            foreach (Transform option in options) option.gameObject.SetActive(option == part);
            foreach (GameObject ancestor in previousAncestors) ancestor.SetActive(true);

            Bounds bounds = part.GetComponent<Renderer>().bounds;
            if (bounds.size.sqrMagnitude < 1e-6f)
                bounds = new Bounds(part.position, Vector3.one * 0.3f);
            float radius = Mathf.Max(bounds.extents.magnitude, 0.12f) * 1.15f;
            float distance = radius / Mathf.Sin(preview.cameraFieldOfView * 0.5f * Mathf.Deg2Rad);
            Vector3 viewDirection = Quaternion.Euler(-12f, category.ViewFromBack ? 205f : 25f, 0f) * Vector3.forward;
            preview.camera.transform.position = bounds.center + viewDirection * distance;
            preview.camera.transform.LookAt(bounds.center);

            preview.BeginStaticPreview(new Rect(0, 0, ThumbnailPixels, ThumbnailPixels));
            preview.Render(true);
            Texture2D texture = preview.EndStaticPreview();

            foreach (var pair in previous) pair.Key.gameObject.SetActive(pair.Value);
            foreach (GameObject ancestor in previousAncestors) ancestor.SetActive(false);
            return texture;
        }

        // 한 번에 다 찍으면 창이 멈추므로 프레임마다 몇 장씩 찍습니다.
        private bool RenderMissingThumbnails(Category category, List<Transform> options)
        {
            int rendered = 0;
            foreach (Transform option in options)
            {
                string key = ThumbnailKey(option.name);
                if (thumbnails.ContainsKey(key)) continue;
                thumbnails[key] = RenderThumbnail(category, options, option);
                if (++rendered >= ThumbnailsPerFrame) return true;
            }
            return false;
        }

        // ───────────────────────── GUI ─────────────────────────

        private void OnGUI()
        {
            DrawToolbar();

            EnsurePreview();
            if (previewModel == null)
            {
                EditorGUILayout.HelpBox("ModularCharacters 모델을 찾지 못했어요. PolygonFantasyHeroCharacters가 임포트되어 있는지 확인해 주세요.", MessageType.Warning);
                return;
            }

            categoryIndex = Mathf.Clamp(categoryIndex, 0, categories.Count - 1);
            Category category = categories[categoryIndex];
            List<Transform> previewOptions = FindOptions(BuildNameMap(previewModel), category);

            if (Event.current.type == EventType.Layout && RenderMissingThumbnails(category, previewOptions))
                Repaint();

            EditorGUILayout.BeginHorizontal();
            DrawCategoryList();
            DrawGrid(category, previewOptions);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUI.BeginChangeCheck();
            GameObject newTarget = (GameObject)EditorGUILayout.ObjectField("입힐 캐릭터 (씬)", target, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                if (newTarget != null && (EditorUtility.IsPersistent(newTarget) || FindModelRoot(newTarget) == null))
                {
                    ShowNotification(new GUIContent("씬에 있는 ModularCharacters를 넣어 주세요"));
                }
                else
                {
                    target = newTarget;
                    CleanupPreview();
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (target == null && GUILayout.Button("씬에 캐릭터 만들기", GUILayout.Width(130)))
                CreateCharacterInScene();

            EditorGUI.BeginChangeCheck();
            gender = (Gender)GUILayout.Toolbar((int)gender, new[] { "남성", "여성" }, GUILayout.Width(120));
            if (EditorGUI.EndChangeCheck())
            {
                categories = BuildCategories(gender);
                ApplyBaseOutfitEverywhere();
            }

            if (GUILayout.Button("기본 복장", GUILayout.Width(80)))
                ApplyBaseOutfitEverywhere();

            mirrorSides = GUILayout.Toggle(mirrorSides, "좌우 같이", GUILayout.Width(75));
            GUILayout.Label("크기", GUILayout.Width(28));
            thumbnailSize = GUILayout.HorizontalSlider(thumbnailSize, 72f, 256f, GUILayout.Width(100));

            if (GUILayout.Button("썸네일 새로고침", GUILayout.Width(110)))
                ClearThumbnails();
            EditorGUILayout.EndHorizontal();
        }

        private void ApplyBaseOutfitEverywhere()
        {
            if (target != null) ApplyBaseOutfit(target, true);
            if (previewModel != null) ApplyBaseOutfit(previewModel, false);
            ClearThumbnails();
        }

        private void CreateCharacterInScene()
        {
            GameObject asset = LoadModelAsset();
            if (asset == null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            Undo.RegisterCreatedObjectUndo(instance, "Create Modular Character");
            Selection.activeGameObject = instance;
            target = instance;
            CleanupPreview();
            ApplyBaseOutfit(target, true);
        }

        private void DrawCategoryList()
        {
            categoryScroll = EditorGUILayout.BeginScrollView(categoryScroll, GUILayout.Width(CategoryPanelWidth));
            Dictionary<string, Transform> map = BuildNameMap(previewModel);
            for (int i = 0; i < categories.Count; i++)
            {
                int count = FindOptions(map, categories[i]).Count;
                if (count == 0) continue;

                var style = new GUIStyle(EditorStyles.toolbarButton) { alignment = TextAnchor.MiddleLeft, fixedHeight = 22 };
                bool selected = i == categoryIndex;
                if (GUILayout.Toggle(selected, $"{categories[i].Label}  ({count})", style) && !selected)
                {
                    categoryIndex = i;
                    gridScroll = Vector2.zero;
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawGrid(Category category, List<Transform> previewOptions)
        {
            string equippedName = GetEquippedName(category);

            float cellWidth = thumbnailSize + 8f;
            float cellHeight = thumbnailSize + 22f;
            float gridWidth = position.width - CategoryPanelWidth - 24f;
            int columns = Mathf.Max(1, Mathf.FloorToInt(gridWidth / cellWidth));

            var cells = new List<string>();
            if (category.AllowNone) cells.Add(null);
            cells.AddRange(previewOptions.Select(o => o.name));

            int rows = Mathf.CeilToInt(cells.Count / (float)columns);
            gridScroll = EditorGUILayout.BeginScrollView(gridScroll);
            Rect area = GUILayoutUtility.GetRect(columns * cellWidth, rows * cellHeight);

            for (int i = 0; i < cells.Count; i++)
            {
                string partName = cells[i];
                var cell = new Rect(area.x + (i % columns) * cellWidth, area.y + (i / columns) * cellHeight, thumbnailSize, thumbnailSize);
                bool equipped = partName == equippedName;

                if (equipped)
                    EditorGUI.DrawRect(new Rect(cell.x - 3, cell.y - 3, cell.width + 6, cell.height + 6), new Color(0.3f, 0.8f, 0.4f));

                if (partName == null)
                {
                    EditorGUI.DrawRect(cell, new Color(0.15f, 0.15f, 0.16f));
                    GUI.Label(cell, "없음", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter });
                }
                else if (thumbnails.TryGetValue(ThumbnailKey(partName), out Texture2D texture) && texture != null)
                {
                    GUI.DrawTexture(cell, texture, ScaleMode.ScaleToFit);
                }
                else
                {
                    EditorGUI.DrawRect(cell, new Color(0.15f, 0.15f, 0.16f));
                    GUI.Label(cell, "...", new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter });
                }

                var labelRect = new Rect(cell.x, cell.yMax + 2, cell.width, 16);
                GUI.Label(labelRect, partName == null ? "" : partName.Substring(PartPrefix.Length), EditorStyles.miniLabel);

                if (GUI.Button(cell, GUIContent.none, GUIStyle.none))
                {
                    EquipEverywhere(category, partName);
                    Repaint();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private string GetEquippedName(Category category)
        {
            GameObject source = target != null ? target : previewModel;
            Transform equipped = FindOptions(BuildNameMap(source), category).FirstOrDefault(o => o.gameObject.activeSelf);
            return equipped != null ? equipped.name : null;
        }
    }
}

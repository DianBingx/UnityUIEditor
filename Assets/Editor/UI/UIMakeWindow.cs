using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UI.ListView;

namespace Sean.Editor
{
    public class UIMakeWindow : EditorWindow
    {
        private const float kWidth = 160f;
        private const float kThickHeight = 50f;
        private const float kThinHeight = 40f;
        private const string kStandardSpritePath = "UI/Skin/UISprite.psd";
        private const string kBackgroundSpriteResourcePath = "UI/Skin/Background.psd";
        private const string kInputFieldBackgroundPath = "UI/Skin/InputFieldBackground.psd";
        private const string kKnobPath = "UI/Skin/Knob.psd";
        private const string kCheckmarkPath = "UI/Skin/Checkmark.psd";
        private const string kDropdownArrow = "UI/Skin/DropdownArrow.psd";
        private const string kUIMask = "UI/Skin/UIMask.psd";

        private static Vector2 s_ThinGUIElementSize = new Vector2(kWidth, kThinHeight);
        private static Color s_DefaultSelectableColor = new Color(1f, 1f, 1f, 1f);
        private static Color s_TextColor = new Color(50f / 255f, 50f / 255f, 50f / 255f, 1f);
        private static int s_TextFontSize = 16;

        private static DefaultControls.Resources _res;
        private Vector2 scrollPos;

        private static DefaultControls.Resources Res
        {
            get
            {
                if (_res.background) return _res;
                var res = new DefaultControls.Resources();
                res.background = AssetDatabase.GetBuiltinExtraResource<Sprite>(kBackgroundSpriteResourcePath);
                res.checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>(kCheckmarkPath);
                res.dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>(kDropdownArrow);
                res.inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>(kInputFieldBackgroundPath);
                res.knob = AssetDatabase.GetBuiltinExtraResource<Sprite>(kKnobPath);
                res.mask = AssetDatabase.GetBuiltinExtraResource<Sprite>(kUIMask);
                res.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
                _res = res;
                return _res;
            }
        }

        #region Label
        static public void AddButtonWithText()
        {
            var go = DefaultControls.CreateButton(Res);
            ToScene(go);
        }

        static public void AddImageButton()
        {
            var go = DefaultControls.CreateButton(Res);
            ToScene(go);
            var text = go.transform.GetComponentInChildren<Text>();
            DestroyImmediate(text.gameObject);
        }
        #endregion

        #region Toggle        

        static public void AddTab()
        {
            GameObject tabRoot = new GameObject("Tab", typeof(ToggleGroup));
            ToScene(tabRoot);
            var group = tabRoot.GetComponent<ToggleGroup>();

            for (int i = 1; i < 3; i++)
            {
                var toggleRoot = CreateUIObject("Tab" + i, tabRoot);

                var tab = toggleRoot.transform as RectTransform;
                tab.sizeDelta = new Vector2(s_ThinGUIElementSize.x / 2 - 10, s_ThinGUIElementSize.y);
                tab.localPosition = new Vector2((i < 2 ? -1 : 1) * (s_ThinGUIElementSize.x - tab.sizeDelta.x) / 2, 0);

                GameObject background = CreateUIObject("Background", toggleRoot);
                GameObject checkmark = CreateUIObject("Checkmark", background);
                Toggle toggle = toggleRoot.AddComponent<Toggle>();
                toggle.isOn = i == 1 ? true : false;

                Image bgImage = background.AddComponent<Image>();
                bgImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
                bgImage.type = Image.Type.Sliced;
                bgImage.color = s_DefaultSelectableColor;
                RectTransform bgRect = background.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.anchoredPosition = Vector2.one / 2;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
                checkmarkRect.anchorMin = Vector2.zero;
                checkmarkRect.anchorMax = Vector2.one;
                checkmarkRect.anchoredPosition = Vector2.one / 2;
                checkmarkRect.offsetMin = Vector2.zero;
                checkmarkRect.offsetMax = Vector2.zero;

                Image checkmarkImage = checkmark.AddComponent<Image>();
                checkmarkImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(kCheckmarkPath);
                toggle.graphic = checkmarkImage;
                toggle.targetGraphic = bgImage;
                toggle.group = group;
            }

        }
        #endregion

        #region ListView
        private void AddTableView(TableView.Direction dir)
        {
            var view = DefaultControls.CreateScrollView(Res);
            ToScene(view);
            var tableView = view.AddComponent<TableView>();
            tableView.mDirection = dir;
            view.name = "TableView";
            var rect = view.GetComponent<ScrollRect>();
            HorizontalOrVerticalLayoutGroup m_LayoutGroup;
            switch (dir)
            {
                case TableView.Direction.Horizontal:
                    rect.horizontal = true;
                    rect.vertical = false;
                    DestroyImmediate(view.transform.Find("Scrollbar Vertical").gameObject);
                    m_LayoutGroup = rect.content.gameObject.AddComponent<HorizontalLayoutGroup>();
                    m_LayoutGroup.childForceExpandHeight = true;
                    m_LayoutGroup.childForceExpandWidth = false;
                    var r_h = (m_LayoutGroup.transform as RectTransform);
                    r_h.anchorMin = Vector2.zero;
                    r_h.anchorMax = Vector2.up;
                    r_h.pivot = Vector2.up / 2;
                    rect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
                    break;
                case TableView.Direction.Vertical:
                    rect.horizontal = false;
                    rect.vertical = true;
                    DestroyImmediate(view.transform.Find("Scrollbar Horizontal").gameObject);
                    m_LayoutGroup = rect.content.gameObject.AddComponent<VerticalLayoutGroup>();
                    m_LayoutGroup.childForceExpandHeight = false;
                    m_LayoutGroup.childForceExpandWidth = true;
                    var r_v = (m_LayoutGroup.transform as RectTransform);
                    r_v.anchorMin = Vector2.up;
                    r_v.anchorMax = Vector2.one;
                    r_v.pivot = new Vector2(0.5f, 1);
                    rect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
                    break;
                default:
                    break;
            }

        }
        #endregion

        #region Tool

        static GameObject CreateUIObject(string name, GameObject parent)
        {
            GameObject go = new GameObject(name);
            go.AddComponent<RectTransform>();
            GameObjectUtility.SetParentAndAlign(go, parent);
            return go;
        }
        private static void SetDefaultTextValues(Text lbl)
        {
            lbl.color = s_TextColor;
            lbl.fontSize = s_TextFontSize;
        }
        private static void CreateEventSystem(bool select)
        {
            CreateEventSystem(select, null);
        }

        private static void CreateEventSystem(bool select, GameObject parent)
        {
            var esys = Object.FindObjectOfType<EventSystem>();
            if (esys == null)
            {
                var eventSystem = new GameObject("EventSystem");
                GameObjectUtility.SetParentAndAlign(eventSystem, parent);
                esys = eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();

                Undo.RegisterCreatedObjectUndo(eventSystem, "Create " + eventSystem.name);
            }

            if (select && esys != null)
            {
                Selection.activeGameObject = esys.gameObject;
            }
        }
        static public GameObject CreateNewUI()
        {
            // Root for the UI
            var root = new GameObject("Canvas");
            root.layer = LayerMask.NameToLayer("UI");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(root, "Create " + root.name);

            // if there is no event system add one...
            CreateEventSystem(false);
            return root;
        }
        // Helper function that returns a Canvas GameObject; preferably a parent of the selection, or other existing Canvas.
        static public GameObject GetOrCreateCanvasGameObject()
        {
            GameObject selectedGo = Selection.activeGameObject;

            // Try to find a gameobject that is the selected GO or one if its parents.
            Canvas canvas = (selectedGo != null) ? selectedGo.GetComponentInParent<Canvas>() : null;
            if (canvas != null && canvas.gameObject.activeInHierarchy)
                return canvas.gameObject;

            // No canvas in selection or its parents? Then use just any canvas..
            canvas = Object.FindObjectOfType(typeof(Canvas)) as Canvas;
            if (canvas != null && canvas.gameObject.activeInHierarchy)
                return canvas.gameObject;

            // No canvas in the scene at all? Then create a new one.
            return CreateNewUI();
        }
        private static void SetPositionVisibleinSceneView(RectTransform canvasRTransform, RectTransform itemTransform)
        {
            // Find the best scene view
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null && SceneView.sceneViews.Count > 0)
                sceneView = SceneView.sceneViews[0] as SceneView;

            // Couldn't find a SceneView. Don't set position.
            if (sceneView == null || sceneView.camera == null)
                return;

            // Create world space Plane from canvas position.
            Vector2 localPlanePosition;
            Camera camera = sceneView.camera;
            Vector3 position = Vector3.zero;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRTransform, new Vector2(camera.pixelWidth / 2, camera.pixelHeight / 2), camera, out localPlanePosition))
            {
                // Adjust for canvas pivot
                localPlanePosition.x = localPlanePosition.x + canvasRTransform.sizeDelta.x * canvasRTransform.pivot.x;
                localPlanePosition.y = localPlanePosition.y + canvasRTransform.sizeDelta.y * canvasRTransform.pivot.y;

                localPlanePosition.x = Mathf.Clamp(localPlanePosition.x, 0, canvasRTransform.sizeDelta.x);
                localPlanePosition.y = Mathf.Clamp(localPlanePosition.y, 0, canvasRTransform.sizeDelta.y);

                // Adjust for anchoring
                position.x = localPlanePosition.x - canvasRTransform.sizeDelta.x * itemTransform.anchorMin.x;
                position.y = localPlanePosition.y - canvasRTransform.sizeDelta.y * itemTransform.anchorMin.y;

                Vector3 minLocalPosition;
                minLocalPosition.x = canvasRTransform.sizeDelta.x * (0 - canvasRTransform.pivot.x) + itemTransform.sizeDelta.x * itemTransform.pivot.x;
                minLocalPosition.y = canvasRTransform.sizeDelta.y * (0 - canvasRTransform.pivot.y) + itemTransform.sizeDelta.y * itemTransform.pivot.y;

                Vector3 maxLocalPosition;
                maxLocalPosition.x = canvasRTransform.sizeDelta.x * (1 - canvasRTransform.pivot.x) - itemTransform.sizeDelta.x * itemTransform.pivot.x;
                maxLocalPosition.y = canvasRTransform.sizeDelta.y * (1 - canvasRTransform.pivot.y) - itemTransform.sizeDelta.y * itemTransform.pivot.y;

                position.x = Mathf.Clamp(position.x, minLocalPosition.x, maxLocalPosition.x);
                position.y = Mathf.Clamp(position.y, minLocalPosition.y, maxLocalPosition.y);
            }

            itemTransform.anchoredPosition = position;
            itemTransform.localRotation = Quaternion.identity;
            itemTransform.localScale = Vector3.one;
        }
        public static void SetDefaultColorTransitionValues(Selectable slider)
        {
            ColorBlock colors = slider.colors;
            colors.highlightedColor = new Color(0.882f, 0.882f, 0.882f);
            colors.pressedColor = new Color(0.698f, 0.698f, 0.698f);
            colors.disabledColor = new Color(0.521f, 0.521f, 0.521f);
        }

        public static void ToScene(GameObject child)
        {
            GameObject parent = Selection.activeGameObject;
            if (parent == null || parent.GetComponentInParent<Canvas>() == null)
            {
                parent = GetOrCreateCanvasGameObject();
            }
            GameObjectUtility.SetParentAndAlign(child, parent);
            Selection.activeGameObject = child;
        }
        public static GameObject CreateUIElementRoot(string name, MenuCommand menuCommand, Vector2 size)
        {
            GameObject parent = menuCommand.context as GameObject;
            if (parent == null || parent.GetComponentInParent<Canvas>() == null)
            {
                parent = GetOrCreateCanvasGameObject();
            }
            GameObject child = new GameObject(name);

            Undo.RegisterCreatedObjectUndo(child, "Create " + name);
            Undo.SetTransformParent(child.transform, parent.transform, "Parent " + child.name);
            GameObjectUtility.SetParentAndAlign(child, parent);

            RectTransform rectTransform = child.AddComponent<RectTransform>();
            rectTransform.sizeDelta = size;
            if (parent != menuCommand.context) // not a context click, so center in sceneview
            {
                SetPositionVisibleinSceneView(parent.GetComponent<RectTransform>(), rectTransform);
            }
            Selection.activeGameObject = child;
            return child;
        }
        #endregion Tools
        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            if (GUILayout.Button("小图Image"))
            {
                var go = DefaultControls.CreateImage(Res);
                ToScene(go);
            }
            if (GUILayout.Button("大图RawImage"))
            {
                var go = DefaultControls.CreateRawImage(Res);
                ToScene(go);
            }

            GUILayout.Space(5);
            GUILayout.Space(5);

            if (GUILayout.Button("文字按钮"))
            {
                AddButtonWithText();
            }
            GUILayout.Space(5);

            if (GUILayout.Button("图片按钮"))
            {
                AddImageButton();
            }
            GUILayout.Space(5);
            GUILayout.Space(5);

            if (GUILayout.Button("进度条"))
            {
                var go = DefaultControls.CreateSlider(Res);
                ToScene(go);
            }
            GUILayout.Space(5);

            if (GUILayout.Button("输入框"))
            {
                var go = DefaultControls.CreateInputField(Res);
                ToScene(go);
            }
            GUILayout.Space(5);
            if (GUILayout.Button("单选框"))
            {
                var go = DefaultControls.CreateToggle(Res);
                ToScene(go);
            }
            GUILayout.Space(5);

            if (GUILayout.Button("Tab页"))
            {
                AddTab();
            }
            GUILayout.Space(5);
            GUILayout.Space(5);

            if (GUILayout.Button("横向滚动列表"))
            {
                AddTableView(TableView.Direction.Horizontal);
            }

            if (GUILayout.Button("竖向滚动列表"))
            {
                AddTableView(TableView.Direction.Vertical);
            }
            EditorGUILayout.EndScrollView();

        }


    }
}
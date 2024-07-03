using lilToon;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Resources.Components
{
    public class Preview
    {
        private VisualTreeAsset _visualTree;
        private float _previewScale = 1.0f;
        private Vector2 _previewOffset = Vector2.zero;
        private float _previewWidth;
        private float _previewHeight;
        private VisualElement _root;
        private IMGUIContainer _iMGUIContainer;
        private UnityEditor.Editor _gameObjectEditor;
        private GameObject _prefab;
        private string _prefabPath;
        private bool _isDragging;

        public Preview(VisualElement rootElement, string prefabPath)
        {
            _root = rootElement;
            _prefabPath = prefabPath;
            _visualTree = UnityEngine.Resources.Load<VisualTreeAsset>("Components/Preview");
        }
        public void ShowContent()
        {
            if (!TryLoadPrefab(_prefabPath, out _prefab))
            {
                _root.Add(new Label("Select a prefab to preview"));
                return;
            }

            Debug.Log("Prefab loaded");
            // CreateResetButton();
            RefreshPreview();
        }

        public void RefreshPreview(string prefabPath = "")
        {
            ResetPreview();

            if (!string.IsNullOrEmpty(prefabPath))
            {
                _prefabPath = prefabPath;
            }

            _root.Clear();

            if (!TryLoadPrefab(_prefabPath, out _prefab))
            {
                _root.Add(new Label("Select a prefab to preview"));
                return;
            }

            // canvas visual element 생성
            var preview = new VisualElement();
            preview.AddToClassList("flex-1");
            _root.Add(preview);

            var previewVisualElement = new VisualElement();
            preview.Add(previewVisualElement);

            preview.RegisterCallback<GeometryChangedEvent>(OnPreviewGeometryChanged);

            void OnPreviewGeometryChanged(GeometryChangedEvent evt)
            {
                preview.UnregisterCallback<GeometryChangedEvent>(OnPreviewGeometryChanged);
                _previewWidth = evt.newRect.width;
                _previewHeight = evt.newRect.height;
                _iMGUIContainer = new IMGUIContainer(() =>
                {
                    var previewRect = GUILayoutUtility.GetRect(_previewWidth, _previewHeight);
                    var previewRectArea = new Rect(_previewOffset.x, _previewOffset.y, _previewWidth * _previewScale, _previewHeight * _previewScale);
                    var eventCurrent = Event.current;

                    GUILayout.BeginArea(previewRectArea);
                    _gameObjectEditor ??= UnityEditor.Editor.CreateEditor(_prefab);

                    _gameObjectEditor.OnInteractivePreviewGUI(previewRect, new GUIStyle() { normal = { background = EditorGUIUtility.whiteTexture } });
                    GUILayout.EndArea();
                });

                _iMGUIContainer.style.width = _previewWidth;
                _iMGUIContainer.style.height = _previewHeight;

                previewVisualElement.Add(_iMGUIContainer);
            }
        }

        public void ResetPreview()
        {
            _previewScale = 1.0f;
            _previewOffset = Vector2.zero;
            _iMGUIContainer = null;
            _gameObjectEditor = null;
        }

        private static bool TryLoadPrefab(string prefabPath, out GameObject prefab)
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            return prefab != null;
        }
    }
}

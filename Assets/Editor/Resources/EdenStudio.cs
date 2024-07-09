using System;
using Editor.Resources.Screens.Export;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Resources
{
    public class EdenStudio : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;
        private VisualElement container;

        public static StyleSheet style;
        public static StyleSheet tailwind;

        [MenuItem("Window/EdenStudio")]
        public static void ShowExample()
        {
            var wnd = GetWindow<EdenStudio>();
            wnd.titleContent = new GUIContent("EdenStudio");
            wnd.minSize = new Vector2(1080, 640);
        }

        public void CreateGUI()
        {
            style = UnityEngine.Resources.Load<StyleSheet>("style");
            tailwind = UnityEngine.Resources.Load<StyleSheet>("tailwind");

            var root = rootVisualElement;
            root.styleSheets.Add(style);
            root.styleSheets.Add(tailwind);


            // Instantiate UXML
            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            root.Add(labelFromUXML);
            container = labelFromUXML.Q("container");
            Export.Show(container, OnExportVrmClicked);
        }

        private void OnExportVrmClicked()
        {
            ExportVrm.Show(container, OnBackClicked);
        }

        private void OnBackClicked()
        {
            Export.Show(container, OnExportVrmClicked);
        }
    }
}

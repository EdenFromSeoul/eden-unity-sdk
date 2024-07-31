using System;
using Editor.Resources.Screens.Export;
using Editor.Scripts.Manager;
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
        private static Label prefab_list_label;
        private static Label settings_label;
        private static EnumField languageDropdown;

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
            prefab_list_label = labelFromUXML.Q<Label>("prefab_list_label");
            settings_label = labelFromUXML.Q<Label>("settings_label");
            languageDropdown = labelFromUXML.Q<EnumField>("language_field");
            
            LocalizationManager.OnLanguageChanged += () =>
            {
                prefab_list_label.text = LocalizationManager.GetLocalizedValue("prefab_list");
                settings_label.text = LocalizationManager.GetLocalizedValue("settings");
                languageDropdown.label = LocalizationManager.GetLocalizedValue("language");
            };
            
            languageDropdown.RegisterValueChangedCallback(OnLanguageChanged);
            prefab_list_label.text = LocalizationManager.GetLocalizedValue("prefab_list");
            settings_label.text = LocalizationManager.GetLocalizedValue("settings");
            languageDropdown.label = LocalizationManager.GetLocalizedValue("language");
            Export.Show(container, OnExportVrmClicked);
        }

        private void OnLanguageChanged(ChangeEvent<Enum> evt)
        {
            Debug.Log($"Language changed to {evt.newValue}");
            var language = (Language)evt.newValue;
            LocalizationManager.SetLanguage(language);
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Editor.Resources.Components;
using Editor.Scripts;
using Editor.Scripts.Manager;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ScrollView = UnityEngine.UIElements.ScrollView;

namespace Editor.Resources.Screens.Export
{
    public class Export : EditorWindow
    {
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset;
        private static VisualElement _container;
        private static ScrollView _scrollView;
        private static List<ItemInfo> _items;
        private static Button _importButton;
        private static VisualElement _exportOptions;
        private static Button _exportButton;
        private static Button _exportUpkButton;
        private static Button _exportVrmButton;
        private static Preview _preview;
        private static Label _title;
        private static Label _subtitle;

        public static void Show(VisualElement root, Action onExportVrmClicked)
        {
            _container = root;
            _container.Clear();

            var visualTree = UnityEngine.Resources.Load<VisualTreeAsset>("Screens/Export/Export");
            visualTree.CloneTree(_container);
            _scrollView = _container.Q<ScrollView>("scroll-view");
            _importButton = _container.Q<Button>("import-button");
            _importButton.clicked += ImportItem;
            _exportOptions = _container.Q("exportOptions");
            _exportButton = _container.Q<Button>("exportButton");
            _exportUpkButton = _container.Q<Button>("exportUpkButton");
            _exportVrmButton = _container.Q<Button>("exportVrmButton");
            _title = _container.Q<Label>("title");
            _subtitle = _container.Q<Label>("subtitle");
            
            _exportUpkButton.clicked += ExportUnityPackage;
            _exportVrmButton.clicked += onExportVrmClicked;
            _preview = new Preview(_container.Q("preview"), EdenStudioInitializer.SelectedItem?.path ?? "");
            _preview.ShowContent();
            _exportOptions.style.display = DisplayStyle.None;

            _exportButton.clicked += () =>
            {
                _exportOptions.style.display = (_exportOptions.style.display == DisplayStyle.None)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            };

            EdenStudioInitializer.OnSelectedItemChanged += OnSelectedItemChanged;
            LocalizationManager.OnLanguageChanged += RefreshLocalization;

            RefreshLocalization();
            if (EdenStudioInitializer.SelectedItem != null)
            {
                _preview.RefreshPreview(EdenStudioInitializer.SelectedItem.path);
            }

            LoadItems();
        }
        
        private static void RefreshLocalization()
        {
            _importButton.text = LocalizationManager.GetLocalizedValue("import_prefab");
            _exportButton.text = LocalizationManager.GetLocalizedValue("export");
            _exportUpkButton.text = LocalizationManager.GetLocalizedValue("save_as_unitypackage");
            _exportVrmButton.text = LocalizationManager.GetLocalizedValue("save_as_vrm");
            _title.text = LocalizationManager.GetLocalizedValue("prefab_list");
            _subtitle.text = LocalizationManager.GetLocalizedValue("manage_prefabs_from_unitypackage");
        }
        
        private static void ExportUnityPackage()
        {
            var selectedItem = EdenStudioInitializer.SelectedItem;
            var path = EditorUtility.SaveFilePanel("Save UnityPackage", "", "New UnityPackage", "unitypackage");
            if (string.IsNullOrEmpty(path)) return;
            AssetDatabase.ExportPackage(selectedItem.path, path, ExportPackageOptions.IncludeDependencies);
        }

        private static void OnSelectedItemChanged(ItemInfo item)
        {
            _preview.RefreshPreview(item.path);
        }

        private static void ImportItem()
        {
            var path = EditorUtility.OpenFilePanel("Import Item", "", "unitypackage");
            if (string.IsNullOrEmpty(path)) return;
            var extension = Path.GetExtension(path);
            if (extension == ".unitypackage")
            {
                AssetDatabase.onImportPackageItemsCompleted += OnImportItemsCompleted;
                AssetDatabase.ImportPackage(path, false);
            }
            // else if (extension == ".prefab")
            // {
            //     var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            //     var item = ItemManager.CreateItem(prefab);
            //     _scrollView.Add(new ItemButton(item, () => { EdenStudioInitializer.SelectedItem = item; }));
            // }
        }

        private static void OnImportItemsCompleted(string[] importedAssets)
        {
            AssetDatabase.onImportPackageItemsCompleted -= OnImportItemsCompleted;
            LoadItems();
        }

        private static async void LoadItems()
        {
            await Task.Yield();
            GetItems();
        }

        private static async void GetItems()
        {
            _scrollView.Clear();
            _items = ItemManager.GetAllPrefabsAsItems(true);

            if (_items.Count == 0)
            {
                var button = new Button(() => ImportItem());
                button.text = LocalizationManager.GetLocalizedValue("import_prefab");
                
                var label = new Label(LocalizationManager.GetLocalizedValue("add_prefabs_and_use_features"));
                _scrollView.Add(label);
                _scrollView.Add(button);

                return;
            }

            foreach (var itemElement in _items.Select(item =>
                         new ItemButton(item, () => { EdenStudioInitializer.SelectedItem = item; })))
            {
                _scrollView.Add(itemElement);
                await Task.Yield();
            }
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Instantiate UXML
            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            root.Add(labelFromUXML);
        }
    }
}
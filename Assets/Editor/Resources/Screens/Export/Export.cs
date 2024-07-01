using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Editor.Resources.Components;
using Editor.Scripts;
using Editor.Scripts.Manager;
using Runtime;
using UniGLTF;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UniVRM10;
using VrmLib;
using VRMShaders;
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
        private static Button _exportButton;
        private static Preview _preview;

        public static void Show(VisualElement root)
        {
            _container = root;
            var visualTree = UnityEngine.Resources.Load<VisualTreeAsset>("Screens/Export/Export");
            visualTree.CloneTree(_container);
            _scrollView = _container.Q<ScrollView>("scroll-view");
            _importButton = _container.Q<Button>("import-button");
            _importButton.clicked += ImportItem;
            _exportButton = _container.Q<Button>("export-button");
            _exportButton.clicked += ExportItem;
            _preview = new Preview(_container.Q("preview"), EdenStudioInitializer.SelectedItem?.path ?? "");
            _preview.ShowContent();
            EdenStudioInitializer.OnSelectedItemChanged += OnSelectedItemChanged;

            if (EdenStudioInitializer.SelectedItem != null)
            {
                _preview.RefreshPreview(EdenStudioInitializer.SelectedItem.path);
            }
            LoadItems();
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
        
        private static void ExportItem()
        {
            // Export the selected item
            var prefabPath = EdenStudioInitializer.SelectedItem.path;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var savePath = "Assets/Eden/" + EdenStudioInitializer.SelectedItem.modelName + ".vrm";

            Debug.Log("Exporting " + prefab.name + " to VRM format...");

            // (VRM)을 붙인 이름으로 프리팹 복제
            var prefabClone = Object.Instantiate(prefab);
            prefabClone.name = prefabClone.name + "(VRM)";

            // 저장
            PrefabUtility.SaveAsPrefabAsset(prefabClone, "Assets/Eden/" + prefabClone.name + ".prefab");

            // 이 프리팹을 사용
            prefab = prefabClone;

            using (var arrayManager = new NativeArrayManager())
            {
                var converter = new ModelExporter();
                var model = converter.Export(arrayManager, prefab);

                model.ConvertCoordinate(Coordinates.Vrm1);
                foreach (var VARIABLE  in model.Materials)
                {
                    var material = VARIABLE as Material;
                    Debug.Log(material);
                    if (material == null) continue;
                    Debug.Log(material.shader);
                    material.shader = Shader.Find("VRM10/MToon10");

                    // set shade texture to same as main texture
                    if (material.HasProperty("_ShadeTex"))
                    {
                        material.SetTexture("_ShadeTex", material.GetTexture("_MainTex"));
                    }

                    // _BumpMap 은 _MainTex 가 null 이 아닐 때만 적용, material name + "_normal" 을 찾아서 적용
                    if (material.HasProperty("_BumpMap") && material.GetTexture("_MainTex") != null)
                    {
                        Debug.Log(material.name);
                        // 해당 이름을 가진 texture guid, texture2D, 확장자 png
                        var guids = AssetDatabase.FindAssets(material.name + "_normal t:texture2D glob:\"*.png\"");

                        if (guids.Length != 0)
                        {
                            var textureGuid = guids.FirstOrDefault();
                            // 해당 guid 를 가진 texture path
                            var texturePath = AssetDatabase.GUIDToAssetPath(textureGuid);
                            // 해당 texture path 에 있는 texture 를 가져옴
                            Debug.Log(texturePath);
                            var normalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

                            if (normalTexture != null)
                            {
                                material.SetTexture("_BumpMap", normalTexture);
                            }
                        }
                    }
                    Debug.Log(material.shader);
                }

                var gltfExportSettings = new GltfExportSettings
                {
                    UseSparseAccessorForMorphTarget = true,
                    ExportOnlyBlendShapePosition = true,
                    DivideVertexBuffer = true
                };
                var exporter = new Vrm10Exporter(new RuntimeTextureSerializer(), gltfExportSettings);
                var option = new ExportArgs();
                var selectedItem = EdenStudioInitializer.SelectedItem;
                var vrmMeta = new VRM10ObjectMeta
                {
                    Name = selectedItem.name,
                    Version = "1.0",
                    Authors = new List<string> { "Eden Studio" },
                };
                exporter.Export(prefab, model, converter, option, vrmMeta);
                var bytes = exporter.Storage.ToGlbBytes();
                File.WriteAllBytes(savePath, bytes);
            }
            
            // var bytes = Vrm10Exporter.Export(prefab, vrmMeta: new VRM10ObjectMeta() { Name = EdenStudioInitializer.SelectedItem.name });
                
            // System.IO.File.WriteAllBytes($"{savePath}.vrm", bytes);
            EditorUtility.DisplayDialog("Exported", "The item has been exported to VRM format.", "OK");
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
                _scrollView.Add(new Label("No items found."));
                return;
            }

            foreach (var itemElement in _items.Select(item => new ItemButton(item, () => { EdenStudioInitializer.SelectedItem = item; })))
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
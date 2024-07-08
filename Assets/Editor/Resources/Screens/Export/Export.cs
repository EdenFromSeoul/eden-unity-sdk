using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Editor.Resources.Components;
using Editor.Scripts;
using Editor.Scripts.Manager;
using lilToon;
using Runtime;
using UniGLTF;
using UniGLTF.MeshUtility;
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
        class TempDisposable : IDisposable
        {
            private List<UnityEngine.Object> _disposables = new();
            
            public void Add(UnityEngine.Object obj)
            {
                _disposables.Add(obj);
            }
            
            public void Dispose()
            {
                foreach (var obj in _disposables)
                {
                    DestroyImmediate(obj);
                }
                _disposables.Clear();
            }
        }
        
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
        
        private static void ChangeMaterials(GameObject prefab, string savePath)
        {
            //1. prefab 하위 오브젝트들을 활성화시킨다.
            List<GameObject> list = new List<GameObject>();
            Stack<Transform> stack = new Stack<Transform>();
            stack.Push(prefab.transform);
            
            while (stack.Count > 0)
            {
                Transform current = stack.Pop();

                foreach (Transform child in current)
                {
                    if (!child.gameObject.activeSelf)
                    {
                        child.gameObject.SetActive(true);
                        list.Add(child.gameObject);
                    }

                    stack.Push(child);
                }
            }
            var setActiveFalseList = list.ToArray();
            
            
            //2.prefab의 skinmeshrenderer을 모아 이중 for each문으로 각각의 material을 가져온다.
            var skinnedMeshRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            List<Material> referenceMaterials = new List<Material>();

            foreach (var skinnedMesh in skinnedMeshRenderers)
            {
                foreach (var sMaterial in skinnedMesh.sharedMaterials)
                {
                    //3. lilToon인지 확인하고, 맞다면 referenceMaterial 라스트애 추가한다.
                    if (sMaterial != null && sMaterial.shader.name.Contains("lilToon"))
                    {
                        if (!referenceMaterials.Contains(sMaterial))
                        {
                            referenceMaterials.Add(sMaterial);
                        }
                    }
                }
            }
            
            
            //4.referneceMaterials를 순환하면 Mtoon으로 재질 변환한다. 변환한 material들은 Dictionary 형식으로 저장한다.
            Dictionary<Material, Material> convertedMaterials = new Dictionary<Material, Material>();
            foreach (var material in referenceMaterials)
            {
                var path = Path.Combine(savePath,
                    material.name + ".mat");
                try
                {
                    lilMaterialBaker.CreateMToonMaterial(material, path);
                }
                catch (Exception e)
                {
                    //Debug.Log(material.name + "Exception : " + e);
                }

                Material m = AssetDatabase.LoadAssetAtPath(path, typeof(Material)) as Material;
                if (!m)
                {
                    Debug.Log("no Materials : " + path);
                }
                convertedMaterials.Add(material, m);
            }
            
            
            //다시 skinMesh단위로 순환하며 Dictionary의 meterial들을 map에서 찾아 교체한다.
            foreach (var skinnedMesh in skinnedMeshRenderers)
            {
                var materials = skinnedMesh.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && materials[i].shader.name.Contains("lilToon"))
                    {
                        if (convertedMaterials.ContainsKey(materials[i]))
                        {
                            materials[i] = convertedMaterials[materials[i]];
                        }
                    }
                }

                skinnedMesh.sharedMaterials = materials;
            }
            
            //오브잭트들을 다시 비활성화한다.
            setActiveFalseList.ToList().ForEach(obj => obj.SetActive(false));
            
        }
        
        private static void ExportItem()
        {
            // Export the selected item
            var prefabPath = EdenStudioInitializer.SelectedItem.path;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var savePath = "Assets/Eden/" + EdenStudioInitializer.SelectedItem.modelName + ".vrm";

            Debug.Log("Exporting " + prefab.name + " to VRM format...");

            // (VRM)을 붙인 이름으로 프리팹 복제
            var prefabClone = Instantiate(prefab);
            prefabClone.name += "(VRM)";

            // 저장
            PrefabUtility.SaveAsPrefabAsset(prefabClone, "Assets/Eden/" + prefabClone.name + ".prefab");

            // 이 프리팹을 사용
            prefab = prefabClone;
            
            //prefab의 material들의 셰이더를 Mtoon으로 변환
            ChangeMaterials(prefab, "Assets/Eden/");

            using (var tempDisposable = new TempDisposable())
            using (var arrayManager = new NativeArrayManager())
            {
                var selectedItem = EdenStudioInitializer.SelectedItem;
                var vrmMeta = new VRM10ObjectMeta
                {
                    Name = selectedItem.name,
                    Version = "1.0",
                    Authors = new List<string> { "Eden Studio" },
                };
                
                var copy = Instantiate(prefab);
                tempDisposable.Add(copy);
                prefab = copy;
                
                ConvertManager.Convert(
                    savePath,
                    prefab,
                    vrmMeta
                    );

                // freeze mesh
                // 왠지 몰라도 노멀라이즈하면 모델이 깨짐. 그래서 일단 주석처리. TODO: 노멀라이즈 수정
                // var newMeshMap = BoneNormalizer.NormalizeHierarchyFreezeMesh(prefab);
                // BoneNormalizer.Replace(prefab, newMeshMap, true, true);
                var converter = new ModelExporter();
                var model = converter.Export(arrayManager, prefab);
               
                model.ConvertCoordinate(Coordinates.Vrm1);
                
                //셰이더를 Mtoon에서 Mtoon10으로 변환 및 _ShadTexture 속성을 임시 저장 및 _ShadeTex 속성으로 복구
                foreach (var VARIABLE  in model.Materials)
                {
                    var material = VARIABLE as Material;
                    if (material == null) continue;
                
                    var ShadeTexture = material.GetTexture("_ShadeTexture");
                    // var RimColor = material.GetColor("_RimColor");
                    // var SphereAdd = material.GetTexture("_SphereAdd");
                    // var OutlineColor = material.GetColor("_OutlineColor");
                    
                    // ShadeTexture가 없는 경우 (일반적으로 lilToon이 아닌 경우)는 MainTex를 사용
                    if (ShadeTexture == null)
                    {
                        ShadeTexture = material.GetTexture("_MainTex");
                    }
                    
                    // Change the shader
                    material.shader = Shader.Find("VRM10/MToon10");
                
                    if (material.HasProperty("_ShadeTex"))
                    {
                        material.SetTexture("_ShadeTex", ShadeTexture);
                    }
                
                    // if (material.HasProperty("_RimColor"))
                    // {
                    //     material.SetColor("_RimColor", RimColor);
                    // }
                    //
                    // if (material.HasProperty("_SphereAdd"))
                    // {
                    //     material.SetTexture("_SphereAdd", SphereAdd);
                    // }
                    //
                    // if (material.HasProperty("_OutlineColor"))
                    // {
                    //     material.SetColor("_OutlineColor", OutlineColor);
                    // }
                }

                var gltfExportSettings = new GltfExportSettings();
                var exporter = new Vrm10Exporter(new EditorTextureSerializer(), gltfExportSettings);
                var option = new ExportArgs();
                exporter.Export(prefab, model, converter, option);
                var bytes = exporter.Storage.ToGlbBytes();
                File.WriteAllBytes(savePath, bytes);
            }
            
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
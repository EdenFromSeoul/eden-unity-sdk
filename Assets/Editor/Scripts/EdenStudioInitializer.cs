using System;
using Editor.Scripts.Manager;
using Runtime;
using UnityEditor;

namespace Editor.Scripts
{
    [InitializeOnLoad]
    public class EdenStudioInitializer
    {
        public static event Action<ItemInfo> OnSelectedItemChanged;

        private const string EdenStudioPath = "Assets/Eden";
        private static bool _initialized;

        private static ItemInfo _selectedItem;

        public static ItemInfo SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem == value) return;
                _selectedItem = value;
                OnSelectedItemChanged?.Invoke(value);
            }
        }

        static EdenStudioInitializer()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            // Add your initialization code here

            if (!AssetDatabase.IsValidFolder(EdenStudioPath))
            {
                AssetDatabase.CreateFolder("Assets", "Eden");
            }

            ItemManager.Initialize();
        }

        private static void OnEditorUpdate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            EditorApplication.update -= OnEditorUpdate;
            Initialize();
        }
    }
}
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor.Scripts
{
    public static class PresetManager
    {
        private const string Vrm10MorphTargetPresetPath = "Assets/Eden/Presets";
        
        public static Vrm10MorphTargetPreset LoadOrCreateVrm10MorphTargetPreset(string path)
        {
            //Presets 폴더 없으면 생성
            if (!Directory.Exists("Assets/Eden/Presets"))
            {
                Directory.CreateDirectory("Assets/Eden/Presets");
            }
            
            var presetPath = Vrm10MorphTargetPresetPath + "/" + path + ".asset";
            var preset = AssetDatabase.LoadAssetAtPath<Vrm10MorphTargetPreset>(presetPath);
            if (preset != null) return preset;
            preset = ScriptableObject.CreateInstance<Vrm10MorphTargetPreset>();
            AssetDatabase.CreateAsset(preset, presetPath);
            AssetDatabase.SaveAssets();
            return preset;
        }
        
        public static void SaveVrm10MorphTargetPreset(Vrm10MorphTargetPreset preset)
        {
            var presetPath = Vrm10MorphTargetPresetPath + "/" + preset.name + ".asset";
            var existingPreset = AssetDatabase.LoadAssetAtPath<Vrm10MorphTargetPreset>(presetPath);
            if (existingPreset)
            {
                existingPreset.presetName = preset.presetName;
                existingPreset.happy = preset.happy;
                existingPreset.angry = preset.angry;
                existingPreset.sad = preset.sad;
                existingPreset.surprised = preset.surprised;
                existingPreset.relaxed = preset.relaxed;
            }
            else
            {
                AssetDatabase.CreateAsset(preset, presetPath);
            }
            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssets();
        }
    }
}
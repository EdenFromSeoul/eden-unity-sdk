using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Editor.Scripts.Manager
{
    public enum Language
    {
        Korean,
        Japanese,
        English
    }

    public static class LocalizationManager
    {
        private static Dictionary<string, string> localizedText = new Dictionary<string, string>();
        private static string missingTextString = "Localized text not found";
        private static Language currentLanguage = Language.Korean;

        public static event Action OnLanguageChanged;

        static LocalizationManager()
        {
            LoadLocalizedText(currentLanguage);
        }

        public static void SetLanguage(Language language)
        {
            if (currentLanguage != language)
            {
                currentLanguage = language;
                Debug.Log($"Language set to {currentLanguage}");
                LoadLocalizedText(currentLanguage);
                OnLanguageChanged?.Invoke();
            }
        }

        private static void LoadLocalizedText(Language language)
        {
            localizedText.Clear();
            var filename = language switch
            {
                Language.Korean => "localization_ko.json",
                Language.Japanese => "localization_ja.json",
                Language.English => "localization_en.json",
                _ => "localization_ko.json"
            };
            string filePath = Path.Combine("Assets/Eden/Studios/Localization", filename);

            if (File.Exists(filePath))
            {
                Debug.Log($"File found at {filePath}");
                string dataAsJson = File.ReadAllText(filePath);
                LocalizationData loadedData = JsonUtility.FromJson<LocalizationData>(dataAsJson);

                for (int i = 0; i < loadedData.items.Length; i++)
                {
                    localizedText.Add(loadedData.items[i].key, loadedData.items[i].value);
                    
                }

                Debug.Log("Data loaded, dictionary contains: " + localizedText.Count + " entries");
            }
            else
            {
                Debug.LogError("Cannot find file at path: " + filePath);
            }
        }

        public static string GetLocalizedValue(string key)
        {
            return localizedText.TryGetValue(key, out var value) ? value : missingTextString;
        }
    }

    [System.Serializable]
    public class LocalizationData
    {
        public LocalizationItem[] items;
    }

    [System.Serializable]
    public class LocalizationItem
    {
        public string key;
        public string value;
    }
}

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace lilToon
{
    // For Custom Shader
    public class Localizer
    {
        // 言語
        private List<Dictionary<string, string>> languages = new List<Dictionary<string, string>>();
        private List<string> codes = new List<string>();
        private int number;

        public Localizer(string guid)
        {
            var paths = Directory.GetFiles(AssetDatabase.GUIDToAssetPath(guid), "*.json");
            var tmpNames = new List<string>();
            foreach(var path in paths)
            {
                var langData = File.ReadAllText(path);
                var lang = JsonDictionaryParser.Deserialize(langData);
                if(lang == null) continue;

                // 言語ファイルの名前が言語コードと一致していることを期待
                var code = Path.GetFileNameWithoutExtension(path);
                languages.Add(lang);
                codes.Add(code);
                try
                {
                    var cul = new CultureInfo(code);
                    tmpNames.Add(cul.NativeName);
                }
                catch
                {
                    tmpNames.Add(code);
                }
            }
            UpdateLanguage();
            Localization.changeLanguageCallback += UpdateLanguage;
        }

        private void UpdateLanguage()
        {
            number = GetIndexByCode(Localization.GetCurrentCode());
        }

        public string GetCurrentCode()
        {
            return codes[number];
        }

        public string[] GetCodes()
        {
            return codes.ToArray();
        }

        private int GetIndexByCode(string code)
        {
            var index = codes.IndexOf(code);
            if(index == -1) index = codes.IndexOf("en-US");
            if(index == -1) number = 0;
            return index;
        }

        // 単純にキーから翻訳を取得
        private string S(string key, int index)
        {
            return languages[index].TryGetValue(key, out string o) ? o : null;
        }

        public string S(string key, string code)
        {
            return S(key, GetIndexByCode(code));
        }

        public string S(string key)
        {
            return S(key, number);
        }

        public string SorKey(string key)
        {
            return languages[number].TryGetValue(key, out string o) ? o : key;
        }

        public string S(SerializedProperty property)
        {
            return S($"inspector.{property.name}");
        }

        // tooltip付きのGUIContentを生成
        public GUIContent G(string key)
        {
            if(DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0) return new GUIContent(S(key) ?? key);
            return new GUIContent(S(key) ?? key, S($"{key}.tooltip"));
        }

        public GUIContent G(SerializedProperty property)
        {
            return G($"inspector.{property.name}");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace lilToon
{
    internal static class Localization
    {
        // 設定ファイルの保存先
        private static readonly string PATH_PREF = $"{UnityEditorInternal.InternalEditorUtility.unityPreferencesFolder}/jp.lilxyzw";
        private static readonly string FILENAME_SETTING = lilConstants.PACKAGE_NAME + ".language.conf";
        private static string PATH_SETTING => $"{PATH_PREF}/{FILENAME_SETTING}";

        // 言語
        private static List<Dictionary<string, string>> languages;
        private static List<string> codes = new List<string>();
        private static string[] names;
        private static int number;
        internal static Action changeLanguageCallback;

        internal static void LoadDatas()
        {
            languages = new List<Dictionary<string, string>>();
            var paths = Directory.GetFiles(lilDirectoryManager.GetLocalizationPath(), "*.json");
            var tmpNames = new List<string>();
            foreach(var path in paths)
            {
                var langData = File.ReadAllText(path);
                var lang = JsonDictionaryParser.Deserialize(langData);
                if(lang == null) continue;
                InitializeLabels(lang);

                // 言語ファイルの名前が言語コードと一致していることを期待
                var code = Path.GetFileNameWithoutExtension(path);
                languages.Add(lang);
                codes.Add(code);
                tmpNames.Add(lang.TryGetValue("Language", out string o) ? o : code);
            }
            names = tmpNames.ToArray();
            number = GetIndexByCode(LoadLanguageSettings());
        }

        private static void InitializeLabels(Dictionary<string, string> lang)
        {
            string TryS(Dictionary<string, string> lang, string key)
            {
                return lang.TryGetValue(key, out string o) ? o : null;
            }

            lang["sCullModes"]                = lilLanguageManager.BuildParams(TryS(lang, "sCullMode"), TryS(lang, "sCullModeOff"), TryS(lang, "sCullModeFront"), TryS(lang, "sCullModeBack"));
            lang["sBlendModes"]               = lilLanguageManager.BuildParams(TryS(lang, "sBlendMode"), TryS(lang, "sBlendModeNormal"), TryS(lang, "sBlendModeAdd"), TryS(lang, "sBlendModeScreen"), TryS(lang, "sBlendModeMul"));
            lang["sAlphaModes"]               = lilLanguageManager.BuildParams(TryS(lang, "sTransparentMode"), TryS(lang, "sAlphaMaskModeNone"), TryS(lang, "sAlphaMaskModeReplace"), TryS(lang, "sAlphaMaskModeMul"), TryS(lang, "sAlphaMaskModeAdd"), TryS(lang, "sAlphaMaskModeSub"));
            lang["sAlphaMaskModes"]           = lilLanguageManager.BuildParams(TryS(lang, "sAlphaMask"), TryS(lang, "sAlphaMaskModeNone"), TryS(lang, "sAlphaMaskModeReplace"), TryS(lang, "sAlphaMaskModeMul"), TryS(lang, "sAlphaMaskModeAdd"), TryS(lang, "sAlphaMaskModeSub"));
            lang["sBlinkSettings"]            = lilLanguageManager.BuildParams(TryS(lang, "sBlinkStrength"), TryS(lang, "sBlinkType"), TryS(lang, "sBlinkSpeed"), TryS(lang, "sBlinkOffset"));
            lang["sDistanceFadeSettings"]     = lilLanguageManager.BuildParams(TryS(lang, "sStartDistance"), TryS(lang, "sEndDistance"), TryS(lang, "sStrength"), TryS(lang, "sBackfaceForceShadow"));
            lang["sDistanceFadeModes"]        = lilLanguageManager.BuildParams("Mode", TryS(lang, "sVertex"), TryS(lang, "sDissolveModePosition"));
            lang["sDissolveParams"]           = lilLanguageManager.BuildParams(TryS(lang, "sDissolveMode"), TryS(lang, "sDissolveModeNone"), TryS(lang, "sDissolveModeAlpha"), TryS(lang, "sDissolveModeUV"), TryS(lang, "sDissolveModePosition"), TryS(lang, "sDissolveShape"), TryS(lang, "sDissolveShapePoint"), TryS(lang, "sDissolveShapeLine"), TryS(lang, "sBorder"), TryS(lang, "sBlur"));
            lang["sDissolveParamsModes"]      = lilLanguageManager.BuildParams(TryS(lang, "sDissolve"), TryS(lang, "sDissolveModeNone"), TryS(lang, "sDissolveModeAlpha"), TryS(lang, "sDissolveModeUV"), TryS(lang, "sDissolveModePosition"));
            lang["sDissolveParamsOther"]      = lilLanguageManager.BuildParams(TryS(lang, "sDissolveShape"), TryS(lang, "sDissolveShapePoint"), TryS(lang, "sDissolveShapeLine"), TryS(lang, "sBorder"), TryS(lang, "sBlur"), "Dummy");
            lang["sGlitterParams1"]           = lilLanguageManager.BuildParams("Tiling", TryS(lang, "sParticleSize"), TryS(lang, "sContrast"));
            lang["sGlitterParams2"]           = lilLanguageManager.BuildParams(TryS(lang, "sBlinkSpeed"), TryS(lang, "sAngleLimit"), TryS(lang, "sRimLightDirection"), TryS(lang, "sColorRandomness"));
            lang["sOutlineVertexColorUsages"] = lilLanguageManager.BuildParams(TryS(lang, "sVertexColor"), TryS(lang, "sNone"), TryS(lang, "sVertexR2Width"), TryS(lang, "sVertexRGBA2Normal"));
            lang["sShadowColorTypes"]         = lilLanguageManager.BuildParams(TryS(lang, "sColorType"), TryS(lang, "sColorTypeNormal"), TryS(lang, "sColorTypeLUT"));
            lang["sShadowMaskTypes"]          = lilLanguageManager.BuildParams(TryS(lang, "sMaskType"), TryS(lang, "sStrength"), TryS(lang, "sFlat"));
            lang["sHSVGs"]                    = lilLanguageManager.BuildParams(TryS(lang, "sHue"), TryS(lang, "sSaturation"), TryS(lang, "sValue"), TryS(lang, "sGamma"));
            lang["sScrollRotates"]            = lilLanguageManager.BuildParams(TryS(lang, "sAngle"), TryS(lang, "sUVAnimation"), TryS(lang, "sScroll"), TryS(lang, "sRotate"));
            lang["sDecalAnimations"]          = lilLanguageManager.BuildParams(TryS(lang, "sAnimation"), TryS(lang, "sXFrames"), TryS(lang, "sYFrames"), TryS(lang, "sFrames"), TryS(lang, "sFPS"));
            lang["sDecalSubParams"]           = lilLanguageManager.BuildParams(TryS(lang, "sXRatio"), TryS(lang, "sYRatio"), TryS(lang, "sFixBorder"));
            lang["sAudioLinkUVModes"]         = lilLanguageManager.BuildParams(TryS(lang, "sAudioLinkUVMode"), TryS(lang, "sAudioLinkUVModeNone"), TryS(lang, "sAudioLinkUVModeRim"), TryS(lang, "sAudioLinkUVModeUV"), TryS(lang, "sAudioLinkUVModeMask"), TryS(lang, "sAudioLinkUVModeMask") + " (Spectrum)", TryS(lang, "sAudioLinkUVModePosition"));
            lang["sAudioLinkVertexUVModes"]   = lilLanguageManager.BuildParams(TryS(lang, "sAudioLinkUVMode"), TryS(lang, "sAudioLinkUVModeNone"), TryS(lang, "sAudioLinkUVModePosition"), TryS(lang, "sAudioLinkUVModeUV"), TryS(lang, "sAudioLinkUVModeMask"));
            lang["sAudioLinkVertexStrengths"] = lilLanguageManager.BuildParams(TryS(lang, "sAudioLinkMovingVector"), TryS(lang, "sAudioLinkNormalStrength"));
            lang["sAudioLinkLocalMapParams"]  = lilLanguageManager.BuildParams(TryS(lang, "sAudioLinkLocalMapBPM"), TryS(lang, "sAudioLinkLocalMapNotes"), TryS(lang, "sOffset"));
            lang["sFakeShadowVectors"]        = lilLanguageManager.BuildParams(TryS(lang, "sVector"), TryS(lang, "sOffset"));
            lang["sFurVectors"]               = lilLanguageManager.BuildParams(TryS(lang, "sVector"), TryS(lang, "sLength"));
            lang["sPreOutTypes"]              = lilLanguageManager.BuildParams(TryS(lang, "sOutType"), TryS(lang, "sOutTypeNormal"), TryS(lang, "sOutTypeFlat"), TryS(lang, "sOutTypeMono"));
            lang["sLightDirectionOverrides"]  = lilLanguageManager.BuildParams(TryS(lang, "sLightDirectionOverride"), TryS(lang, "sObjectFollowing"));
        }

        internal static string GetCurrentCode()
        {
            return codes[number];
        }

        internal static string[] GetCodes()
        {
            return codes.ToArray();
        }

        private static int GetIndexByCode(string code)
        {
            var index = codes.IndexOf(code);
            if(index == -1) index = codes.IndexOf("en-US");
            if(index == -1) number = 0;
            return index;
        }

        // 単純にキーから翻訳を取得
        private static string S(string key, int index)
        {
            return languages[index].TryGetValue(key, out string o) ? o : null;
        }

        internal static string S(string key, string code)
        {
            return S(key, GetIndexByCode(code));
        }

        internal static string S(string key)
        {
            return S(key, number);
        }

        internal static string SorKey(string key)
        {
            return languages[number].TryGetValue(key, out string o) ? o : key;
        }

        internal static string S(SerializedProperty property)
        {
            return S($"inspector.{property.name}");
        }

        // tooltip付きのGUIContentを生成
        internal static GUIContent G(string key)
        {
            if(DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0) return new GUIContent(S(key) ?? key);
            return new GUIContent(S(key) ?? key, S($"{key}.tooltip"));
        }

        internal static GUIContent G(SerializedProperty property)
        {
            return G($"inspector.{property.name}");
        }

        // 各所で表示される言語設定GUI
        internal static bool SelectLanguageGUI()
        {
            EditorGUI.BeginChangeCheck();
            number = EditorGUILayout.Popup("Editor Language", number, names);
            if(EditorGUI.EndChangeCheck())
            {
                changeLanguageCallback?.Invoke();
                SaveLanguageSettings();
                return true;
            }
            return false;
        }

        internal static void SelectLanguageGUI(Rect position)
        {
            EditorGUI.BeginChangeCheck();
            number = EditorGUI.Popup(position, "Editor Language", number, names);
            if(EditorGUI.EndChangeCheck()) SaveLanguageSettings();
        }

        // 設定ファイルの読み込みと保存
        private static string LoadLanguageSettings()
        {
            if(!Directory.Exists(PATH_PREF)) Directory.CreateDirectory(PATH_PREF);
            if(!File.Exists(PATH_SETTING)) File.WriteAllText(PATH_SETTING, CultureInfo.CurrentCulture.Name);
            return SafeIO.LoadFile(PATH_SETTING);
        }

        private static void SaveLanguageSettings()
        {
            if(!Directory.Exists(PATH_PREF)) Directory.CreateDirectory(PATH_PREF);
            SafeIO.SaveFile(PATH_SETTING, codes[number]);
        }
    }

    // なるべく安全に保存・読み込み
    internal static class SafeIO
    {
        internal static void SaveFile(string path, string content)
        {
            using(var fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                fs.SetLength(0);
                using(var sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    sw.Write(content);
                }
            }
        }

        internal static string LoadFile(string path)
        {
            using(var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using(var sr = new StreamReader(fs, Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }
    }
}

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace lilToon
{
    public class lilLanguageManager
    {
        public static string sMainColorBranch;
        public static string sCullModes;
        public static string sBlendModes;
        public static string sAlphaModes;
        public static string sAlphaMaskModes;
        public static string blinkSetting;
        public static string sDistanceFadeSetting;
        public static string sDistanceFadeSettingMode;
        public static string sDissolveParams;
        public static string sDissolveParamsMode;
        public static string sDissolveParamsOther;
        public static string sGlitterParams1;
        public static string sGlitterParams2;
        public static string sTransparentMode;
        public static string sOutlineVertexColorUsages;
        public static string sShadowColorTypes;
        public static string sShadowMaskTypes;
        public static string[] sRenderingModeList;
        public static string[] sRenderingModeListLite;
        public static string[] sTransparentModeList;
        public static string[] sBlendModeList;
        public static GUIContent mainColorRGBAContent;
        public static GUIContent colorRGBAContent;
        public static GUIContent colorAlphaRGBAContent;
        public static GUIContent maskBlendContent;
        public static GUIContent maskBlendRGBContent;
        public static GUIContent maskBlendRGBAContent;
        public static GUIContent colorMaskRGBAContent;
        public static GUIContent alphaMaskContent;
        public static GUIContent ditherContent;
        public static GUIContent maskStrengthContent;
        public static GUIContent normalMapContent;
        public static GUIContent noiseMaskContent;
        public static GUIContent adjustMaskContent;
        public static GUIContent matcapContent;
        public static GUIContent gradationContent;
        public static GUIContent gradSpeedContent;
        public static GUIContent smoothnessContent;
        public static GUIContent metallicContent;
        public static GUIContent parallaxContent;
        public static GUIContent audioLinkMaskContent;
        public static GUIContent audioLinkMaskSpectrumContent;
        public static GUIContent customMaskContent;
        public static GUIContent shadow1stColorRGBAContent;
        public static GUIContent shadow2ndColorRGBAContent;
        public static GUIContent shadow3rdColorRGBAContent;
        public static GUIContent blurMaskRGBContent;
        public static GUIContent shadowAOMapContent;
        public static GUIContent widthMaskContent;
        public static GUIContent lengthMaskContent;
        public static GUIContent triMaskContent;
        public static GUIContent cubemapContent;
        public static GUIContent audioLinkLocalMapContent;
        public static GUIContent gradationMapContent;
        public static LanguageSettings langSet { get { return LanguageSettings.instance; } }

        public class LanguageSettings : ScriptableSingleton<LanguageSettings>
        {
            public int languageNum = -1;
            public string languageNames = "";
            public string languageName = "English";
        }

        public static string S(string key) { return Localization.S(key); }
        public static string BuildParams(params string[] labels) { return string.Join("|", labels); }

        public static bool ShouldApplyTemp()
        {
            return string.IsNullOrEmpty(langSet.languageNames);
        }

        public static void UpdateLanguage()
        {
            Localization.LoadDatas();
            InitializeLabels();
        }

        public static void SelectLang()
        {
            EditorGUI.BeginChangeCheck();
            Localization.SelectLanguageGUI();
            if(EditorGUI.EndChangeCheck()) InitializeLabels();
            if(!string.IsNullOrEmpty(S("sLanguageWarning"))) EditorGUILayout.HelpBox(S("sLanguageWarning"),MessageType.Warning);
        }

        private static void InitializeLabels()
        {
            sCullModes                      = S("sCullModes");
            sBlendModes                     = S("sBlendModes");
            sAlphaModes                     = S("sAlphaModes");
            sAlphaMaskModes                 = S("sAlphaMaskModes");
            blinkSetting                    = S("sBlinkSettings");
            sDistanceFadeSetting            = S("sDistanceFadeSettings");
            sDistanceFadeSettingMode        = S("sDistanceFadeModes");
            sDissolveParams                 = S("sDissolveParams");
            sDissolveParamsMode             = S("sDissolveParamsModes");
            sDissolveParamsOther            = S("sDissolveParamsOther");
            sGlitterParams1                 = S("sGlitterParams1");
            sGlitterParams2                 = S("sGlitterParams2");
            sTransparentMode                = BuildParams(S("sRenderingMode"), S("sRenderingModeOpaque"), S("sRenderingModeCutout"), S("sRenderingModeTransparent"), S("sRenderingModeRefraction"), S("sRenderingModeFur"), S("sRenderingModeFurCutout"), S("sRenderingModeGem"));
            sRenderingModeList              = new[]{S("sRenderingModeOpaque"), S("sRenderingModeCutout"), S("sRenderingModeTransparent"), S("sRenderingModeRefraction"), S("sRenderingModeRefractionBlur"), S("sRenderingModeFur"), S("sRenderingModeFurCutout"), S("sRenderingModeFurTwoPass"), S("sRenderingModeGem")};
            sRenderingModeListLite          = new[]{S("sRenderingModeOpaque"), S("sRenderingModeCutout"), S("sRenderingModeTransparent")};
            sTransparentModeList            = new[]{S("sTransparentModeNormal"), S("sTransparentModeOnePass"), S("sTransparentModeTwoPass")};
            sBlendModeList                  = new[]{S("sBlendModeNormal"), S("sBlendModeAdd"), S("sBlendModeScreen"), S("sBlendModeMul")};
            sOutlineVertexColorUsages       = S("sOutlineVertexColorUsages");
            sShadowColorTypes               = S("sShadowColorTypes");
            sShadowMaskTypes                = S("sShadowMaskTypes");
            colorRGBAContent                = new GUIContent(S("sColor"),                              S("sTextureRGBA"));
            colorAlphaRGBAContent           = new GUIContent(S("sColorAlpha"),                         S("sTextureRGBA"));
            maskBlendContent                = new GUIContent(S("sMask"),                               S("sBlendR"));
            maskBlendRGBContent             = new GUIContent(S("sMask"),                               S("sTextureRGB"));
            maskBlendRGBAContent            = new GUIContent(S("sMask"),                               S("sTextureRGBA"));
            colorMaskRGBAContent            = new GUIContent(S("sColor") + " / " + S("sMask"),    S("sTextureRGBA"));
            alphaMaskContent                = new GUIContent(S("sAlphaMask"),                          S("sAlphaR"));
            ditherContent                   = new GUIContent(S("sDither"),                             S("sAlphaR"));
            maskStrengthContent             = new GUIContent(S("sStrengthMask"),                       S("sStrengthR"));
            normalMapContent                = new GUIContent(S("sNormalMap"),                          S("sNormalRGB"));
            noiseMaskContent                = new GUIContent(S("sNoise"),                              S("sNoiseR"));
            matcapContent                   = new GUIContent(S("sMatCap"),                             S("sTextureRGBA"));
            gradationContent                = new GUIContent(S("sGradation"),                          S("sTextureRGBA"));
            gradSpeedContent                = new GUIContent(S("sGradTexSpeed"),                       S("sTextureRGBA"));
            smoothnessContent               = new GUIContent(S("sSmoothness"),                         S("sSmoothnessR"));
            metallicContent                 = new GUIContent(S("sMetallic"),                           S("sMetallicR"));
            parallaxContent                 = new GUIContent(S("sParallax"),                           S("sParallaxR"));
            audioLinkMaskContent            = new GUIContent(S("sMask"),                               S("sAudioLinkMaskRGB"));
            audioLinkMaskSpectrumContent    = new GUIContent(S("sMask"),                               S("sAudioLinkMaskRGBSpectrum"));
            customMaskContent               = new GUIContent(S("sMask"),                               "");
            shadow1stColorRGBAContent       = new GUIContent(S("sShadow1stColor"),                     S("sTextureRGBA"));
            shadow2ndColorRGBAContent       = new GUIContent(S("sShadow2ndColor"),                     S("sTextureRGBA"));
            shadow3rdColorRGBAContent       = new GUIContent(S("sShadow3rdColor"),                     S("sTextureRGBA"));
            blurMaskRGBContent              = new GUIContent(S("sBlurMask"),                           S("sBlurRGB"));
            shadowAOMapContent              = new GUIContent("AO Map",                                      S("sBorderRGB"));
            widthMaskContent                = new GUIContent(S("sWidth"),                              S("sWidthR"));
            lengthMaskContent               = new GUIContent(S("sLengthMask"),                         S("sStrengthR"));
            triMaskContent                  = new GUIContent(S("sTriMask"),                            S("sTriMaskRGB"));
            cubemapContent                  = new GUIContent("Cubemap Fallback");
            audioLinkLocalMapContent        = new GUIContent(S("sAudioLinkLocalMap"));
            gradationMapContent             = new GUIContent(S("sGradationMap"));
        }

        public static string GetDisplayLabel(MaterialProperty prop)
        {
            var labels = prop.displayName.Split('|').First().Split('+').Select(m=>S(m)).ToArray();
            if(Event.current.alt) labels[0] = prop.name;
            return string.Join("", labels);
        }

        public static string GetDisplayName(MaterialProperty prop)
        {
            var labels = prop.displayName.Split('|').Select(
                n=>string.Join("",n.Split('+').Select(m=>S(m)).ToArray())
            ).ToArray();
            if(Event.current.alt)
            {
                if(labels[0].Contains("|")) labels[0] = prop.name + labels[0].Substring(labels[0].IndexOf("|"));
                else labels[0] = prop.name;
            }
            return string.Join("|", labels);
        }

        public static string GetDisplayName(string label)
        {
            return string.Join("|",
                label.Split('|').Select(
                    n=>string.Join("",n.Split('+').Select(m=>S(m)).ToArray())
                ).ToArray()
            );
        }

        // For Custom Shader
        private static Dictionary<string,string> loc;

        public static string GetLoc(string key)
        {
            var s = S(key);
            return !string.IsNullOrEmpty(s) ? s : loc.TryGetValue(key, out string o) ? o : null;
        }

        public static void LoadCustomLanguage(string langFileGUID)
        {
            LoadLanguage(lilDirectoryManager.GUIDToPath(langFileGUID));
        }

        private static void LoadLanguage(string langPath)
        {
            if(string.IsNullOrEmpty(langPath) || !File.Exists(langPath)) return;
            loc = new Dictionary<string, string>();
            StreamReader sr = new StreamReader(langPath);

            string str = sr.ReadLine();
            langSet.languageNames = str.Substring(str.IndexOf("\t")+1);
            langSet.languageName = langSet.languageNames.Split('\t')[langSet.languageNum];
            while((str = sr.ReadLine()) != null)
            {
                var lineContents = str.Split('\t');
                loc[lineContents[0]] = lineContents[langSet.languageNum+1];
            }
            sr.Close();
        }
    }
}
#endif
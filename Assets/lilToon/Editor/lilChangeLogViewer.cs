#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace lilToon
{
    internal static class lilChangeLogViewer
    {
        internal static IEnumerator GetChangelogEn()
        {
            using(UnityWebRequest webRequest = UnityWebRequest.Get(lilConstants.changelogEnURL))
            {
                yield return webRequest.SendWebRequest();
                #if UNITY_2020_2_OR_NEWER
                    if(webRequest.result != UnityWebRequest.Result.ConnectionError)
                #else
                    if(!webRequest.isNetworkError)
                #endif
                {
                    lilEditorParameters.instance.changelogEn = ParseChangelog(webRequest.downloadHandler.text);
                }
            }
        }

        internal static IEnumerator GetChangelogJp()
        {
            using(UnityWebRequest webRequest = UnityWebRequest.Get(lilConstants.changelogJpURL))
            {
                yield return webRequest.SendWebRequest();
                #if UNITY_2020_2_OR_NEWER
                    if(webRequest.result != UnityWebRequest.Result.ConnectionError)
                #else
                    if(!webRequest.isNetworkError)
                #endif
                {
                    lilEditorParameters.instance.changelogJp = ParseChangelog(webRequest.downloadHandler.text).Replace(" ", "\u00A0");
                }
            }
        }

        private static string ParseChangelog(string md)
        {
            var sb = new StringBuilder();
            using(var sr = new StringReader(md))
            {
                // Skip header
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine();

                string line;
                while((line = sr.ReadLine()) != null)
                {
                    line = line.ReplaceSyntax("`", "\u2006<color=#e96900>", "</color>\u2006");
                    if(line.StartsWith("### "))
                    {
                        sb.AppendLine($"<size=15><b>{line.Substring(4)}</b></size>");
                    }
                    else if(line.StartsWith("## "))
                    {
                        sb.AppendLine($"<color=#2d9c63><size=20><b>{line.Substring(3)}</b></size></color>");
                    }
                    else
                    {
                        sb.AppendLine("  " + line);
                    }
                }
            }
            return sb.ToString();
        }

        private static string ReplaceSyntax(this string s, string syntax, string start, string end)
        {
            while(true)
            {
                var first = s.IndexOf(syntax);
                if(first == -1) return s;

                var length = syntax.Length;
                var second = s.IndexOf(syntax, first + length);
                if(second == -1) return s;

                s = s.Remove(first) + start + s.Substring(first + length);
                var second2 = s.IndexOf(syntax);
                s = s.Remove(second2) + end + s.Substring(second2 + length);
            }
        }
    }

    internal class lilChangeLogWindow : EditorWindow
    {
        private static GUIStyle style;
        private Vector2 scrollPosition = Vector2.zero;
        [MenuItem("Window/_lil/Changelog")]
        internal static void Init()
        {
            var window = (lilChangeLogWindow)GetWindow(typeof(lilChangeLogWindow));
            window.Show();
        }

        void OnGUI()
        {
            if(style == null) style = new GUIStyle(EditorStyles.label){richText = true, wordWrap = true};

            EditorGUI.indentLevel++;
            lilLanguageManager.SelectLang();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if(lilLanguageManager.LanguageSettings.instance.languageName == "Japanese" && !string.IsNullOrEmpty(lilEditorParameters.instance.changelogJp))
            {
                EditorGUILayout.LabelField(lilEditorParameters.instance.changelogJp, style);
            }
            else if(!string.IsNullOrEmpty(lilEditorParameters.instance.changelogEn))
            {
                EditorGUILayout.LabelField(lilEditorParameters.instance.changelogEn, style);
            }
            else
            {
                EditorGUILayout.LabelField(Localization.S("sChangelogLoadFailed"));
            }
            EditorGUILayout.EndScrollView();

            EditorGUI.indentLevel--;
        }
    }
}
#endif

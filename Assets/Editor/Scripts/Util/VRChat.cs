using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Editor.Scripts.Struct;
using UnityEngine;
using UniVRM10;
using VRC.SDK3.Avatars.Components;

namespace Editor.Scripts.Util
{
    public static class VRChat
    {
        internal enum HandGesture
        {
            Victory,
            RockNRoll,
            HandOpen,
            ThumbsUp,
            Peace,
            HandGun,
            FingerPoint,
        }

        /// <summary>
        /// 自動まばたきに利用されるメッシュのオブジェクトのパス。
        /// </summary>
        internal static readonly string AutoBlinkMeshPath = "Body";

        static readonly IDictionary<ExpressionPreset, int> ExpressionPresetVRChatVisemeDict =
            new Dictionary<ExpressionPreset, int>
            {
                { ExpressionPreset.aa, 10 },
                { ExpressionPreset.ih, 12 },
                { ExpressionPreset.ou, 14 },
                { ExpressionPreset.ee, 11 },
                { ExpressionPreset.oh, 13 },
            };

        static readonly IDictionary<string, ExpressionPreset> ExpressionPresetCustomDict =
            new Dictionary<string, ExpressionPreset>
            {
                {"happy", ExpressionPreset.happy},
                {"sad", ExpressionPreset.sad},
                {"angry", ExpressionPreset.angry},
                {"relaxed", ExpressionPreset.relaxed},
                {"surprised", ExpressionPreset.surprised},
            };

        // regex of blink estimated shape key name
        private static readonly Regex BlinkShapeKeyRegex = new("blink|まばたき|またたき|瞬き|eye|目|瞳|眼|wink|ウィンク|ｳｨﾝｸ|ウインク|ｳｲﾝｸ",
            RegexOptions.IgnoreCase);

        internal static (IEnumerable<AnimationClip> clips, IDictionary<ExpressionPreset, VRM10Expression> expressions)
            GetExpressionsFromVRChatAvatar(GameObject gameObject, IEnumerable<string> shapeKeyNames, IDictionary<string, List<BlendShapeData>> selectedBlendShapes)
        {
            var clips = new List<AnimationClip>();
            var expressions = new Dictionary<ExpressionPreset, VRM10Expression>();
            var avatarDescriptor = gameObject.GetComponent<VRCAvatarDescriptor>();
            var visemeBlendShapes = avatarDescriptor.VisemeBlendShapes;
            var shapeKeyNamesList = shapeKeyNames.ToList();

            foreach (var (preset, i) in ExpressionPresetVRChatVisemeDict)
            {
                var shapeKeyName = visemeBlendShapes[i];
                if (shapeKeyName == null || !shapeKeyNamesList.Contains(shapeKeyName))
                {
                    continue;
                }

                var expression = ScriptableObject.CreateInstance<VRM10Expression>();
                expression.MorphTargetBindings = new[]
                {
                    new MorphTargetBinding
                    {
                        RelativePath = "Body", Index = shapeKeyNamesList.IndexOf(shapeKeyName), Weight = 1.0f
                    },
                };

                expressions.Add(preset, expression);
            }

            GetBlinkExpressionsFromVRChatAvatar(gameObject, shapeKeyNamesList, expressions);
            GetOtherExpressions(gameObject, expressions, selectedBlendShapes);

            return (clips, expressions);
        }

        internal static IEnumerable<string> GetBlinkShapeKeyNames(IEnumerable<string> shapeKeyNames)
        {
            return shapeKeyNames.Where(shapeKeyName => BlinkShapeKeyRegex.IsMatch(shapeKeyName));
        }

        private static void GetBlinkExpressionsFromVRChatAvatar(GameObject gameObject,
            IEnumerable<string> shapeKeyNames,
            IDictionary<ExpressionPreset, VRM10Expression> expressions)
        {
            var shapeKeyNamesList = shapeKeyNames.ToList();
            var body = gameObject.transform.Find("Body");
            var dummyBlinkShapeKeyNamesList = new List<string>();

            if (body)
            {
                var renderer = body.GetComponent<SkinnedMeshRenderer>();
                if (renderer && renderer.sharedMesh && renderer.sharedMesh.blendShapeCount >= 4)
                {
                    dummyBlinkShapeKeyNamesList.Add(renderer.sharedMesh.GetBlendShapeName(0));
                    dummyBlinkShapeKeyNamesList.Add(renderer.sharedMesh.GetBlendShapeName(1));
                }
            }

            var customEyeLookSettings = gameObject.GetComponent<VRCAvatarDescriptor>().customEyeLookSettings;
            if (customEyeLookSettings.eyelidsSkinnedMesh
                && customEyeLookSettings.eyelidsSkinnedMesh.sharedMesh
                && customEyeLookSettings.eyelidsBlendshapes != null
                && customEyeLookSettings.eyelidsBlendshapes.Length == 3
                && customEyeLookSettings.eyelidsSkinnedMesh.sharedMesh.blendShapeCount >
                customEyeLookSettings.eyelidsBlendshapes[0])
            {
                var expression = ScriptableObject.CreateInstance<VRM10Expression>();
                expression.MorphTargetBindings = new[]
                {
                    new MorphTargetBinding
                    {
                        RelativePath = "Body", Index = customEyeLookSettings.eyelidsBlendshapes[0],
                        Weight = 1.0f
                    },
                };

                expressions.Add(ExpressionPreset.blink, expression);
            }

            var blinkShapeKeyNames = GetBlinkShapeKeyNames(shapeKeyNamesList);
            Debug.Log(string.Join(", ", blinkShapeKeyNames));
        }

        private static void GetOtherExpressions(GameObject gameObject,
            IDictionary<ExpressionPreset, VRM10Expression> expressions,
            IDictionary<string, List<BlendShapeData>> selectedBlendShapes)
        {
            // selectedBlendShapes 에 있는 것들을 추가
            foreach (var selected in selectedBlendShapes)
            {
                var expressionPreset = ExpressionPresetCustomDict[selected.Key];
                var expression = ScriptableObject.CreateInstance<VRM10Expression>();
                expression.MorphTargetBindings = selected.Value.Select(data =>
                    new MorphTargetBinding
                    {
                        RelativePath = "Body", Index = data.index, Weight = 1.0f
                    }).ToArray();

                expressions.Add(expressionPreset, expression);
            }
        }
    }
}
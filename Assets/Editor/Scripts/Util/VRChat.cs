using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UniVRM10;
using VRC.SDK3.Avatars.Components;

namespace Editor.Scripts.Util
{
    public static class VRChat
    {
        enum HandGesture
        {
            Victory,
            RockNRoll,
            HandOpen,
            ThumbsUp,
            Peace,
            HandGun,
        }

        static readonly IDictionary<ExpressionPreset, int> ExpressionPresetVRChatVisemeDict =
            new Dictionary<ExpressionPreset, int>
            {
                { ExpressionPreset.aa, 10 },
                { ExpressionPreset.ih, 12 },
                { ExpressionPreset.ou, 14 },
                { ExpressionPreset.ee, 11 },
                { ExpressionPreset.oh, 13 },
            };

        // regex of blink estimated shape key name
        private static readonly Regex BlinkShapeKeyRegex = new("blink|まばたき|またたき|瞬き|eye|目|瞳|眼|wink|ウィンク|ｳｨﾝｸ|ウインク|ｳｲﾝｸ",
            RegexOptions.IgnoreCase);

        internal static (IEnumerable<AnimationClip> clips, IDictionary<ExpressionPreset, VRM10Expression> expressions)
            GetExpressionsFromVRChatAvatar(GameObject gameObject, IEnumerable<string> shapeKeyNames)
        {
            var clips = new List<AnimationClip>();
            var expressions = new Dictionary<ExpressionPreset, VRM10Expression>();
            var avatarDescriptor = gameObject.GetComponent<VRCAvatarDescriptor>();
            var visemeBlendShapes = avatarDescriptor.VisemeBlendShapes;
            var shapeKeyNamesList = shapeKeyNames.ToList();
            Debug.Log(string.Join(", ", shapeKeyNamesList));
            Debug.Log(string.Join(", ", visemeBlendShapes));

            foreach (var (preset, i) in ExpressionPresetVRChatVisemeDict)
            {
                var shapeKeyName = visemeBlendShapes[i];
                if (shapeKeyName == null || !shapeKeyNamesList.Contains(shapeKeyName))
                {
                    continue;
                }

                // index 찾기
                var index = shapeKeyNamesList.ToList().IndexOf(shapeKeyName);
                Debug.Log($"{preset}: {shapeKeyName} ({index})");

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
    }
}
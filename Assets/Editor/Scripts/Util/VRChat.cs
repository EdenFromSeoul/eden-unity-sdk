using System.Collections.Generic;
using System.Linq;
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

        internal static (IEnumerable<AnimationClip> clips, IDictionary<ExpressionPreset, VRM10Expression> expressions)
            GetExpressionsFromVRChatAvatar(GameObject gameObject, IEnumerable<string> shapeKeyNames)
        {
            var clips = new List<AnimationClip>();
            var expressions = new Dictionary<ExpressionPreset, VRM10Expression>();
            var avatarDescriptor = gameObject.GetComponent<VRCAvatarDescriptor>();
            var visemeBlendShapes = avatarDescriptor.VisemeBlendShapes;
            Debug.Log(string.Join(", ", shapeKeyNames));
            Debug.Log(string.Join(", ", visemeBlendShapes));

            if (visemeBlendShapes != null)
            {
                foreach (var (preset, i) in ExpressionPresetVRChatVisemeDict)
                {
                    var shapeKeyName = visemeBlendShapes[i];
                    if (shapeKeyName == null || !shapeKeyNames.Contains(shapeKeyName))
                    {
                        continue;
                    }

                    // index 찾기
                    var index = shapeKeyNames.ToList().IndexOf(shapeKeyName);
                    Debug.Log($"{preset}: {shapeKeyName} ({index})");

                    var expression = ScriptableObject.CreateInstance<VRM10Expression>();
                    expression.MorphTargetBindings = new[]
                    {
                        new MorphTargetBinding { RelativePath = "Body", Index = shapeKeyNames.ToList().IndexOf(shapeKeyName), Weight = 1.0f },
                    };

                    expressions.Add(preset, expression);
                }
            }

            return (clips, expressions);
        }
    }
}
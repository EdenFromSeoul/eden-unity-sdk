using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Editor.Scripts.Struct;
using UnityEngine;
using UniVRM10;
using VRC.SDK3.Avatars.Components;
using ExpressionPreset = UniVRM10.ExpressionPreset;

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
                { "happy", ExpressionPreset.happy },
                { "sad", ExpressionPreset.sad },
                { "angry", ExpressionPreset.angry },
                { "relaxed", ExpressionPreset.relaxed },
                { "surprised", ExpressionPreset.surprised },
            };

        // regex of blink estimated shape key name
        private static readonly Regex BlinkShapeKeyRegex = new("blink|まばたき|またたき|瞬き|eye|目|瞳|眼|wink|ウィンク|ｳｨﾝｸ|ウインク|ｳｲﾝｸ",
            RegexOptions.IgnoreCase);

        /// <summary>
        /// 【SDK2】Cats Blender PluginでVRChat用に生成されるまばたきのシェイプキー名。
        /// </summary>
        /// <remarks>
        /// 参照:
        /// cats-blender-plugin/eyetracking.py at 0.13.3 · michaeldegroot/cats-blender-plugin
        /// <https://github.com/michaeldegroot/cats-blender-plugin/blob/0.13.3/tools/eyetracking.py>
        /// </remarks>
        private static readonly IEnumerable<string> OrderedBlinkGeneratedByCatsBlenderPlugin
            = new string[] { "vrc.blink_left", "vrc.blink_right", "vrc.lowerlid_left", "vrc.lowerlid_right" };

        internal static (IEnumerable<AnimationClip> clips, IDictionary<ExpressionPreset, VRChatExpressionBinding>
            expressions
            ) GetExpressions(GameObject gameObject, IEnumerable<string> shapeKeyNames,
                IDictionary<string, List<BlendShapeData>> selectedBlendShapes)
        {
            var clips = new List<AnimationClip>();
            var expressions = new Dictionary<ExpressionPreset, VRChatExpressionBinding>();
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

                expressions[preset] = new VRChatExpressionBinding
                {
                    RelativePath = "Body",
                    ShapeKeyNames = new[] { shapeKeyName }
                };
            }

            var controller = avatarDescriptor.baseAnimationLayers
                .FirstOrDefault(layer => layer.type == VRCAvatarDescriptor.AnimLayerType.FX)
                .animatorController;

            if (controller)
            {
                clips = controller.animationClips.ToList();
            }

            GetBlinkExpressions(gameObject, shapeKeyNamesList, expressions);
            GetOtherExpressions(expressions, selectedBlendShapes);

            return (clips, expressions);
        }

        internal static (IEnumerable<AnimationClip> clips, IDictionary<ExpressionPreset, VRM10Expression> expressions)
            GetExpressionsFromVRChatAvatar(GameObject gameObject, IEnumerable<string> shapeKeyNames,
                IDictionary<string, List<BlendShapeData>> selectedBlendShapes)
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

        private static void GetBlinkExpressions(GameObject gameObject,
            IEnumerable<string> shapeKeyNames,
            IDictionary<ExpressionPreset, VRChatExpressionBinding> expressions)
        {
            var shapeKeyNamesList = shapeKeyNames.ToList();
            var body = gameObject.transform.Find("Body");
            var dummyBlinkShapeKeyNamesList = new List<string>();

            if (body)
            {
                var renderer = body.GetComponent<SkinnedMeshRenderer>();
                if (renderer != null && renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount >= 4)
                {
                    dummyBlinkShapeKeyNamesList.Add(renderer.sharedMesh.GetBlendShapeName(0));
                    dummyBlinkShapeKeyNamesList.Add(renderer.sharedMesh.GetBlendShapeName(1));
                }
            }

            dummyBlinkShapeKeyNamesList.AddRange(shapeKeyNamesList.Where(
                shapeKeyName => OrderedBlinkGeneratedByCatsBlenderPlugin.Contains(shapeKeyName)));

            var settings = gameObject.GetComponent<VRCAvatarDescriptor>().customEyeLookSettings;
            if (settings.eyelidsSkinnedMesh != null
                && settings.eyelidsSkinnedMesh.sharedMesh != null
                && settings.eyelidsBlendshapes != null
                && settings.eyelidsBlendshapes.Count() == 3
                && settings.eyelidsSkinnedMesh.sharedMesh.blendShapeCount > settings.eyelidsBlendshapes[0])
            {
                expressions[ExpressionPreset.blink] = new VRChatExpressionBinding
                {
                    RelativePath = "Body",
                    ShapeKeyNames = new[]
                    {
                        settings.eyelidsSkinnedMesh.sharedMesh.GetBlendShapeName(settings.eyelidsBlendshapes[0])
                    }
                };
            }

            var blinkShapeKeys = shapeKeyNamesList.Where(shapeKeyName => shapeKeyName.ToLower().Contains("blink"))
                .ToList();
            if (blinkShapeKeys.Count() > 0)
            {
                if (expressions.ContainsKey(ExpressionPreset.blink)
                    && blinkShapeKeys.Contains(expressions[ExpressionPreset.blink].ShapeKeyNames.First()))
                {
                    // SDK3の両目まばたきが設定済みなら、それを取り除く
                    blinkShapeKeys.Remove(expressions[ExpressionPreset.blink].ShapeKeyNames.First());
                }

                foreach (var (preset, name) in new Dictionary<ExpressionPreset, string>
                         {
                             { ExpressionPreset.blinkLeft, "left" },
                             { ExpressionPreset.blinkRight, "right" },
                         })
                {
                    var blinkOneEyeShapeKeyNames = blinkShapeKeys.Where(shapeKeyName => Regex.IsMatch(
                        shapeKeyName,
                        $"(^|[^a-z])${Regex.Escape(name[0].ToString())}?([^a-z]|$)|{Regex.Escape(name)}",
                        RegexOptions.IgnoreCase
                    )).ToList();
                    if (blinkOneEyeShapeKeyNames.Any())
                    {
                        if (blinkOneEyeShapeKeyNames.Count() > 1)
                        {
                            var blinkOneEyeShapeKeyNamesList = blinkOneEyeShapeKeyNames.Except(dummyBlinkShapeKeyNamesList)
                                .ToList();
                            if (blinkOneEyeShapeKeyNamesList.Count() > 1)
                            {
                                blinkOneEyeShapeKeyNames = blinkOneEyeShapeKeyNamesList;
                            }
                        }
                        expressions[preset] = new VRChatExpressionBinding
                        {
                            RelativePath = "Body",
                            ShapeKeyNames = new[] { blinkOneEyeShapeKeyNames.First() }
                        };
                    }
                }

                if (!expressions.ContainsKey(ExpressionPreset.blink))
                {
                    var blinkBothEyesShapeKeyName = blinkShapeKeys.FirstOrDefault(shapeKeyName =>
                        !Regex.IsMatch(shapeKeyName, "(^|[^a-z])[lr]([^a-z]|$)|left|right", RegexOptions.IgnoreCase));
                    if (blinkBothEyesShapeKeyName != null)
                    {
                        expressions[ExpressionPreset.blink] = new VRChatExpressionBinding
                        {
                            ShapeKeyNames = new[] { blinkBothEyesShapeKeyName }
                        };
                    }
                    else
                    {
                        var blinkOneEyeShapeKeyNames = new[] { ExpressionPreset.blinkLeft, ExpressionPreset.blinkRight }
                            .Select(preset => expressions.ContainsKey(preset)
                                ? expressions[preset].ShapeKeyNames.First()
                                : null).ToList();
                        if (blinkOneEyeShapeKeyNames.Count() > 1)
                        {
                            expressions[ExpressionPreset.blink] = new VRChatExpressionBinding
                            {
                                ShapeKeyNames = blinkOneEyeShapeKeyNames.ToArray()
                            };
                        }
                    }
                }
            }
        }

        private static void GetOtherExpressions(IDictionary<ExpressionPreset, VRChatExpressionBinding> expressions,
            IDictionary<string, List<BlendShapeData>> selectedBlendShapes)
        {
            foreach (var selected in selectedBlendShapes)
            {
                var expressionPreset = ExpressionPresetCustomDict[selected.Key];
                var expression = new VRChatExpressionBinding
                {
                    ShapeKeyNames = selected.Value.Select(data => data.shapeKeyName).ToArray(),
                };

                expressions.Add(expressionPreset, expression);
            }
        }
    }
}
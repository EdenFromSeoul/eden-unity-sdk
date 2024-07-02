using System.Collections.Generic;
using System.Linq;
using Editor.Scripts.Util;
using Esperecyan.Unity.VRMConverterForVRChat.VRChatToVRM;
using Esperecyan.UniVRMExtensions.SwayingObjects;
using UnityEngine;
using UniVRM10;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRM;
using ExpressionPreset = UniVRM10.ExpressionPreset;

namespace Editor.Scripts.Manager
{
    public class ConvertManager
    {
        private static readonly IEnumerable<string> VRMSupportedShaderNames = new[]
        {
            "Standard",
            "Standard (Specular setup)",
            "Unlit/Color",
            "Unlit/Texture",
            "Unlit/Transparent",
            "Unlit/Transparent Cutout",
            "UniGLTF/NormalMapDecoder",
            "UniGLTF/NormalMapEncoder",
            "UniGLTF/StandardVColor",
            "UniGLTF/UniUnlit",
            "VRM/MToon",
            "VRM/UnlitCutout",
            "VRM/UnlitTexture",
            "VRM/UnlitTransparent",
            "VRM/UnlitTransparentZWrite",
            "VRM10/MToon10"
        };

        public static void Convert(
            string path,
            GameObject gameObject,
            VRM10ObjectMeta vrm10ObjectMeta,
            IDictionary<ExpressionPreset, VRChatExpressionBinding> expressionBindings)
        {
            // Convert the file
            ClearUnusedComponents(gameObject);

            // attach vrm instance to the game object
            var vrm10Instance = gameObject.AddComponent<Vrm10Instance>();
            vrm10Instance.Vrm = ScriptableObject.CreateInstance<VRM10Object>();
            vrm10Instance.Vrm.Meta = vrm10ObjectMeta;
            // vrm10Instance.Vrm.Expression = expressionBindings;
            // expressions 별로 vrc expression binding을 vrm expression으로 변환. 옆 80라인 참고바람.
            var shapeKeyNames = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>()
                .Select(renderer => renderer.sharedMesh)
                .Where(mesh => mesh)
                .SelectMany(mesh => SkinnedMesh.GetAllShapeKeys(mesh, false))
                .Select(shapeKey => shapeKey.Name)
                .Distinct();

            (var animations, var expressionsDict) = VRChat.GetExpressionsFromVRChatAvatar(gameObject, shapeKeyNames);

            // var expressions = new VRM10ObjectExpression();
            // var aa = ScriptableObject.CreateInstance<VRM10Expression>();
            //
            // aa.MorphTargetBindings = new[]
            //     { new MorphTargetBinding { RelativePath = "Body", Index = 0, Weight = 1.0f } };
            //
            // expressions.AddClip(ExpressionPreset.aa, aa);
            //
            // expressions.Ih = ScriptableObject.CreateInstance<VRM10Expression>();
            // expressions.Ih.MorphTargetBindings = new[]
            //     { new MorphTargetBinding { RelativePath = "Body", Index = 1, Weight = 1.0f } };

            foreach (var (preset, expression) in expressionsDict)
            {
                vrm10Instance.Vrm.Expression.AddClip(preset, expression);
            }

            var sourceAndDestination = gameObject.GetComponent<Animator>();

            if (sourceAndDestination.GetComponentsInChildren<VRCPhysBone>().Length > 0)
            {
                VRCPhysBonesToVRMSpringBonesConverter.Convert(sourceAndDestination, sourceAndDestination);
            }


            RemoveUnusedColliderGroups(gameObject);
        }

        private static void ClearUnusedComponents(GameObject gameObject)
        {
            // Clear unused components
            foreach (var transform in gameObject.transform.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (transform || transform.gameObject || transform.gameObject.activeSelf) continue;
                Object.DestroyImmediate(transform);
            }

            foreach (var component in gameObject.transform.GetComponentsInChildren<MonoBehaviour>())
            {
                if (component || component.enabled) continue;
                Object.DestroyImmediate(component);
            }
        }

        private static void RemoveUnusedColliderGroups(GameObject instance)
        {
            var animator = instance.GetComponent<Animator>();
            var hands = new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand }
                .Select(bone => animator.GetBoneTransform(bone).gameObject);

            var objectsHavingUsedColliderGroup = instance.GetComponentsInChildren<VRMSpringBone>()
                .SelectMany(springBone => springBone.ColliderGroups)
                .Select(colliderGroup => colliderGroup.gameObject)
                .ToArray();

            foreach (var colliderGroup in instance.GetComponentsInChildren<VRMSpringBoneColliderGroup>())
            {
                if (!objectsHavingUsedColliderGroup.Contains(colliderGroup.gameObject)
                    && !hands.Contains(colliderGroup.gameObject))
                {
                    Object.DestroyImmediate(colliderGroup);
                }
            }
        }
    }
}
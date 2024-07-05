using System.Collections.Generic;
using System.Linq;
using Editor.Scripts.Util;
using UnityEngine;
using UniVRM10;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRM;

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
            VRM10ObjectMeta vrm10ObjectMeta)
        {
            // Convert the file
            ClearUnusedComponents(gameObject);

            // attach vrm instance to the game object
            var vrm10Instance = gameObject.AddComponent<Vrm10Instance>();
            vrm10Instance.Vrm = ScriptableObject.CreateInstance<VRM10Object>();
            vrm10Instance.Vrm.Meta = vrm10ObjectMeta;
            var shapeKeyNames = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>()
                .Select(renderer => renderer.sharedMesh)
                .Where(mesh => mesh)
                .SelectMany(mesh => SkinnedMesh.GetAllShapeKeys(mesh, false))
                .Select(shapeKey => shapeKey.Name)
                .Distinct();
            vrm10Instance.SpringBone = new Vrm10InstanceSpringBone();

            (var animations, var expressionsDict) = VRChat.GetExpressionsFromVRChatAvatar(gameObject, shapeKeyNames);

            foreach (var (preset, expression) in expressionsDict)
            {
                vrm10Instance.Vrm.Expression.AddClip(preset, expression);
            }

            var sourceAndDestination = gameObject.GetComponent<Animator>();

            if (sourceAndDestination.GetComponentsInChildren<VRCPhysBone>().Length > 0)
            {
                VRCPhysBoneToVRM10SpringBonesConverter.Convert(sourceAndDestination, sourceAndDestination);
            }
            
            vrm10Instance.SpringBone.ColliderGroups = gameObject.GetComponentsInChildren<VRM10SpringBoneColliderGroup>().ToList();

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
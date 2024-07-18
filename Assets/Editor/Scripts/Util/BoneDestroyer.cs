using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using VRM;

namespace Editor.Scripts.Util
{
    public class BoneDestroyer
    {
        internal static void DestroyVRCPhysBones(GameObject instance, bool isAsset)
        {
#if VRC_SDK_VRCSDK3
            foreach (var component in instance.GetComponentsInChildren<VRCPhysBoneBase>(includeInactive: true)
                         .Cast<Component>()
                         .Concat(instance.GetComponentsInChildren<VRCPhysBoneColliderBase>(includeInactive: true)))
            {
                if (isAsset)
                {
                    Object.DestroyImmediate(component);
                }
                else
                {
                    Undo.DestroyObjectImmediate(component);
                }
            }
#endif
        }

        internal static void DestroyDynamicBones(GameObject instance, bool isAsset)
        {
            foreach (var component in instance.GetComponentsInChildren<Component>(includeInactive: true))
            {
                if (!new[] { "DynamicBone", "DynamicBoneCollider" }.Contains(component.GetType().FullName))
                {
                    continue;
                }

                if (isAsset)
                {
                    Object.DestroyImmediate(component);
                }
                else
                {
                    Undo.DestroyObjectImmediate(component);
                }
            }
        }

        internal static void DestroyVRMSpringBones(GameObject instance, bool isAsset)
        {
            foreach (var component in instance.GetComponentsInChildren<VRMSpringBone>(includeInactive: true)
                         .Cast<Component>()
                         .Concat(instance.GetComponentsInChildren<VRMSpringBoneColliderGroup>(includeInactive: true)))
            {
                if (isAsset)
                {
                    Object.DestroyImmediate(component);
                }
                else
                {
                    Undo.DestroyObjectImmediate(component);
                }
            }
        }
    }
}
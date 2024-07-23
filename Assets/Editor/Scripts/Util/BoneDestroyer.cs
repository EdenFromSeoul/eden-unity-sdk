using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Original Code: https://github.com/esperecyan/VRMConverterForVRChat
 * Initial Developer: esperecyan
 *
 * Alternatively, the contents of this file may be used under the terms
 * of the MIT license (the "MIT License"), in which case the provisions
 * of the MIT License are applicable instead of those above.
 * If you wish to allow use of your version of this file only under the
 * terms of the MIT License and not to allow others to use your version
 * of this file under the MPL, indicate your decision by deleting the
 * provisions above and replace them with the notice and other provisions
 * required by the MIT License. If you do not delete the provisions above,
 * a recipient may use your version of this file under either the MPL or
 * the MIT License.
 */
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
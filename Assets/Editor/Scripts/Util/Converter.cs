/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Original Code: https://github.com/esperecyan/VRMConverterForVRChat/blob/master/Editor/Utilities/SkinnedMeshUtility.cs
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
#nullable enable
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Editor.Scripts.Util
{
    public class Converter : IDisposable
    {
        internal readonly GameObject SourceGameObject;
        internal readonly GameObject TargetGameObject;
        internal readonly bool TargetIsAsset;
        internal readonly GameObject Secondary;

        private readonly Dictionary<HumanBodyBones, Transform> sourceSkeletonBones;
        private readonly string? targetAssetPath;
        private readonly int undoGroupIndex;

        private bool isDisposed = false;

        internal Converter(Animator sourceAnimator, Animator targetAnimator)
        {
            SourceGameObject = sourceAnimator.gameObject;
            TargetGameObject = targetAnimator.gameObject;
            TargetIsAsset = PrefabUtility.IsPartOfPrefabAsset(TargetGameObject);
            if (TargetIsAsset)
            {
                TargetGameObject = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(TargetGameObject));
                targetAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targetAnimator);
            }
            else
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName($"Convert Swaying objects from “{sourceAnimator.name}” to “{targetAnimator.name}”");
            }
            undoGroupIndex = Undo.GetCurrentGroup();
            
            var secondary = TargetGameObject.transform.Find("secondary");
            if (secondary == null)
            {
                Secondary = new GameObject("secondary");
                Secondary.transform.SetParent(TargetGameObject.transform);
                Secondary.transform.localPosition = Vector3.zero;
                if (!TargetIsAsset)
                {
                    Undo.RegisterCreatedObjectUndo(Secondary, "");
                }
            }
            else
            {
                Secondary = secondary.gameObject;
            }
        }

        internal Transform? FindCorrespondingBone(Transform sourceBone, string? target)
        {
            var targetBone = BoneMapper.FindCorrespondingBone(
                sourceBone,
                this.SourceGameObject,
                this.TargetGameObject,
                this.sourceSkeletonBones
            );
            
            if (targetBone == null && target != null)
            {
                Debug.LogWarning($"Failed to find corresponding bone for {sourceBone.name} in {target}");
            }
            
            return targetBone;
        }

        public void SaveAsset()
        {
            if (!TargetIsAsset)
            {
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(TargetGameObject, targetAssetPath);
        }

        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}
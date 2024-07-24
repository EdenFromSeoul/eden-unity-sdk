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
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UniGLTF;
using UnityEditor;
using UnityEngine;

namespace Editor.Scripts.Util
{
    internal class BoneMapper
    {
        /// <summary>
        /// すべてのスケルトンボーンを取得します。
        /// </summary>
        /// <param name="avatar"></param>
        /// <returns></returns>
        internal static Dictionary<HumanBodyBones, Transform> GetAllSkeletonBones(GameObject avatar)
        {
            var instance = avatar;
            var isAsset = PrefabUtility.IsPartOfPrefabAsset(avatar);
            if (isAsset)
            {
                instance = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(avatar));
            }

            var animator = instance.GetComponent<Animator>();
            var bones = Enum.GetValues(typeof(HumanBodyBones)).Cast<HumanBodyBones>()
                .Where(bone => bone != HumanBodyBones.LastBone)
                .Select(bone =>
                {
                    var transform = animator.GetBoneTransform(bone);
                    return (KeyValuePair<HumanBodyBones, Transform>?)(transform != null
                        ? new(bone, transform)
                        : null);
                })
                .OfType<KeyValuePair<HumanBodyBones, Transform>>() // nullを取り除く
                .ToDictionary(
                    boneTransformPair => boneTransformPair.Key,
                    boneTransformPair => boneTransformPair.Value
                );

            if (!isAsset)
            {
                return bones;
            }

            var bonePathPairs = bones.ToDictionary(
                boneTransformPair => boneTransformPair.Key,
                boneTransformPair => boneTransformPair.Value.RelativePathFrom(instance.transform)
            );

            PrefabUtility.UnloadPrefabContents(instance);

            return bonePathPairs.ToDictionary(
                bonePathPair => bonePathPair.Key,
                bonePathPair => avatar.transform.Find(bonePathPair.Value)
            );
        }

        /// <summary>
        /// コピー元のアバターの指定ボーンと対応する、コピー先のアバターのボーンを返します。
        /// </summary>
        /// <param name="sourceBone"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="sourceSkeletonBones"></param>
        /// <returns>見つからなかった場合は <c>null</c> を返します。</returns>
        internal static Transform? FindCorrespondingBone(
            Transform sourceBone,
            GameObject source,
            GameObject target,
            Dictionary<HumanBodyBones, Transform> sourceSkeletonBones
        )
        {
            if (!sourceBone.IsChildOf(source.transform))
            {
                return null;
            }

            var sourceBoneRelativePath = sourceBone.RelativePathFrom(root: source.transform);
            var targetBone = target.transform.Find(sourceBoneRelativePath);
            if (targetBone)
            {
                return targetBone;
            }

            if (!sourceBone.IsChildOf(sourceSkeletonBones[HumanBodyBones.Hips]))
            {
                return null;
            }

            var humanoidAndSkeletonBone = ClosestSkeletonBone(sourceBone, sourceSkeletonBones);
            var targetAnimator = target.GetComponent<Animator>();
            var targetSkeletonBone = targetAnimator.GetBoneTransform(humanoidAndSkeletonBone.Key);
            if (!targetSkeletonBone)
            {
                return null;
            }

            targetBone = targetSkeletonBone.Find(sourceBone.RelativePathFrom(humanoidAndSkeletonBone.Value));
            if (targetBone)
            {
                return targetBone;
            }

            return targetSkeletonBone.GetComponentsInChildren<Transform>()
                .FirstOrDefault(bone => bone.name == sourceBone.name);
        }

        /// <summary>
        /// 祖先方向へたどり、指定されたボーンを含む直近のスケルトンボーンを取得します。
        /// </summary>
        /// <param name="bone"></param>
        /// <param name="avatar"></param>
        /// <param name="skeletonBones"></param>
        /// <returns></returns>
        private static KeyValuePair<HumanBodyBones, Transform> ClosestSkeletonBone(
            Transform bone,
            Dictionary<HumanBodyBones, Transform> skeletonBones
        )
        {
            foreach (var parent in bone.Ancestors())
            {
                if (!skeletonBones.ContainsValue(parent))
                {
                    continue;
                }

                return skeletonBones.FirstOrDefault(humanoidAndSkeletonBone => humanoidAndSkeletonBone.Value == parent);
            }

            throw new ArgumentException();
        }
    }
}
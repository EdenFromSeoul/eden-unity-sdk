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
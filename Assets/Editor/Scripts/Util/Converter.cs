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
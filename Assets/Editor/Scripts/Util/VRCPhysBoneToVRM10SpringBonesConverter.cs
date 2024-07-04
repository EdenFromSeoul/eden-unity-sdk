#nullable enable
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UniVRM10;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace Editor.Scripts.Util
{
    public class VRCPhysBoneToVRM10SpringBonesConverter
    {
        public delegate VRM10SpringBoneParameters ParametersConverter(VRCPhysBoneParameters vrcPhysBoneParameters);

        private static VRM10SpringBoneParameters DefaultParametersConverter(VRCPhysBoneParameters vrcPhysBoneParameters)
        {
            return new VRM10SpringBoneParameters
            {
                StiffnessForce = vrcPhysBoneParameters.Stiffness,
                GravityPower = vrcPhysBoneParameters.Gravity,
                GravityDir = new Vector3(0, -1.0f, 0),
                DragForce = 0.4f,
                JointRadius = 0.02f
            };
        }

        public static void Convert(
            Animator sourceAnimator,
            Animator targetAnimator,
            bool ignoreColliders = false,
            ParametersConverter? parametersConverter = null)
        {
            if (parametersConverter == null)
            {
                parametersConverter = DefaultParametersConverter;
            }

            using (var converter = new Converter(sourceAnimator, targetAnimator))
            {
                if (!ignoreColliders)
                {
                    SetSpringBoneColliderGroups(converter);
                }

                SetSpringBones(converter, parametersConverter, ignoreColliders);

                converter.SaveAsset();
            }
        }

        private static void SetSpringBoneColliderGroups(Converter converter)
        {
            foreach (var sourceColliders in converter.SourceGameObject.GetComponentsInChildren<VRCPhysBoneCollider>()
                         .GroupBy(collider => collider.transform))
            {
                var targetBone = converter.FindCorrespondingBone(
                    sourceColliders.Key,
                    converter.TargetGameObject.name
                );
                if (targetBone == null)
                {
                    continue;
                }

                var targetColliders = sourceColliders.Select(sourceCollider =>
                    ConvertCollider(sourceCollider, targetBone)
                );
                
                var targetColliderGroup = targetBone.GetComponent<VRM10SpringBoneColliderGroup>();
                if (targetColliderGroup != null)
                {
                    if (!converter.TargetIsAsset)
                    {
                        Undo.RecordObject(targetColliderGroup, "");
                    }

                    targetColliderGroup.Colliders = targetColliders.ToList();
                }
                else
                {
                    (converter.TargetIsAsset ? targetBone.gameObject.AddComponent<VRM10SpringBoneColliderGroup>()
                        : Undo.AddComponent<VRM10SpringBoneColliderGroup>(targetBone.gameObject)).Colliders =
                        targetColliders.ToList();
                    
                }
                
            }
        }

        private static VRM10SpringBoneCollider ConvertCollider(
            VRCPhysBoneCollider sourceCollider,
            Transform targetBone
        )
        {
            var targetCollider = targetBone.gameObject.AddComponent<VRM10SpringBoneCollider>();

            targetCollider.ColliderType = sourceCollider.shapeType switch
            {
                VRCPhysBoneColliderBase.ShapeType.Sphere => VRM10SpringBoneColliderTypes.Sphere,
                VRCPhysBoneColliderBase.ShapeType.Capsule => VRM10SpringBoneColliderTypes.Capsule,
                VRCPhysBoneColliderBase.ShapeType.Plane => VRM10SpringBoneColliderTypes.Plane,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            targetCollider.Offset = sourceCollider.position;
            targetCollider.Radius = sourceCollider.radius;

            switch (sourceCollider.shapeType)
            {
                case VRCPhysBoneColliderBase.ShapeType.Capsule:
                {
                    var capsuleTail = sourceCollider.position + sourceCollider.rotation * Vector3.up * sourceCollider.height;
                    targetCollider.Tail = capsuleTail;
                    break;
                }
                case VRCPhysBoneColliderBase.ShapeType.Plane:
                    targetCollider.Normal = sourceCollider.axis;
                    break;
            }
            
            return targetCollider;
        }

        private static void SetSpringBones(
            Converter converter,
            ParametersConverter parametersConverter,
            bool ignoreColliders
        )
        {
            // TODO implement this method
        }
    }
}
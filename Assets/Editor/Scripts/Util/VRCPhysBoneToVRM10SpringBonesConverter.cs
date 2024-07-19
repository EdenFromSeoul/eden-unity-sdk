#nullable enable
using System;
using System.Collections.Generic;
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
            var stiffness = vrcPhysBoneParameters.Spring * 2.0f;
            var drag = vrcPhysBoneParameters.Stiffness * 4.0f;
            var gravity = 0.0f;

            if (vrcPhysBoneParameters.Gravity != 0)
            {
                stiffness = (vrcPhysBoneParameters.Spring / (vrcPhysBoneParameters.Gravity + 1)) * 2.0f;
                gravity = vrcPhysBoneParameters.Gravity * vrcPhysBoneParameters.Pull;
            }

            return new VRM10SpringBoneParameters
            {
                StiffnessForce = stiffness,
                GravityPower = Math.Abs(gravity),
                // 중력 방향은 Gravity 값의 부호에 따라 달라짐 (Gravity가 음수면 위 방향, 양수면 아래 방향)
                GravityDir = new Vector3(0, gravity >= 0 ? -1.0f : 1.0f, 0),
                DragForce = drag,
                JointRadius = vrcPhysBoneParameters.Radius * 0.8f,
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
                    (converter.TargetIsAsset
                            ? targetBone.gameObject.AddComponent<VRM10SpringBoneColliderGroup>()
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

            switch (sourceCollider.shapeType)
            {
                case VRCPhysBoneColliderBase.ShapeType.Sphere:
                    // if (sourceCollider.insideBounds)
                    // {
                    //     targetCollider.ColliderType = VRM10SpringBoneColliderTypes.SphereInside;
                    // }
                    // else
                    // {
                    //     targetCollider.ColliderType = VRM10SpringBoneColliderTypes.Sphere;
                    // }

                    targetCollider.ColliderType = VRM10SpringBoneColliderTypes.Sphere;

                    break;
                case VRCPhysBoneColliderBase.ShapeType.Capsule:
                    // if (sourceCollider.insideBounds)
                    // {
                    //     targetCollider.ColliderType = VRM10SpringBoneColliderTypes.CapsuleInside;
                    // }
                    // else
                    // {
                    //     targetCollider.ColliderType = VRM10SpringBoneColliderTypes.Capsule;
                    // }

                    targetCollider.ColliderType = VRM10SpringBoneColliderTypes.Capsule;

                    break;
                case VRCPhysBoneColliderBase.ShapeType.Plane:
                    // targetCollider.ColliderType = VRM10SpringBoneColliderTypes.Plane;

                    targetCollider.ColliderType = VRM10SpringBoneColliderTypes.Sphere;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            targetCollider.Offset = sourceCollider.position / 2.0f;
            targetCollider.Radius = sourceCollider.radius * 0.5f;

            switch (sourceCollider.shapeType)
            {
                case VRCPhysBoneColliderBase.ShapeType.Capsule:
                {
                    targetCollider.Offset = (sourceCollider.position - sourceCollider.rotation * Vector3.up * sourceCollider.height / 2.0f) / 2.0f;
                    // tail은 포지션에서 height / 2 를 더한 값
                    var capsuleTail = (sourceCollider.position + sourceCollider.rotation * Vector3.up * sourceCollider.height / 2.0f) / 2.0f;
                    targetCollider.Tail = capsuleTail;
                    break;
                }
                case VRCPhysBoneColliderBase.ShapeType.Plane:
                    // targetCollider.Normal = sourceCollider.axis;
                    break;
                case VRCPhysBoneColliderBase.ShapeType.Sphere:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return targetCollider;
        }

        private static void SetSpringBones(
            Converter converter,
            ParametersConverter parametersConverter,
            bool ignoreColliders
        )
        {
            var targetAnimator = converter.TargetGameObject.GetComponent<Animator>();
            var targetHandColliderGroups = new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand }
                .Select(bone => targetAnimator.GetBoneTransform(bone))
                .Select(hand => hand.GetComponent<VRM10SpringBoneColliderGroup>())
                .Where(group => group != null);
            // target animator의 parent로부터 VRM10Instance를 찾아서 거기에 VRM10SpringBone을 추가해야함
            var targetInstance = targetAnimator.transform.GetComponentInParent<Vrm10Instance>();
            if (targetInstance == null)
            {
                Debug.LogError("VRM10Instance not found");
                return;
            }

            targetInstance.SpringBone = new Vrm10InstanceSpringBone();

            foreach (var vrcPhysBones in converter.SourceGameObject.GetComponentsInChildren<VRCPhysBone>()
                         .Select(vrcPhysBone =>
                         {
                             var parameters = parametersConverter(new VRCPhysBoneParameters
                             {
#if VRC_SDK_VRCSDK3
                                 Version = vrcPhysBone.version,
#endif
                                 Pull = vrcPhysBone.pull,
                                 PullCurve = vrcPhysBone.pullCurve,
                                 Spring = vrcPhysBone.spring,
                                 SpringCurve = vrcPhysBone.springCurve,
                                 Stiffness = vrcPhysBone.stiffness,
                                 StiffnessCurve = vrcPhysBone.stiffnessCurve,
                                 Gravity = vrcPhysBone.gravity,
                                 GravityFalloff = vrcPhysBone.gravityFalloff,
                                 GravityFalloffCurve = vrcPhysBone.gravityFalloffCurve,
                                 Radius = vrcPhysBone.radius,
#if VRC_SDK_VRCSDK3
                                 ImmobileType = vrcPhysBone.immobileType,
#endif
                                 Immobile = vrcPhysBone.immobile,
                                 ImmobileCurve = vrcPhysBone.immobileCurve,
                                 GrabMovement = vrcPhysBone.grabMovement,
                                 MaxStretch = vrcPhysBone.maxStretch,
                                 MaxStretchCurve = vrcPhysBone.maxStretchCurve,
                             });

                             var targetColliderGroups = new List<VRM10SpringBoneColliderGroup>();

                             if (vrcPhysBone.colliders != null)
                             {
                                 foreach (var sourceCollider in vrcPhysBone.colliders)
                                 {
                                     if (sourceCollider == null)
                                     {
                                         continue;
                                     }

                                     if (!sourceCollider.transform.IsChildOf(converter.SourceGameObject.transform))
                                     {
                                         continue;
                                     }

                                     var targetBone = converter.FindCorrespondingBone(
                                         sourceCollider.transform,
                                         converter.TargetGameObject.name
                                     );

                                     if (targetBone == null)
                                     {
                                         continue;
                                     }

                                     var targetColliderGroup = targetBone.GetComponent<VRM10SpringBoneColliderGroup>();

                                     if (targetColliderGroup == null ||
                                         targetColliderGroups.Contains(targetColliderGroup))
                                     {
                                         continue;
                                     }

                                     targetColliderGroups.Add(targetColliderGroup);
                                     Debug.Log($"Added collider group to {targetColliderGroup.gameObject.name}");
                                 }
                             }

                             if (!ignoreColliders &&
                                 (vrcPhysBone.allowCollision != VRCPhysBoneBase.AdvancedBool.False ||
                                  vrcPhysBone.allowGrabbing != VRCPhysBoneBase.AdvancedBool.False))
                             {
                                 foreach (var handColliderGroup in targetHandColliderGroups)
                                 {
                                     if (!targetColliderGroups.Contains(handColliderGroup))
                                     {
                                         targetColliderGroups.Add(handColliderGroup);
                                     }
                                 }
                             }

                             return (vrcPhysBone, parameters, targetColliderGroups, compare: string.Join("\n", new[]
                                 {
                                     parameters.StiffnessForce,
                                     parameters.GravityPower,
                                     parameters.DragForce,
                                     BoneTransformUtility.CalculateDistance(vrcPhysBone.transform, vrcPhysBone.radius),
                                 }.Select(value => value.ToString("F2"))
                                 .Concat(targetColliderGroups.Select(group =>
                                     group.transform.RelativePathFrom(converter.TargetGameObject.transform) +
                                     vrcPhysBone.parameter))
                             ));
                         })
                         .GroupBy(tuple => tuple.compare))
            {
                foreach (var vrcPhysBone in vrcPhysBones)
                {
                    // joint는 VRCPhysBone이 있는 위치에 추가해야함
                    var newJoint = vrcPhysBone.vrcPhysBone.gameObject.AddComponent<VRM10SpringBoneJoint>();

                    newJoint.m_stiffnessForce = vrcPhysBone.parameters.StiffnessForce;
                    newJoint.m_gravityPower = vrcPhysBone.parameters.GravityPower;
                    newJoint.m_gravityDir = vrcPhysBone.parameters.GravityDir;
                    newJoint.m_dragForce = vrcPhysBone.parameters.DragForce;
                    newJoint.m_jointRadius = vrcPhysBone.parameters.JointRadius;
                    // transform name으로 검색 없으면 spring 생성
                    var spring =
                        targetInstance.SpringBone.Springs.Find(spring =>
                            spring.Name == vrcPhysBone.vrcPhysBone.transform.name);


                    if (spring == null)
                    {
                        spring = new Vrm10InstanceSpringBone.Spring(vrcPhysBone.vrcPhysBone.transform.name);
                        targetInstance.SpringBone.Springs.Add(spring);
                    }

                    spring.ColliderGroups.AddRange(vrcPhysBone.targetColliderGroups);

                    AddJointRecursive(vrcPhysBone.vrcPhysBone.transform, newJoint, spring);
                }
            }
        }

        private static void AddJointRecursive(Transform transform, VRM10SpringBoneJoint sourceJoint,
            Vrm10InstanceSpringBone.Spring spring)
        {
            var joint = transform.gameObject.GetComponent<VRM10SpringBoneJoint>();

            if (joint == null)
            {
                joint = transform.gameObject.AddComponent<VRM10SpringBoneJoint>();
                Debug.Log($"Added joint to {transform.gameObject.name}");
            }
            else
            {
                Debug.Log($"Joint already exists in {transform.gameObject.name}");
            }

            joint.m_stiffnessForce = sourceJoint.m_stiffnessForce;
            joint.m_gravityPower = sourceJoint.m_gravityPower;
            joint.m_gravityDir = sourceJoint.m_gravityDir;
            joint.m_dragForce = sourceJoint.m_dragForce;
            joint.m_jointRadius = sourceJoint.m_jointRadius;

            spring.Joints.Add(joint);

            if (transform.childCount > 0)
            {
                foreach (Transform child in transform)
                {
                    AddJointRecursive(child, sourceJoint, spring);
                }
            }
        }
    }
}
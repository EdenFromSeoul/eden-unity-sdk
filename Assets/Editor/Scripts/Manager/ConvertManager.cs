using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editor.Scripts.Struct;
using Editor.Scripts.Util;
using Esperecyan.UniVRMExtensions;
using UniGLTF;
using UniGLTF.MeshUtility;
using UnityEditor;
using UnityEngine;
using UniVRM10;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDKBase;
using VRM;
using Object = UnityEngine.Object;

namespace Editor.Scripts.Manager
{
    public class ConvertManager
    {
        private static readonly string TempPath = "Assets/Eden/Temp";
        private static readonly string TempPrefabName = "temp.prefab";

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
            IDictionary<string, List<BlendShapeData>> selectedBlendShapes)
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

            (var animations, var expressionsDict) =
                VRChat.GetExpressionsFromVRChatAvatar(gameObject, shapeKeyNames, selectedBlendShapes);

            foreach (var (preset, expression) in expressionsDict)
            {
                vrm10Instance.Vrm.Expression.AddClip(preset, expression);
            }

            var sourceAndDestination = gameObject.GetComponent<Animator>();

            if (sourceAndDestination.GetComponentsInChildren<VRCPhysBone>().Length > 0)
            {
                VRCPhysBoneToVRM10SpringBonesConverter.Convert(sourceAndDestination, sourceAndDestination);
            }

            vrm10Instance.SpringBone.ColliderGroups =
                gameObject.GetComponentsInChildren<VRM10SpringBoneColliderGroup>().ToList();

            RemoveUnusedColliderGroups(gameObject);
        }

        public static void ConvertVrm0(
            string path,
            GameObject gameObject,
            VRMMetaObject vrmMetaObject,
            IDictionary<string, List<BlendShapeData>> selectedBlendShapes)
        {
            try
            {
                var originalName = gameObject.name;
                var shapeKeyNames = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>()
                    .Select(renderer => renderer.sharedMesh)
                    .Where(mesh => mesh != null)
                    .SelectMany(mesh => SkinnedMesh.GetAllShapeKeys(mesh, false))
                    .Select(shapeKey => shapeKey.Name)
                    .Distinct();

                // Convert the file
                ClearUnusedComponents(gameObject);

                var tempFolder = UnityPath.FromUnityPath(TempPath);
                tempFolder.EnsureFolder();
                var tempPrefabPath = tempFolder.Child(TempPrefabName).Value;
                VRMInitializer.Initialize(tempPrefabPath, gameObject);
                Object.DestroyImmediate(gameObject.GetComponentInChildren<VRMSpringBone>());
                SetFirstPersonOffset(gameObject);
                SetLookAtBoneApplyer(gameObject);

                var sourceAndDestination = gameObject.GetComponent<Animator>();
                if (sourceAndDestination.GetComponentsInChildren<VRCPhysBone>().Length > 0)
                {
                    VRCPhysBonesToVRMSpringBonesConverter.Convert(sourceAndDestination, sourceAndDestination);
                }

                RemoveUnusedColliderGroups(gameObject);

                VRMBoneNormalizer.Execute(gameObject, true);

                // 全メッシュ結合
                var combinedRenderer = CombineMeshesAndSubMeshes.Combine(
                    gameObject,
                    notCombineRendererObjectNames: new List<string>(),
                    destinationObjectName: "vrm-mesh",
                    savingAsAsset: false
                );

                TabMeshSeparator.SeparationProcessing(gameObject);
                gameObject.name = originalName;
                var animator = gameObject.GetComponent<Animator>();
                animator.avatar = Duplicator.CreateObjectToFolder(animator.avatar, tempPrefabPath);
                vrmMetaObject.name = "Meta";
                gameObject.GetComponent<VRMMeta>().Meta =
                    Duplicator.CreateObjectToFolder(vrmMetaObject, tempPrefabPath);
                foreach (var renderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    renderer.sharedMesh.name = renderer.name;
                    renderer.sharedMesh = Duplicator.CreateObjectToFolder(renderer.sharedMesh, tempPrefabPath);
                }

                SetFirstPersonRenderers(gameObject);


                (var animations, var expressionsDict) =
                    VRChat.GetExpressionsFromVRChatAvatar(gameObject, shapeKeyNames, selectedBlendShapes);

                foreach (var (preset, expression) in expressionsDict)
                {
                    // convert vrm10 preset to vrm0 preset
                    var vrm0Preset = preset switch
                    {
                        ExpressionPreset.custom => BlendShapePreset.Unknown,
                        ExpressionPreset.happy => BlendShapePreset.Joy,
                        ExpressionPreset.angry => BlendShapePreset.Angry,
                        ExpressionPreset.sad => BlendShapePreset.Sorrow,
                        ExpressionPreset.relaxed => BlendShapePreset.Fun,
                        ExpressionPreset.aa => BlendShapePreset.A,
                        ExpressionPreset.ih => BlendShapePreset.I,
                        ExpressionPreset.ou => BlendShapePreset.U,
                        ExpressionPreset.ee => BlendShapePreset.E,
                        ExpressionPreset.oh => BlendShapePreset.O,
                        ExpressionPreset.blink => BlendShapePreset.Blink,
                        ExpressionPreset.blinkLeft => BlendShapePreset.Blink_L,
                        ExpressionPreset.blinkRight => BlendShapePreset.Blink_R,
                        ExpressionPreset.lookUp => BlendShapePreset.LookUp,
                        ExpressionPreset.lookDown => BlendShapePreset.LookDown,
                        ExpressionPreset.lookLeft => BlendShapePreset.LookLeft,
                        ExpressionPreset.lookRight => BlendShapePreset.LookRight,
                        ExpressionPreset.neutral => BlendShapePreset.Neutral,
                        _ => BlendShapePreset.Unknown
                    };

                    // convert vrm10 expression to vrm0 expression
                    gameObject.GetComponent<VRMBlendShapeProxy>().BlendShapeAvatar.GetClip(vrm0Preset).Values =
                        expression.MorphTargetBindings.Select(binding => new BlendShapeBinding
                                { RelativePath = binding.RelativePath, Index = binding.Index, Weight = binding.Weight })
                            .ToArray();
                }

                var prefab =
                    PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, tempPrefabPath,
                        InteractionMode.AutomatedAction);
                AssetDatabase.SaveAssets();
                File.WriteAllBytes(path,
                    VRMEditorExporter.Export(prefab, meta: null, ScriptableObject.CreateInstance<VRMExportSettings>())
                );
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", e.Message, "OK");
                throw;
            }
            finally
            {
                if (gameObject)
                {
                    Object.DestroyImmediate(gameObject);
                }

                AssetDatabase.DeleteAsset("Assets/Eden/Temp");
            }
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

        private static void SetFirstPersonOffset(GameObject instance)
        {
            var avatarDescriptor = instance.GetComponent<VRC_AvatarDescriptor>();
            var firstPerson = instance.GetComponent<VRMFirstPerson>();
            firstPerson.FirstPersonOffset = avatarDescriptor.ViewPosition - firstPerson.FirstPersonBone.position;
        }

        private static void SetFirstPersonRenderers(GameObject instance)
        {
            instance.GetComponent<VRMFirstPerson>().TraverseRenderers();
        }

        private static void SetLookAtBoneApplyer(GameObject instance)
        {
            var lookAtBoneApplyer = instance.GetComponent<VRMLookAtBoneApplyer>();

            var settings = instance.GetComponent<VRCAvatarDescriptor>().customEyeLookSettings;
            if (settings.eyesLookingUp != null && settings.eyesLookingDown != null
                                               && settings.eyesLookingLeft != null && settings.eyesLookingRight != null)
            {
                lookAtBoneApplyer.VerticalUp.CurveYRangeDegree
                    = Math.Min(-settings.eyesLookingUp.left.x, -settings.eyesLookingUp.right.x);
                lookAtBoneApplyer.VerticalDown.CurveYRangeDegree
                    = Math.Min(settings.eyesLookingDown.left.x, settings.eyesLookingDown.right.x);
                lookAtBoneApplyer.HorizontalOuter.CurveYRangeDegree
                    = Math.Min(-settings.eyesLookingLeft.left.y, settings.eyesLookingRight.right.y);
                lookAtBoneApplyer.HorizontalInner.CurveYRangeDegree
                    = Math.Min(-settings.eyesLookingLeft.right.y, settings.eyesLookingRight.left.y);
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
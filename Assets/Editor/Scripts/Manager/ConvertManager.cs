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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editor.Scripts.Struct;
using Editor.Scripts.Util;
using lilToon;
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
using VRMInitializer = Editor.Scripts.Util.VRMInitializer;

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

            UnityPath.FromUnityPath(TempPath).EnsureFolder();
            ChangeMaterials(gameObject, TempPath);
        }

        public static void ConvertVrm0(
            string path,
            GameObject gameObject,
            VRMMetaObject vrmMetaObject,
            IDictionary<string, List<BlendShapeData>> selectedBlendShapes,
            bool removeUnusedBlendShapes = true
            )
        {
            try
            {
                if (gameObject == null)
                {
                    throw new ArgumentNullException(nameof(gameObject), "GameObject cannot be null.");
                }

                if (vrmMetaObject == null)
                {
                    throw new ArgumentNullException(nameof(vrmMetaObject), "VRMMetaObject cannot be null.");
                }

                var originalName = gameObject.name;

                // Debugging Step 1: Get all SkinnedMeshRenderers
                var skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                Debug.Log($"Number of SkinnedMeshRenderers: {skinnedMeshRenderers.Length}");

                // Debugging Step 2: Get all sharedMeshes
                var sharedMeshes = skinnedMeshRenderers
                    .Select(renderer => renderer.sharedMesh)
                    .Where(mesh => mesh != null)
                    .ToList();
                Debug.Log($"Number of non-null sharedMeshes: {sharedMeshes.Count}");

                // weight가 0이 아닌 shape key만 가져오기
                var usedShapeKeys = new List<string>();
                foreach (var renderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mesh = renderer.sharedMesh;
                    for (int i = 0; i < mesh.blendShapeCount; i++)
                    {
                        if (renderer.GetBlendShapeWeight(i) > 0)
                        {
                            usedShapeKeys.Add(mesh.GetBlendShapeName(i));
                        }
                    }
                }


                // Debugging Step 3: Get all shape keys
                var shapeKeyNames = sharedMeshes
                    .SelectMany(mesh => SkinnedMesh.GetAllShapeKeys(mesh, false))
                    .Where(shapeKey => shapeKey != null) // 추가된 null 체크
                    .Select(shapeKey => shapeKey.Name)
                    .Distinct()
                    .ToList();
                Debug.Log($"Number of distinct shapeKeyNames: {shapeKeyNames.Count}");


                var (_, expressionsDict) =
                    VRChat.GetExpressions(gameObject, shapeKeyNames, selectedBlendShapes);

                // Convert the file
                ClearUnusedComponents(gameObject);

                Debug.Log("Cleared unused components.");

                var necessaryShapeKeys = new List<string>();
                foreach (var (_, expressionBinding) in expressionsDict)
                {
                    foreach (var names in expressionBinding.ShapeKeyNames)
                    {
                        Debug.Log($"Shape key: {names}");
                    }
                    necessaryShapeKeys.AddRange(expressionBinding.ShapeKeyNames.ToList());
                }

                // used이지만 necessary에 없는 shape key 추가
                foreach (var shapeKey in usedShapeKeys)
                {
                    if (!necessaryShapeKeys.Contains(shapeKey))
                    {
                        necessaryShapeKeys.Add(shapeKey);
                    }
                }

                necessaryShapeKeys = necessaryShapeKeys.Distinct().ToList();

                var tempFolder = UnityPath.FromUnityPath(TempPath);
                tempFolder.EnsureFolder();
                var tempPrefabPath = tempFolder.Child(TempPrefabName).Value;

                Debug.Log($"Temp prefab path: {tempPrefabPath}");

                // file 있으면 제거
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                VRMInitializer.Initialize(tempPrefabPath, gameObject);
                Object.DestroyImmediate(gameObject.GetComponentInChildren<VRMSpringBone>());
                SetFirstPersonOffset(gameObject);
                SetLookAtBoneApplyer(gameObject);

                Debug.Log("Set first person offset and look at bone applyer.");

                var sourceAndDestination = gameObject.GetComponent<Animator>();
                if (sourceAndDestination == null)
                {
                    throw new NullReferenceException("Animator component is missing on the GameObject.");
                }

                if (sourceAndDestination.GetComponentsInChildren<VRCPhysBone>().Length > 0)
                {
                    VRCPhysBonesToVRMSpringBonesConverter.Convert(sourceAndDestination, sourceAndDestination);
                }

                Debug.Log("Converted VRCPhysBones to VRMSpringBones.");

                RemoveUnusedColliderGroups(gameObject);

                VRMBoneNormalizer.Execute(gameObject, true);

                // 全メッシュ結合
                // var combinedRenderer = CombineMeshesAndSubMeshes.Combine(
                //     gameObject,
                //     notCombineRendererObjectNames: new List<string>(),
                //     destinationObjectName: "vrm-mesh",
                //     savingAsAsset: false
                // );

                // 모든 skinned mesh renderer의 shared mesh에서 clean up
                if (removeUnusedBlendShapes)
                {
                    foreach (var renderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        SkinnedMesh.CleanUpShapeKeysVrm0(renderer.sharedMesh, necessaryShapeKeys);
                    }
                    // SkinnedMesh.CleanUpShapeKeysVrm0(combinedRenderer.sharedMesh, necessaryShapeKeys);
                }

                TabMeshSeparator.SeparationProcessing(gameObject);

                ChangeMaterials(gameObject, TempPath);

                Debug.Log("Separated meshes.");
                gameObject.name = originalName;
                var animator = gameObject.GetComponent<Animator>();
                if (animator.avatar == null)
                {
                    throw new NullReferenceException("Animator avatar is missing.");
                }

                animator.avatar = Duplicator.CreateObjectToFolder(animator.avatar, tempPrefabPath);
                vrmMetaObject.name = "Meta";
                var vrmMeta = gameObject.GetComponent<VRMMeta>();
                if (vrmMeta == null)
                {
                    throw new NullReferenceException("VRMMeta component is missing.");
                }

                vrmMeta.Meta = Duplicator.CreateObjectToFolder(vrmMetaObject, tempPrefabPath);
                foreach (var renderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    renderer.sharedMesh.name = renderer.name;
                    renderer.sharedMesh = Duplicator.CreateObjectToFolder(renderer.sharedMesh, tempPrefabPath);
                }

                SetFirstPersonRenderers(gameObject);

                Debug.Log("Set first person renderers.");

                // var (_, expressionsDict) =
                //     VRChat.GetExpressionsFromVRChatAvatar(gameObject, shapeKeyNames, selectedBlendShapes);

                var blendShapeProxy = gameObject.GetComponent<VRMBlendShapeProxy>();
                if (blendShapeProxy == null)
                {
                    throw new NullReferenceException("VRMBlendShapeProxy component is missing.");
                }
                var blendShapeAvatar = blendShapeProxy.BlendShapeAvatar;

                foreach (var (preset, expression) in expressionsDict)
                {
                    // convert vrm10 expression to vrm0 expression
                    var blendShapeClip = GetExpression(blendShapeAvatar, preset);
                    if (blendShapeClip == null)
                    {
                        throw new NullReferenceException($"BlendShapeClip for preset {preset} is missing.");
                    }

                    var bindingList = new List<BlendShapeBinding>();

                    foreach (var names in expression.ShapeKeyNames)
                    {
                        var index = necessaryShapeKeys.IndexOf(names);
                        if (index == -1)
                        {
                            Debug.LogWarning($"Shape key {names} is missing.");
                            continue;
                            // throw new NullReferenceException($"Shape key {names} is missing.");
                        }

                        Debug.Log($"{names} : {index} : {expression.RelativePath}");

                        bindingList.Add(new BlendShapeBinding
                        {
                            // RelativePath = "vrm-mesh",
                            RelativePath = expression.RelativePath ?? "Body",
                            Index = index,
                            Weight = 100
                        });
                    }

                    blendShapeClip.Values = bindingList.ToArray();
                }

                Debug.Log("Set expressions.");

                var prefab =
                    PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, tempPrefabPath,
                        InteractionMode.AutomatedAction);
                AssetDatabase.SaveAssets();

                Debug.Log("Saved prefab.");
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


        private static void ReplaceShaders(GameObject instance, string temporaryPrefabPath)
        {
            var alreadyDuplicatedMaterials = new Dictionary<Material, Material>();

            foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(material =>
                {
                    if (VRMSupportedShaderNames.Contains(material.shader.name))
                    {
                        return material;
                    }

                    if (alreadyDuplicatedMaterials.ContainsKey(material))
                    {
                        return alreadyDuplicatedMaterials[material];
                    }

                    var newMaterial = Object.Instantiate(material);
                    newMaterial.name = material.name;

                    var shaderName = material.shader.name.ToLower();
                    if (shaderName.Contains("unlit"))
                    {
                        newMaterial.shader = Shader.Find("UniGLTF/UniUnlit");
                    }
                    else if (shaderName.Contains("toon"))
                    {
                        newMaterial.shader = Shader.Find("VRM/MToon");
                    }
                    newMaterial.renderQueue = material.renderQueue;

                    return alreadyDuplicatedMaterials[material]
                        = Duplicator.CreateObjectToFolder(newMaterial, temporaryPrefabPath);
                }).ToArray();
            }
        }

        private static void ChangeMaterials(GameObject prefab, string savePath)
        {
            //1. prefab 하위 오브젝트들을 활성화시킨다.
            List<GameObject> list = new List<GameObject>();
            Stack<Transform> stack = new Stack<Transform>();
            stack.Push(prefab.transform);

            while (stack.Count > 0)
            {
                Transform current = stack.Pop();

                foreach (Transform child in current)
                {
                    if (!child.gameObject.activeSelf)
                    {
                        child.gameObject.SetActive(true);
                        list.Add(child.gameObject);
                    }

                    stack.Push(child);
                }
            }

            var setActiveFalseList = list.ToArray();


            //2.prefab의 skinmeshrenderer을 모아 이중 for each문으로 각각의 material을 가져온다.
            var skinnedMeshRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            List<Material> referenceMaterials = new List<Material>();

            foreach (var skinnedMesh in skinnedMeshRenderers)
            {
                foreach (var sMaterial in skinnedMesh.sharedMaterials)
                {
                    //3. lilToon인지 확인하고, 맞다면 referenceMaterial 라스트애 추가한다.
                    if (sMaterial != null && sMaterial.shader.name.Contains("lilToon"))
                    {
                        if (!referenceMaterials.Contains(sMaterial))
                        {
                            referenceMaterials.Add(sMaterial);
                        }
                    }
                }
            }

            //4.referneceMaterials를 순환하면 Mtoon으로 재질 변환한다. 변환한 material들은 Dictionary 형식으로 저장한다.
            var convertedMaterials = new Dictionary<Material, Material>();
            foreach (var material in referenceMaterials)
            {
                var path = Path.Combine(savePath,
                    material.name + ".mat");
                try
                {
                    lilMaterialBaker.CreateMToonMaterial(material, path);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                Material m = AssetDatabase.LoadAssetAtPath(path, typeof(Material)) as Material;
                if (!m)
                {
                    Debug.Log("no Materials : " + path);
                }

                convertedMaterials.Add(material, m);
            }


            //다시 skinMesh단위로 순환하며 Dictionary의 meterial들을 map에서 찾아 교체한다.
            foreach (var skinnedMesh in skinnedMeshRenderers)
            {
                var materials = skinnedMesh.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && materials[i].shader.name.Contains("lilToon"))
                    {
                        if (convertedMaterials.ContainsKey(materials[i]))
                        {
                            materials[i] = convertedMaterials[materials[i]];
                        }
                    }
                }

                skinnedMesh.sharedMaterials = materials;
            }

            //오브잭트들을 다시 비활성화한다.
            setActiveFalseList.ToList().ForEach(obj => obj.SetActive(false));
        }

        private static BlendShapeClip GetExpression(BlendShapeAvatar blendShapeAvatar, ExpressionPreset preset)
        {
            switch (preset)
            {
                case ExpressionPreset.aa:
                    return blendShapeAvatar.GetClip(BlendShapePreset.A);
                case ExpressionPreset.ih:
                    return blendShapeAvatar.GetClip(BlendShapePreset.I);
                case ExpressionPreset.ou:
                    return blendShapeAvatar.GetClip(BlendShapePreset.U);
                case ExpressionPreset.ee:
                    return blendShapeAvatar.GetClip(BlendShapePreset.E);
                case ExpressionPreset.oh:
                    return blendShapeAvatar.GetClip(BlendShapePreset.O);
                case ExpressionPreset.happy:
                    return blendShapeAvatar.GetClip(BlendShapePreset.Joy);
                case ExpressionPreset.angry:
                    return blendShapeAvatar.GetClip(BlendShapePreset.Angry);
                case ExpressionPreset.sad:
                    return blendShapeAvatar.GetClip(BlendShapePreset.Sorrow);
                case ExpressionPreset.relaxed:
                    return blendShapeAvatar.GetClip(BlendShapePreset.Fun);
                case ExpressionPreset.surprised:
                    var blendShapeClip = ScriptableObject.CreateInstance<BlendShapeClip>();
                    blendShapeClip.BlendShapeName = "Surprised";
                    blendShapeAvatar.Clips.Add(blendShapeClip);
                    return blendShapeClip;
                case ExpressionPreset.blink:
                    return blendShapeAvatar.GetClip(BlendShapePreset.Blink);
                case ExpressionPreset.blinkLeft:
                    return blendShapeAvatar.GetClip(BlendShapePreset.Blink_L);
                case ExpressionPreset.blinkRight:
                    return blendShapeAvatar.GetClip(BlendShapePreset.Blink_R);
                case ExpressionPreset.lookUp:
                    return blendShapeAvatar.GetClip(BlendShapePreset.LookUp);
                case ExpressionPreset.lookDown:
                    return blendShapeAvatar.GetClip(BlendShapePreset.LookDown);
                case ExpressionPreset.lookLeft:
                    return blendShapeAvatar.GetClip(BlendShapePreset.LookLeft);
                case ExpressionPreset.lookRight:
                    return blendShapeAvatar.GetClip(BlendShapePreset.LookRight);
                case ExpressionPreset.neutral:
                    return blendShapeAvatar.GetClip(BlendShapePreset.Neutral);
                case ExpressionPreset.custom:
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }
        }

        private static void ClearUnusedComponents(GameObject gameObject)
        {
            foreach (var transform in gameObject.transform.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (transform == null || transform.gameObject == null || transform.gameObject.activeSelf)
                {
                    continue;
                }
                Object.DestroyImmediate(transform.gameObject);
            }

            foreach (var component in gameObject.transform.GetComponentsInChildren<MonoBehaviour>())
            {
                if (component == null || component.enabled)
                {
                    continue;
                }
                Object.DestroyImmediate(component);
            }

            // particle system 모두 제거
            foreach (var particleSystem in gameObject.GetComponentsInChildren<ParticleSystem>())
            {
                Object.DestroyImmediate(particleSystem);
            }

            // particle system renderer 모두 제거
            foreach (var particleSystemRenderer in gameObject.GetComponentsInChildren<ParticleSystemRenderer>())
            {
                Object.DestroyImmediate(particleSystemRenderer);
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
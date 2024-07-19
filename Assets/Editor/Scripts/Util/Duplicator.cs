using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniGLTF;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRM;
using Object = UnityEngine.Object;

namespace Editor.Scripts.Util
{
    /// <summary>
    /// アバターの複製を行います。
    /// </summary>
    public class Duplicator
    {
        /// <summary>
        /// アセットの種類ごとの、複製先のフォルダ名の末尾に追加する文字列。
        /// </summary>
        private static readonly Dictionary<Type, string> FolderNameSuffixes = new()
        {
            { typeof(Mesh            ), ".Meshes"      },
            { typeof(Material        ), ".Materials"   },
            { typeof(Texture         ), ".Textures"    },
            { typeof(BlendShapeAvatar), ".BlendShapes" },
            { typeof(BlendShapeClip  ), ".BlendShapes" },
            { typeof(Object          ), ".VRChat"      },
        };

        /// <summary>
        /// オブジェクトを複製します。
        /// </summary>
        /// <param name="sourceAvatar">プレハブ、またはHierarchy上のオブジェクト。</param>
        /// <param name="destinationPath">「Assets/」から始まり「.prefab」で終わる複製先のパス。</param>
        /// <param name="notCombineRendererObjectNames">結合しないメッシュレンダラーのオブジェクト名。</param>
        /// <param name="combineMeshesAndSubMeshes">メッシュ・サブメッシュを結合するなら <c>true</c>。</param>
        /// <returns>複製後のインスタンス。</returns>
        public static GameObject Duplicate(
            GameObject sourceAvatar,
            string destinationPath,
            IEnumerable<string> notCombineRendererObjectNames,
            bool combineMeshesAndSubMeshes
        )
        {
            var destinationPrefab = DuplicatePrefab(sourceAvatar, destinationPath);
            var destinationPrefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(destinationPrefab);

            DuplicateAndCombineMeshes(
                destinationPrefabInstance,
                combineMeshesAndSubMeshes,
                notCombineRendererObjectNames
            );
            DuplicateMaterials(destinationPrefabInstance);

            PrefabUtility.RecordPrefabInstancePropertyModifications(destinationPrefabInstance);
            destinationPrefabInstance.transform.SetAsLastSibling();
            return destinationPrefabInstance;
        }

        /// <summary>
        /// アセットの種類に応じて、保存先を決定します。
        /// </summary>
        /// <param name="destinationFolderUnityPath"></param>
        /// <param name="type">アセットの種類。</param>
        /// <param name="fileName">ファイル名。</param>
        /// <returns>「Assets/」から始まるパス。</returns>
        internal static string DetermineAssetPath(string destinationFolderPath, Type type, string fileName = "")
        {
            var destinationFolderUnityPath = UnityPath.FromUnityPath(destinationFolderPath);
            foreach (var (assetType, suffix) in FolderNameSuffixes)
            {
                if (assetType.IsAssignableFrom(type))
                {
                    destinationFolderUnityPath = destinationFolderUnityPath.GetAssetFolder(suffix);
                    break;
                }
            }

            destinationFolderUnityPath.EnsureFolder();

            return destinationFolderUnityPath.Child(fileName).Value;
        }

        /// <summary>
        /// アセットの種類に応じて、保存先を決定します。
        /// </summary>
        /// <param name="prefabInstance"></param>
        /// <param name="type">アセットの種類。</param>
        /// <param name="fileName">ファイル名。</param>
        /// <returns>「Assets/」から始まるパス。</returns>
        internal static string DetermineAssetPath(GameObject prefabInstance, Type type, string fileName = "")
        {
            return DetermineAssetPath(
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabInstance),
                type,
                fileName
            );
        }

        /// <summary>
        /// アセットをプレハブが置かれているディレクトリの直下のフォルダへ複製します。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">複製元のオブジェクト。</param>
        /// <param name="prefabInstance">プレハブインスタンス。</param>
        /// <param name="fileName">ファイル名が複製元と異なる場合に指定。</param>
        /// <returns></returns>
        internal static T DuplicateAssetToFolder<T>(
            T source,
            GameObject prefabInstance,
            string fileName = ""
        ) where T : Object
        {
            string destinationFileName;
            if (string.IsNullOrEmpty(fileName))
            {
                var sourceUnityPath = UnityPath.FromAsset(source);
                if (!sourceUnityPath.IsUnderWritableFolder || AssetDatabase.IsSubAsset(source))
                {
                    destinationFileName = source.name.EscapeFilePath() + ".asset";
                }
                else
                {
                    destinationFileName = Path.GetFileName(sourceUnityPath.Value);
                }
            }
            else
            {
                destinationFileName = fileName;
            }

            return DuplicateAsset(
                source,
                destinationPath: DetermineAssetPath(prefabInstance, typeof(T), destinationFileName)
            );
        }

        /// <summary>
        /// オブジェクトをプレハブが置かれているディレクトリの直下のフォルダへ保存します。
        /// </summary>
        /// <remarks>
        /// 保存先にすでにアセットが存在していれば上書きし、metaファイルは新規生成しません。
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="System.ArgumentException">source がすでにアセットとして存在するか、<see cref="AnimatorController"> の場合。</exception>
        /// <param name="source">オブジェクト。</param>
        /// <param name="prefabInstance">プレハブインスタンス。</param>
        /// <param name="destinationFileName">ファイル名がオブジェクト名と異なる場合に指定。</param>
        /// <returns></returns>
        internal static T CreateObjectToFolder<T>(
            T source,
            string prefabPath,
            string? destinationFileName = null
        ) where T : Object
        {
            var path = AssetDatabase.GetAssetPath(source);
            if (!string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"source はすでにアセットとして「{path}」に存在します。アセットを作成しません。");
                return source; // 이미 에셋으로 존재하면 source를 반환하고, 새로운 에셋을 만들지 않음.
            }

            if (source is AnimatorController)
            {
                throw new ArgumentException($"{nameof(AnimatorController)} は上書きできません。", nameof(T));
            }

            var destinationPath = DetermineAssetPath(
                prefabPath,
                typeof(T),
                destinationFileName ?? source.name.EscapeFilePath() + ".asset"
            );

            var destination = AssetDatabase.LoadMainAssetAtPath(destinationPath);
            if (destination)
            {
                EditorUtility.CopySerialized(source, destination);
            }
            else
            {
                AssetDatabase.CreateAsset(source, destinationPath);
            }

            var dest = AssetDatabase.LoadAssetAtPath<T>(destinationPath);
            dest.name = Path.GetFileNameWithoutExtension(destinationPath);
            return dest;
        }

        /// <summary>
        /// オブジェクトをプレハブが置かれているディレクトリの直下のフォルダへ保存します。
        /// </summary>
        /// <remarks>
        /// 保存先にすでにアセットが存在していれば上書きし、metaファイルは新規生成しません。
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="ArgumentException">source がすでにアセットとして存在するか、<see cref="AnimatorController"> の場合。</exception>
        /// <param name="source">オブジェクト。</param>
        /// <param name="prefabInstance">プレハブインスタンス。</param>
        /// <param name="destinationFileName">ファイル名がオブジェクト名と異なる場合に指定。</param>
        /// <returns></returns>
        internal static T CreateObjectToFolder<T>(
            T source,
            GameObject prefabInstance,
            string destinationFileName
        ) where T : Object
        {
            return CreateObjectToFolder(
                source,
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabInstance),
                destinationFileName
            );
        }

        /// <summary>
        /// AnimatorControllerを複製します。
        /// </summary>
        /// <param name="sourceController"></param>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        private static AnimatorController DuplicateAnimatorControllerAsset(
            AnimatorController sourceController,
            string destinationPath
        )
        {
            var temporaryFolderPath = AssetDatabase.GenerateUniqueAssetPath("Assets/temporary");
            AssetDatabase.CreateFolder(
                Path.GetDirectoryName(temporaryFolderPath),
                Path.GetFileName(temporaryFolderPath)
            );
            var temporaryPath = Path.Combine(temporaryFolderPath, Path.GetFileName(destinationPath));
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(sourceController), temporaryPath);

            if (AssetDatabase.LoadMainAssetAtPath(destinationPath) == null)
            {
                AssetDatabase.CreateAsset(new AnimatorController(), destinationPath);
            }

            File.Copy(temporaryPath.AssetPathToFullPath(), destinationPath.AssetPathToFullPath(), overwrite: true);
            AssetDatabase.ImportAsset(destinationPath);

            var destinationController = AssetDatabase.LoadAssetAtPath<AnimatorController>(destinationPath);

            EditorUtility.SetDirty(destinationController);
            AssetDatabase.SaveAssets();

            AssetDatabase.DeleteAsset(temporaryFolderPath);

            return destinationController;
        }

        /// <summary>
        /// アセットインスタンスを複製します。
        /// </summary>
        /// <remarks>
        /// オブジェクト名の末尾に「(Clone)」が付加されないようにします。
        /// </remarks>
        /// <param name="instance"></param>
        /// <returns></returns>
        private static Object DuplicateAssetInstance(Object instance)
        {
            var newInstance = Object.Instantiate(instance);
            newInstance.name = instance.name;
            return newInstance;
        }

        /// <summary>
        /// アセットを複製します。
        /// </summary>
        /// <remarks>
        /// 複製先にすでにアセットが存在していれば上書きし、metaファイルは複製しません。
        /// </remarks>
        /// <param name="source"></param>
        /// <param name="duplicatedPath">「Assets/」から始まりファイル名で終わる複製先のパス。</param>
        private static T DuplicateAsset<T>(T source, string destinationPath) where T : Object
        {
            if (source is AnimatorController controller)
            {
                return (T)(object)DuplicateAnimatorControllerAsset(controller, destinationPath);
            }

            var sourceUnityPath = UnityPath.FromAsset(source);
            var destination = AssetDatabase.LoadMainAssetAtPath(destinationPath);
            var copied = false;
            if (destination)
            {
                if (AssetDatabase.IsNativeAsset(source) || !sourceUnityPath.IsUnderWritableFolder)
                {
                    EditorUtility.CopySerialized(source, destination);
                }
                else
                {
                    var sourceFullPath = sourceUnityPath.FullPath;
                    var destinationFullPath = destinationPath.AssetPathToFullPath();
                    if (File.GetLastWriteTime(sourceFullPath) != File.GetLastWriteTime(destinationFullPath))
                    {
                        File.Copy(sourceFullPath, destinationFullPath, overwrite: true);
                        AssetDatabase.ImportAsset(destinationPath);
                        copied = true;
                    }
                }
            }
            else
            {
                if (AssetDatabase.IsSubAsset(source) || !sourceUnityPath.IsUnderWritableFolder)
                {
                    AssetDatabase.CreateAsset(DuplicateAssetInstance(source), destinationPath);
                }
                else
                {
                    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), destinationPath);
                }
            }

            var destinationAsset = AssetDatabase.LoadAssetAtPath<T>(destinationPath);
            if (copied)
            {
                EditorUtility.SetDirty(destinationAsset);
                AssetDatabase.SaveAssets();
            }
            return destinationAsset;
        }

        /// <summary>
        /// ルートとなるプレハブを複製します。
        /// </summary>
        /// <param name="sourceAvatar">プレハブ、またはHierarchy上のオブジェクト。</param>
        /// <param name="destinationPath">「Assets/」から始まり「.prefab」で終わる複製先のパス。</param>
        /// <returns></returns>
        private static GameObject DuplicatePrefab(
            GameObject sourceAvatar,
            string destinationPath
        )
        {
            // プレハブ
            GameObject sourceInstance = Object.Instantiate(sourceAvatar);
            GameObject destinationPrefab = PrefabUtility
                .SaveAsPrefabAssetAndConnect(sourceInstance, destinationPath, InteractionMode.AutomatedAction);
            Object.DestroyImmediate(sourceInstance);

            // Avatar
            var animator = destinationPrefab.GetComponent<Animator>();
            var destinationAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(destinationPath);
            if (destinationAvatar)
            {
                EditorUtility.CopySerialized(animator.avatar, destinationAvatar);
                animator.avatar = destinationAvatar;
            }
            else
            {
                animator.avatar = (Avatar)DuplicateAssetInstance(animator.avatar);
                AssetDatabase.AddObjectToAsset(animator.avatar, destinationPrefab);
            }

            // AvatarDescription (最終的に削除するので、アセットは複製しない)
            var humanoidDescription = destinationPrefab.GetComponent<VRMHumanoidDescription>();
            humanoidDescription.Avatar = animator.avatar;
            var description = humanoidDescription.Description;
            if (description == null)
            {
                // VRMInitializerで生成されたモデル
                description = PrefabUtility.GetOutermostPrefabInstanceRoot(sourceAvatar) // GetDescriptionはFBX等以外では機能しない
                    .GetComponent<VRMHumanoidDescription>().GetDescription(out var _);
            }
            humanoidDescription.Description = description;

            return destinationPrefab;
        }

        /// <summary>
        /// もっともシェイプキーが多いメッシュを取得します。
        /// </summary>
        /// <param name="prefabInstance"></param>
        /// <returns></returns>
        private static SkinnedMeshRenderer GetFaceMeshRenderer(GameObject prefabInstance)
        {
            return prefabInstance.GetComponentsInChildren<SkinnedMeshRenderer>()
                .OrderByDescending(renderer => renderer.sharedMesh ? renderer.sharedMesh.blendShapeCount : 0).First();
        }

        /// <summary>
        /// プレハブが依存しているメッシュを複製・結合します。
        /// </summary>
        /// <param name="prefabInstance"></param>
        /// <param name="combineMeshesAndSubMeshes"></param>
        /// <param name="notCombineRendererObjectNames"></param>
        private static void DuplicateAndCombineMeshes(
            GameObject prefabInstance,
            bool combineMeshesAndSubMeshes,
            IEnumerable<string> notCombineRendererObjectNames
        )
        {
            var faceMeshTransform = combineMeshesAndSubMeshes
                ? null
                : GetFaceMeshRenderer(prefabInstance: prefabInstance).transform;

            var sameNameTransform = prefabInstance.transform.Find(VRChat.AutoBlinkMeshPath);
            if (sameNameTransform && (faceMeshTransform == null || faceMeshTransform != sameNameTransform))
            {
                sameNameTransform.name += "-" + VRChat.AutoBlinkMeshPath;
            }

            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabInstance);
            if (faceMeshTransform == null)
            {
                // CombineMeshesAndSubMeshes.Combine(
                //     root: prefabInstance,
                //     notCombineRendererObjectNames,
                //     destinationObjectName: VRChat.AutoBlinkMeshPath
                // );
            }
            else
            {
                if (faceMeshTransform != sameNameTransform)
                {
                    faceMeshTransform.parent = prefabInstance.transform;
                    faceMeshTransform.name = VRChat.AutoBlinkMeshPath;
                }
            }
            PrefabUtility.SaveAsPrefabAssetAndConnect(prefabInstance, prefabPath, InteractionMode.AutomatedAction);

            var alreadyDuplicatedMeshes = new Dictionary<Mesh, Mesh>();

            foreach (var renderer in prefabInstance.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (combineMeshesAndSubMeshes && renderer.name == VRChat.AutoBlinkMeshPath)
                {
                    continue;
                }

                var mesh = renderer.sharedMesh;
                renderer.sharedMesh = alreadyDuplicatedMeshes.ContainsKey(mesh)
                    ? alreadyDuplicatedMeshes[mesh]
                    : DuplicateAssetToFolder(
                        source: mesh,
                        prefabInstance,
                        fileName: Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(mesh))
                            == VRChat.AutoBlinkMeshPath + ".asset"
                            ? VRChat.AutoBlinkMeshPath + "-" + VRChat.AutoBlinkMeshPath + ".asset"
                            : ""
                    );
                alreadyDuplicatedMeshes[mesh] = renderer.sharedMesh;
            }

            foreach (var filter in prefabInstance.GetComponentsInChildren<MeshFilter>())
            {
                Mesh mesh = filter.sharedMesh;
                filter.sharedMesh = alreadyDuplicatedMeshes.ContainsKey(mesh)
                    ? alreadyDuplicatedMeshes[mesh]
                    : DuplicateAssetToFolder(
                        source: mesh,
                        prefabInstance,
                        fileName: Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(mesh))
                            == VRChat.AutoBlinkMeshPath + ".asset"
                            ? VRChat.AutoBlinkMeshPath + "-" + VRChat.AutoBlinkMeshPath + ".asset"
                            : ""
                    );
                alreadyDuplicatedMeshes[mesh] = filter.sharedMesh;
            }
        }

        /// <summary>
        /// プレハブが依存しているマテリアルを複製します。
        /// </summary>
        /// <param name="prefabInstance"></param>
        private static void DuplicateMaterials(GameObject prefabInstance)
        {
            var alreadyDuplicatedMaterials = new Dictionary<Material, Material>();

            foreach (var renderer in prefabInstance.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(material =>
                {
                    if (alreadyDuplicatedMaterials.ContainsKey(material))
                    {
                        return alreadyDuplicatedMaterials[material];
                    }

                    return alreadyDuplicatedMaterials[material]
                        = DuplicateAssetToFolder(source: material, prefabInstance);
                }).ToArray();
            }
        }
    }
}
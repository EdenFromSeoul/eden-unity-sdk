using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniGLTF.MeshUtility;
using UnityEditor;
using UnityEngine;

namespace Editor.Scripts.Util
{
    /// <summary>
    /// 指定したオブジェクト階下のメッシュを、指定したオブジェクト直下へ結合します。その際、マテリアルが同一であるサブメッシュ (マテリアルスロット) を結合します。
    /// </summary>
    internal static class CombineMeshesAndSubMeshes
    {
        /// <summary>
        /// メッシュを結合します。
        /// </summary>
        /// <param name="root"></param>
        /// <param name="notCombineRendererObjectNames">結合しないメッシュレンダラーのオブジェクト名。</param>
        /// <param name="destinationObjectName">結合したメッシュのオブジェクト名。</param>
        /// <param name="savingAsAsset">アセットとして保存しないなら <c>true</c> を指定。</param>
        /// <returns></returns>
        internal static SkinnedMeshRenderer Combine(
            GameObject root,
            IEnumerable<string> notCombineRendererObjectNames,
            string destinationObjectName,
            bool savingAsAsset = true
        )
        {
            return CombineAllMeshes(
                root: root,
                destinationObjectName: destinationObjectName,
                notCombineRendererObjectNames: notCombineRendererObjectNames,
                savingAsAsset: savingAsAsset
            );
        }

        /// <summary>
        /// メッシュ、サブメッシュを結合します。
        /// </summary>
        /// <param name="root"></param>
        /// <param name="destinationObjectName"></param>
        /// <param name="notCombineRendererObjectNames"></param>
        /// <param name="savingAsAsset"></param>
        /// <returns></returns>
        private static SkinnedMeshRenderer CombineAllMeshes(
            GameObject root,
            string destinationObjectName,
            IEnumerable<string> notCombineRendererObjectNames,
            bool savingAsAsset
        )
        {
            var meshIntegrationGroup = new MeshIntegrationGroup
            {
                Name = destinationObjectName,
            };
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (notCombineRendererObjectNames.Contains(renderer.name))
                {
                    continue;
                }
                meshIntegrationGroup.Renderers.Add(renderer);
            }
            MeshIntegrator.TryIntegrate(
                meshIntegrationGroup,
                MeshIntegrator.BlendShapeOperation.Use,
                out var meshIntegrationResult
            );

            var destinationRenderer
                = meshIntegrationResult.AddIntegratedRendererTo(root).ElementAt(0).GetComponent<SkinnedMeshRenderer>();

            var rootPath = AssetDatabase.GetAssetPath(root);
            if (string.IsNullOrEmpty(rootPath))
            {
                rootPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            }

            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (renderer == destinationRenderer || notCombineRendererObjectNames.Contains(renderer.name))
                {
                    continue;
                }

                GameObject gameObject = renderer.gameObject;
                if (!gameObject)
                {
                    continue;
                }

                if (gameObject.transform.childCount > 0)
                {
                    Object.DestroyImmediate(renderer);
                }
                else
                {
                    var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
                    if (prefabRoot)
                    {
                        PrefabUtility.UnpackPrefabInstance(
                            prefabRoot,
                            PrefabUnpackMode.OutermostRoot,
                            InteractionMode.AutomatedAction
                        );
                    }
                    Object.DestroyImmediate(gameObject);
                }
            }

            if (savingAsAsset)
            {
                var destinationFolderPath = "Assets";
                if (!string.IsNullOrEmpty(rootPath))
                {
                    destinationFolderPath = Path.ChangeExtension(rootPath, ".Meshes");
                    if (!AssetDatabase.IsValidFolder(destinationFolderPath))
                    {
                        AssetDatabase.CreateFolder(
                            Path.GetDirectoryName(destinationFolderPath),
                            Path.GetFileName(destinationFolderPath)
                        );
                    }
                }

                var destinationPath = destinationFolderPath + "/" + destinationRenderer.sharedMesh.name + ".asset";
                var destination = AssetDatabase.LoadAssetAtPath<Mesh>(destinationPath);
                if (destination)
                {
                    destination.Clear(false);
                    EditorUtility.CopySerialized(destinationRenderer.sharedMesh, destination);
                    destinationRenderer.sharedMesh = destination;
                }
                else
                {
                    AssetDatabase.CreateAsset(destinationRenderer.sharedMesh, destinationPath);
                }
            }

            return destinationRenderer;
        }
    }
}
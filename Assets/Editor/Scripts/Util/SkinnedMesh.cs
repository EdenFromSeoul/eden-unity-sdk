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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniVRM10;

namespace Editor.Scripts.Util
{
    public static class SkinnedMesh
    {
        internal static IEnumerable<BlendShape> GetAllShapeKeys(Mesh mesh, bool useShapeKeyNormalsAndTangents)
        {
            var shapeKeys = new List<BlendShape>();

            var meshVertexCount = mesh.vertexCount;
            for (var i = 0; i < mesh.blendShapeCount; i++)
            {
                var deltaVertices = new Vector3[meshVertexCount];
                var deltaNormals = new Vector3[meshVertexCount];
                var deltaTangents = new Vector3[meshVertexCount];

                mesh.GetBlendShapeFrameVertices(
                    i,
                    0,
                    deltaVertices,
                    useShapeKeyNormalsAndTangents ? deltaNormals : null,
                    useShapeKeyNormalsAndTangents ? deltaTangents : null
                );

                var blendShape = new BlendShape(
                    name: mesh.GetBlendShapeName(i)
                );
                blendShape.Positions.AddRange(deltaVertices);
                blendShape.Normals.AddRange(deltaNormals);
                blendShape.Tangents.AddRange(deltaTangents);
                shapeKeys.Add(blendShape);
            }

            return shapeKeys;
        }

        internal static void CleanUpShapeKeys(Mesh mesh, IEnumerable<string> nesessaryShapeKeys)
        {
            var shapeKeys = GetAllShapeKeys(mesh, useShapeKeyNormalsAndTangents: false);
            mesh.ClearBlendShapes();
            foreach (var name in nesessaryShapeKeys)
            {
                var shapeKey = shapeKeys.FirstOrDefault(key => key.Name == name);
                if (shapeKey == null)
                {
                    continue;
                }

                mesh.AddBlendShapeFrame(
                    shapeKey.Name,
                    100,
                    shapeKey.Positions.ToArray(),
                    shapeKey.Normals.ToArray(),
                    shapeKey.Tangents.ToArray()
                );
            }

        }

        /// <summary>
        /// 指定したメッシュのすべてのシェイプキーを取得します。
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="useShapeKeyNormalsAndTangents"></param>
        /// <returns></returns>
        internal static IEnumerable<UniGLTF.BlendShape> GetAllShapeKeysVrm0(Mesh mesh, bool useShapeKeyNormalsAndTangents)
        {
            var shapeKeys = new List<UniGLTF.BlendShape>();

            var meshVertexCount = mesh.vertexCount;
            for (var i = 0; i < mesh.blendShapeCount; i++)
            {
                var deltaVertices = new Vector3[meshVertexCount];
                var deltaNormals = new Vector3[meshVertexCount];
                var deltaTangents = new Vector3[meshVertexCount];

                mesh.GetBlendShapeFrameVertices(
                    i,
                    0,
                    deltaVertices,
                    useShapeKeyNormalsAndTangents ? deltaNormals : null,
                    useShapeKeyNormalsAndTangents ? deltaTangents : null
                );

                var blendShape = new UniGLTF.BlendShape(
                    name: mesh.GetBlendShapeName(i),
                    vertexCount: 0,
                    hasPositions: true,
                    hasNormals: true,
                    hasTangents: true
                );
                blendShape.Positions.AddRange(deltaVertices);
                blendShape.Normals.AddRange(deltaNormals);
                blendShape.Tangents.AddRange(deltaTangents);
                shapeKeys.Add(blendShape);
            }

            return shapeKeys;
        }



        /// <summary>
        /// nesessaryShapeKeysで指定されたシェイプキーから法線・接線を削除し、それ以外のシェイプキーは削除します。
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="nesessaryShapeKeys"></param>
        /// <returns></returns>
        internal static void CleanUpShapeKeysVrm0(Mesh mesh, IEnumerable<string> nesessaryShapeKeys)
        {
            var shapeKeys = GetAllShapeKeysVrm0(mesh, useShapeKeyNormalsAndTangents: false);
            mesh.ClearBlendShapes();
            foreach (var name in nesessaryShapeKeys)
            {
                var shapeKey = shapeKeys.FirstOrDefault(key => key.Name == name);
                if (shapeKey == null)
                {
                    continue;
                }

                mesh.AddBlendShapeFrame(
                    shapeKey.Name,
                    100,
                    shapeKey.Positions.ToArray(),
                    shapeKey.Normals.ToArray(),
                    shapeKey.Tangents.ToArray()
                );
            }

        }
    }
}
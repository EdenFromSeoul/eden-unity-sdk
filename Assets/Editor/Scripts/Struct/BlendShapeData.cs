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
using UnityEngine;

namespace Editor.Scripts.Struct
{
    [Serializable]
    public struct BlendShapeData
    {
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public int index;
        public string shapeKeyName;

        public BlendShapeData(SkinnedMeshRenderer skinnedMeshRenderer, int index, string shapeKeyName)
        {
            this.skinnedMeshRenderer = skinnedMeshRenderer;
            this.index = index;
            this.shapeKeyName = shapeKeyName;
        }
    }
}
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
using UnityEngine;

namespace Editor.Scripts.Util
{
    /// <summary>
    /// <see cref="VRM.VRMSpringBone"/>の各種パラメータに対応する値。
    /// </summary>
    public class VRMSpringBoneParameters
    {
        public float StiffnessForce = 1.0f;
        public float GravityPower = 0.0f;
        public Vector3 GravityDir = new(0, -1.0f, 0);
        public float DragForce = 0.4f;
    }
}
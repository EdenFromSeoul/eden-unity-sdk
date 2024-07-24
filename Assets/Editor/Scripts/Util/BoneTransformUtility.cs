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
#nullable enable
using UnityEngine;

namespace Editor.Scripts.Util
{
    public static class BoneTransformUtility
    {
        /// <summary>
        /// 向きが異なる <see cref="Transform"/> 同士で、同じワールド座標へのオフセットを計算します。
        /// </summary>
        /// <param name="sourceTransform"></param>
        /// <param name="sourceOffset"></param>
        /// <param name="destinationTransform"></param>
        /// <returns></returns>
        internal static Vector3 CalculateOffset(
            Transform sourceTransform,
            Vector3 sourceOffset,
            Transform destinationTransform
        )
        {
            return destinationTransform.InverseTransformPoint(
                sourceTransform.TransformPoint(sourceOffset) - sourceTransform.position
                + destinationTransform.position
            );
        }

        /// <summary>
        /// 다른 스케일을 가진 두 <see cref="Transform"/> 간의 길이를 계산합니다.
        /// </summary>
        /// <param name="sourceTransform">X축 방향의 스케일을 사용하는 원본 <see cref="Transform"/>.</param>
        /// <param name="distance">조정할 길이.</param>
        /// <param name="destinationTransform">X축 방향의 스케일을 사용하는 대상 <see cref="Transform"/>. 지정하지 않으면 길이는 정규화됩니다.</param>
        /// <returns>조정된 길이 값.</returns>
        internal static float CalculateDistance(
            Transform sourceTransform,
            float distance,
            Transform? destinationTransform = null
        )
        {
            return distance * sourceTransform.lossyScale.x
                   / (destinationTransform != null ? destinationTransform.lossyScale.x : 1);
        }
    }
}
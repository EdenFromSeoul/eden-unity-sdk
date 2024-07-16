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
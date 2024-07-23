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
#nullable enable
using System;
using System.Linq;
using UnityEngine;
using VRC.Dynamics;

namespace Editor.Scripts.Util
{
    public class VRCPhysBoneParameters
    {
#if VRC_SDK_VRCSDK3
        public VRCPhysBoneBase.Version Version =
            (VRCPhysBoneBase.Version)Enum.GetValues(typeof(VRCPhysBoneBase.Version)).Cast<int>().Max();
        public VRCPhysBoneBase.IntegrationType IntegrationType = VRCPhysBoneBase.IntegrationType.Simplified;
#endif
        public float Pull = 0.2f;
        public AnimationCurve? PullCurve = null;
        public float Spring = 0.2f;
        public AnimationCurve? SpringCurve = null;
        public float Stiffness = 0.2f;
        public AnimationCurve? StiffnessCurve = null;
        public float Gravity = 0;
        public AnimationCurve? GravityCurve = null;
        public float GravityFalloff = 0;
        public AnimationCurve? GravityFalloffCurve = null;
        public float Radius = 0.1f;
#if VRC_SDK_VRCSDK3
        public VRCPhysBoneBase.ImmobileType ImmobileType;
#endif
        public float Immobile = 0;
        public AnimationCurve? ImmobileCurve = null;
        public float GrabMovement = 0;
        public float MaxStretch = 0;
        public AnimationCurve? MaxStretchCurve = null;
    }
}
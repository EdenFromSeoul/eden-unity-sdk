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
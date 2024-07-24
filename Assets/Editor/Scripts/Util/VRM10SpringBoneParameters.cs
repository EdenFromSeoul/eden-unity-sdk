using UnityEngine;

namespace Editor.Scripts.Util
{
    public class VRM10SpringBoneParameters
    {
        public float StiffnessForce = 1.0f;
        public float GravityPower = 0;
        public Vector3 GravityDir = new(0, -1.0f, 0);
        public float DragForce = 0.4f;
        public float JointRadius = 0.02f;
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace Editor.Scripts.Struct
{
    public struct VRChatExpressionBinding
    {
        public string RelativePath;
        public AnimationClip AnimationClip;
        public IEnumerable<string> ShapeKeyNames;
    }
}
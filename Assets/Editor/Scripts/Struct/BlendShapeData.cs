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
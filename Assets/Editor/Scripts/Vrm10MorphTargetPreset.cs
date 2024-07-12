using System;
using System.Collections.Generic;
using UnityEngine;

namespace Editor.Scripts
{
    [Serializable]
    public struct SerializableBlendShapeData
    {
        public string skinnedMeshRendererPath;
        public int index;
        public string shapeKeyName;

        public SerializableBlendShapeData(string skinnedMeshRendererPath, int index, string shapeKeyName)
        {
            this.skinnedMeshRendererPath = skinnedMeshRendererPath;
            this.index = index;
            this.shapeKeyName = shapeKeyName;
        }
    }


    /// <summary>
    /// VRM 1.0 모프 타겟 프리셋.
    /// </summary>
    [CreateAssetMenu(fileName = "Vrm10MorphTargetPreset", menuName = "Vrm10MorphTargetPreset", order = 0)]
    public class Vrm10MorphTargetPreset: ScriptableObject
    {
        [SerializeField] public string presetName;
        [SerializeField] public List<SerializableBlendShapeData> happy;
        [SerializeField] public List<SerializableBlendShapeData> angry;
        [SerializeField] public List<SerializableBlendShapeData> sad;
        [SerializeField] public List<SerializableBlendShapeData> surprised;
        [SerializeField] public List<SerializableBlendShapeData> relaxed;

        public IEnumerable<SerializableBlendShapeData> Get(string expression)
        {
            return expression switch
            {
                "happy" => happy ?? new List<SerializableBlendShapeData>(),
                "angry" => angry ?? new List<SerializableBlendShapeData>(),
                "sad" => sad ?? new List<SerializableBlendShapeData>(),
                "surprised" => surprised ?? new List<SerializableBlendShapeData>(),
                "relaxed" => relaxed ?? new List<SerializableBlendShapeData>(),
                _ => new List<SerializableBlendShapeData>()
            };
        }
        
        public void Set(string expression, List<SerializableBlendShapeData> data)
        {
            switch (expression)
            {
                case "happy":
                    happy = data;
                    break;
                case "angry":
                    angry = data;
                    break;
                case "sad":
                    sad = data;
                    break;
                case "surprised":
                    surprised = data;
                    break;
                case "relaxed":
                    relaxed = data;
                    break;
            }
        }
    }
}
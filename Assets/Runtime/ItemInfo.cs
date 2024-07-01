using System;
using UnityEngine;

namespace Runtime
{
    public class ItemInfo : ScriptableObject
    {
        public enum ModelType { VRChat, VRM, Other }
        public enum ModelSlot { Head, Body, Legs, Feet, Hands, Other }
        public enum ModelStatus { Pinned, Show, Hidden, Other }

        public string path;
        public string modelName;
        public string lastModified;
        public ModelType type;
        public ModelSlot slot;
        public ModelStatus status;
        public Texture2D preview { get; internal set; }

        public ItemInfoData ToData()
        {
            return new ItemInfoData
            {
                path = path,
                modelName = modelName,
                lastModified = lastModified,
                type = (ItemInfoData.ModelType) Enum.Parse(typeof(ItemInfoData.ModelType), type.ToString()),
                slot = (ItemInfoData.ModelSlot) Enum.Parse(typeof(ItemInfoData.ModelSlot), slot.ToString()),
                status = (ItemInfoData.ModelStatus) Enum.Parse(typeof(ItemInfoData.ModelStatus), status.ToString()),
                preview = preview != null ? Convert.ToBase64String(preview.EncodeToPNG()) : null
            };
        }

    }
}
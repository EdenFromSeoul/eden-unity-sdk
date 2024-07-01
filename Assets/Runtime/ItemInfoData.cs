using System;
using System.Collections.Generic;

namespace Runtime
{
    [Serializable]
    public class ItemInfoData
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
        public string preview; // base64 encoded string for the texture
    }

    [Serializable]
    public class ItemInfoListData
    {
        public List<ItemInfoData> items;
    }
}
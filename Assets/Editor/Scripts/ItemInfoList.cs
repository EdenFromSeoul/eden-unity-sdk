using System;
using System.Collections.Generic;
using UnityEngine;

namespace Editor.Scripts
{
    [Serializable]
    public class ItemInfoList : ScriptableObject
    {
        public List<ItemInfo> items;

        public ItemInfoListData ToData()
        {
            var data = new ItemInfoListData();
            data.items = new List<ItemInfoData>();
            foreach (var item in items)
            {
                data.items.Add(item.ToData());
            }

            return data;
        }
    }
}

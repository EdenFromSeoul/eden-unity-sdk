using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Runtime;
using UnityEditor;
using UnityEngine;

namespace Editor.Scripts.Manager
{
    public class ItemManager : MonoBehaviour
    {
        private const string ItemsInfoPath = "Assets/Eden/Items.json";

        public static List<ItemInfo> ItemsInfoList;

        public static void Initialize()
        {
            if (!File.Exists(ItemsInfoPath))
            {
                File.WriteAllText(ItemsInfoPath, "{}");
            }

            UpdateItemsInfo();
        }

        public static void UpdateItemsInfo()
        {
            var allItems = GetAllPrefabsAsItems();

            Debug.Log(allItems.Count);
            Debug.Log(JsonUtility.ToJson(allItems));

            allItems = allItems.OrderByDescending(i => i.status == ItemInfo.ModelStatus.Pinned)
                .ThenByDescending(i => i.lastModified)
                .ToList();
            SaveItemsInfo(allItems);

            if (ItemsInfoList == null)
            {
                ItemsInfoList = allItems;
            }
        }

        internal static void SaveItemsInfo(List<ItemInfo> items)
        {
            string directory = Path.GetDirectoryName(ItemsInfoPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var itemsInfo = ScriptableObject.CreateInstance<ItemInfoList>();
            itemsInfo.items = items;
            var json = JsonUtility.ToJson(itemsInfo.ToData(), true);
            File.WriteAllText(ItemsInfoPath, json);
        }

        internal static List<ItemInfo> GetItemsInfo()
        {
            if (ItemsInfoList != null) return ItemsInfoList;
            var json = File.ReadAllText(ItemsInfoPath);
            var itemsInfo = JsonUtility.FromJson<ItemInfoList>(json);
            ItemsInfoList = itemsInfo.items;

            return ItemsInfoList;
        }

        internal static List<ItemInfo> GetAllPrefabsAsItems(bool preview = false)
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

            return guids.Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(GetItem)
                .OrderByDescending(p => p.lastModified)
                .ToList();
        }

        internal static ItemInfo GetItem(string itemPath)
        {
            var itemInfo = ScriptableObject.CreateInstance<ItemInfo>();
            itemInfo.path = itemPath;
            itemInfo.modelName = Path.GetFileNameWithoutExtension(itemPath);
            itemInfo.lastModified = File.GetLastWriteTime(itemPath).ToString(CultureInfo.InvariantCulture);

            return itemInfo;
        }
    }
}
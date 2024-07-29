using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Editor.Scripts.Util;
using UnityEditor;
using UnityEngine;

namespace Editor.Scripts.Manager
{
    public class ItemManager : MonoBehaviour
    {
        private const string ItemsInfoPath = "Assets/Eden/items.eden";

        public static List<ItemInfo> ItemsInfoList;

        public static void Initialize()
        {
            if (!File.Exists(ItemsInfoPath))
            {
                SaveItemsInfo(new List<ItemInfo>());
            }

            UpdateItemsInfo();
        }

        public static void UpdateItemsInfo()
        {
            var allItems = GetAllPrefabsAsItems();

            Debug.Log(allItems.Count);

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

            using (FileStream fileStream = new FileStream(ItemsInfoPath, FileMode.Create))
            {
                var itemsInfo = ScriptableObject.CreateInstance<ItemInfoList>();
                itemsInfo.items = items;
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fileStream, itemsInfo.ToData());
            }
        }

        internal static List<ItemInfo> GetItemsInfo()
        {
            if (ItemsInfoList != null) return ItemsInfoList;

            using (FileStream fileStream = new FileStream(ItemsInfoPath, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                ItemsInfoList = (List<ItemInfo>)formatter.Deserialize(fileStream);
            }

            return ItemsInfoList;
        }

        internal static List<ItemInfo> GetAllPrefabsAsItems(bool preview = false)
        {
            var guids = AssetUtility.FindAssetsExcludingDirectory("t:Prefab", new[] { "Resources", "Eden", "lilToon" });

            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
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

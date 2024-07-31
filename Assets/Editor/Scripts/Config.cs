using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Editor.Scripts
{
    public class Config
    {
        public static string ConfigPath = "Assets/Eden/Studios/config.eden";
        
        [Serializable]
        public class ConfigData
        {
            public string language;
            public string theme;
        }
        
        public static ConfigData LoadConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                SaveConfig(new ConfigData());
            }

            using (FileStream fileStream = new FileStream(ConfigPath, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (ConfigData)formatter.Deserialize(fileStream);
            }
        }
        
        public static void SaveConfig(ConfigData config)
        {
            string directory = Path.GetDirectoryName(ConfigPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (FileStream fileStream = new FileStream(ConfigPath, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fileStream, config);
            }
        }
    }
}
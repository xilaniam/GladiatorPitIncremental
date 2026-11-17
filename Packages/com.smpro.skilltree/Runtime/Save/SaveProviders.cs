using System;
using System.IO;
using UnityEngine;

namespace SMPro.SkillTree
{
    public interface ISaveProvider
    {
        void Save(string key, string json);
        bool TryLoad(string key, out string json);
    }

    [Serializable]
    public class PlayerPrefsSaveProvider : ISaveProvider
    {
        public string keyPrefix = "skilltree.";

        public void Save(string key, string json)
        {
            PlayerPrefs.SetString(keyPrefix + key, json);
            PlayerPrefs.Save();
        }

        public bool TryLoad(string key, out string json)
        {
            json = PlayerPrefs.GetString(keyPrefix + key, null);
            return !string.IsNullOrEmpty(json);
        }
    }

    [Serializable]
    public class FileSaveProvider : ISaveProvider
    {
        public string folder = "SkillTree";

        public void Save(string key, string json)
        {
            string path = PathFor(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
        }

        public bool TryLoad(string key, out string json)
        {
            string path = PathFor(key);
            json = File.Exists(path) ? File.ReadAllText(path) : null;
            return json != null;
        }

        string PathFor(string key) => Path.Combine(Application.persistentDataPath, folder, key + ".json");
    }
}

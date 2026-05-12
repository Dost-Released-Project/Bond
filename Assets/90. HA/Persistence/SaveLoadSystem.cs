using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Bond.Persistence
{
    public interface ISaveable
    {
        string Key { get; }
        object Data { get; }
        void Restore(object data);
    }

    public interface ISaveable<T> : ISaveable
    {
        new T Data { get; }
        void Restore(T data);
        
        object ISaveable.Data => Data;
        void ISaveable.Restore(object data) => Restore((T)data);
    }

    public class SaveData : ISaveable<string>
    {
        public string Key { get; set; }
        public string Data { get; set; }
        public void Restore(string data)
        {
            Data = data;
        }
    }
    
    public class SaveLoadSystem
    {
        public string SaveRootDirectory;
        private List<ISaveable> saveables = new List<ISaveable>();
        
        private JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto,
        };

        public SaveLoadSystem()
        {
#if UNITY_EDITOR
            SaveRootDirectory = Path.Combine(Application.dataPath, "Data", "Save");
#else
            SaveRootDirectory = Path.Combine(Application.persistentDataPath, "Save");
#endif
            if (Directory.Exists(SaveRootDirectory) == false)
            {
                Directory.CreateDirectory(SaveRootDirectory);
            }
        }

        public void Register(ISaveable saveable)
        {
            if (saveables.Any(x => x.Key == saveable.Key))
                saveable = saveables.First(x => x.Key == saveable.Key);
            saveables.Add(saveable);
        }
        
        public void LoadAndRegister(ISaveable saveable)
        {
            saveable.Restore(Load(saveable.Key));
            Register(saveable);
        }

        public void Unregister(ISaveable saveable)
        {
            saveables.Remove(saveable);
        }

        private string GetPath(string saveKey)
        {
            return Path.Combine(SaveRootDirectory, $"{saveKey}.json");
        }
        
        public void SaveAll()
        {
            foreach (ISaveable saveable in saveables)
            {
                Save(saveable.Key, saveable.Data);
            }
            
            AssetDatabase.Refresh();
        }

        public void Save(ISaveable saveable)
        {
            Save(saveable.Key, saveable.Data);
        }

        private void Save(string key, object data)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented, settings);
            string path = GetPath(key);
            File.WriteAllText(path, json);
        }

        public IEnumerable<ISaveable> Load()
        {
            foreach (var filePath in Directory.EnumerateFiles(SaveRootDirectory, "*.json"))
            {
                string json = File.ReadAllText(filePath);
                var obj = JsonConvert.DeserializeObject(json, settings);

                yield return obj as ISaveable;
            }
        }

        private object Load(string key)
        {
            string json = File.ReadAllText(GetPath(key));
            var obj = JsonConvert.DeserializeObject(json, settings);

            Debug.Log(obj.GetType());
            return obj;
        }

        public void DebugList()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var saveable in saveables)
            {
                sb.AppendLine(saveable.Key);
            }
            Debug.Log(sb.ToString());
        }
    }
}
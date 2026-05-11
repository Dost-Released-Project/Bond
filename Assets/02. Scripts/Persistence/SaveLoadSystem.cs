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
        /// <summary>
        /// 저장할 파일명
        /// </summary>
        string Key { get; }
        /// <summary>
        /// 저장할 데이터
        /// </summary>
        object Data { get; }
        /// <summary>
        /// 데이터를 받고 다시 인스턴스로 복원하는 로직
        /// </summary>
        /// <param name="data"></param>
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
    
    public static class SaveLoadSystem
    {
#if UNITY_EDITOR
        public static string SaveRootDirectory = Path.Combine(Application.dataPath, "Data", "Save");
#else
        public static string SaveRootDirectory = Path.Combine(Application.persistentDataPath, "Save");
#endif
        // public static string SaveRootDirectory;
        // private List<ISaveable> saveables = new List<ISaveable>();
        
        private static JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto,
        };

//         public SaveLoadSystem()
//         {
// #if UNITY_EDITOR
//             SaveRootDirectory = Path.Combine(Application.dataPath, "Data", "Save");
// #else
//             SaveRootDirectory = Path.Combine(Application.persistentDataPath, "Save");
// #endif
//             if (Directory.Exists(SaveRootDirectory) == false)
//             {
//                 Directory.CreateDirectory(SaveRootDirectory);
//             }
//         }
//
//         public void Register(ISaveable saveable)
//         {
//             if (saveables.Any(x => x.Key == saveable.Key))
//                 saveable = saveables.First(x => x.Key == saveable.Key);
//             saveables.Add(saveable);
//         }
//         
//         public void LoadAndRegister(ISaveable saveable)
//         {
//             saveable.Restore(Load(saveable.Key));
//             Register(saveable);
//         }
//         
//         public void Unregister(ISaveable saveable)
//         {
//             saveables.Remove(saveable);
//         }

        private static string GetPath(string saveKey)
        {
            return Path.Combine(SaveRootDirectory, $"{saveKey}.json");
        }
        
        // public void SaveAll()
        // {
        //     foreach (ISaveable saveable in saveables)
        //     {
        //         Save(saveable.Key, saveable.Data);
        //     }
        //     
        //     AssetDatabase.Refresh();
        // }

        public static void Save(ISaveable saveable)
        {
            Save(saveable.Key, saveable.Data);
        }

        private static void Save(string key, object data)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented, settings);
            string path = GetPath(key);
            File.WriteAllText(path, json);

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

        public static IEnumerable<ISaveable> ReadAll()
        {
            foreach (var filePath in Directory.EnumerateFiles(SaveRootDirectory, "*.json"))
            {
                string json = File.ReadAllText(filePath);
                var obj = JsonConvert.DeserializeObject(json, settings);

                yield return obj as ISaveable;
            }
        }

        public static void Load(ISaveable saveable)
        {
            string json = File.ReadAllText(GetPath(saveable.Key));
            Type type = saveable.Data.GetType();
            var obj = JsonConvert.DeserializeObject(json, type, settings);
            saveable.Restore(obj);
        }

        public static void Load<T>(ISaveable<T> saveable)
        {
            string json = File.ReadAllText(GetPath(saveable.Key));
            var obj = JsonConvert.DeserializeObject<T>(json, settings);
            saveable.Restore(obj);
        }
    }
}
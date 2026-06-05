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
    
    public static class SaveLoadSystem
    {
#if UNITY_EDITOR
        private static readonly string SAVE_ROOT = Path.Combine(Application.dataPath, "Data", "Save");
#else
        private static readonly string SAVE_ROOT = Path.Combine(Application.persistentDataPath, "Save");
#endif
        
        public static JsonSerializerSettings Settings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            // BaseSO(카탈로그 SO)는 Id로만 직렬화 → 로드 시 DBSORegistry에서 재해석.
            Converters = { new BaseSORefConverter() },
        };

        private static string GetPath(string saveKey)
        {
            return Path.Combine(SAVE_ROOT, $"{saveKey}.json");
        }

        public static void Save(ISaveable saveable)
        {
            Save(saveable.Key, saveable.Data);
        }

        private static void Save(string key, object data)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented, Settings);
            string path = GetPath(key);
            File.WriteAllText(path, json);

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

        public static IEnumerable<ISaveable> ReadAll()
        {
            foreach (var filePath in Directory.EnumerateFiles(SAVE_ROOT, "*.json"))
            {
                string json = File.ReadAllText(filePath);
                var obj = JsonConvert.DeserializeObject(json, Settings);

                yield return obj as ISaveable;
            }
        }

        public static void Load(ISaveable saveable)
        {
            try
            {
                string json = File.ReadAllText(GetPath(saveable.Key));
                Type type = saveable.Data.GetType();
                var obj = JsonConvert.DeserializeObject(json, type, Settings);
                saveable.Restore(obj);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
            }
        }

        public static void Load<T>(ISaveable<T> saveable)
        {
            try
            {
                string json = File.ReadAllText(GetPath(saveable.Key));
                var obj = JsonConvert.DeserializeObject<T>(json, Settings);
                saveable.Restore(obj);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
            }
        }
    }
}
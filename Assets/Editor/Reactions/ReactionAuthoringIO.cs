using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Reactions;

namespace Reactions.Authoring
{
    /// <summary>
    /// 리액션 저작 배관 (Editor 전용): 폴더 보장 / BaseSO 식별자 주입 / 에셋 생성·교체 / DB 등록.
    /// 리액션 내용과 무관하게 모든 리액션이 공유하는 부분이라 한 곳에 모은다.
    /// </summary>
    internal static class ReactionAuthoringIO
    {
        /// <summary>BaseSO 의 private 식별자(_id/_displayName/_description)를 SerializedObject 로 주입.</summary>
        public static void SetBaseSoIds(BaseSO so, string id, string displayName, string description)
        {
            var sob = new SerializedObject(so);
            sob.FindProperty("_id").stringValue = id;
            sob.FindProperty("_displayName").stringValue = string.IsNullOrEmpty(displayName) ? id : displayName;
            sob.FindProperty("_description").stringValue = description ?? "";
            sob.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// folder/{id}.asset 으로 영속화. 이미 있으면 CopySerialized 로 제자리 덮어써 GUID(외부 참조)를 보존한다.
        /// 재실행해도 안전(idempotent).
        /// </summary>
        public static ReactionDefinitionSO Persist(ReactionDefinitionSO so, string folder, string id)
        {
            EnsureFolder(folder);
            string path = $"{folder}/{id}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<ReactionDefinitionSO>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(so, existing); // _id 포함 전 필드 복사, 기존 GUID 유지
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(so); // 임시 인스턴스 정리
                return existing;
            }

            AssetDatabase.CreateAsset(so, path);
            EditorUtility.SetDirty(so);
            return so;
        }

        /// <summary>"ReactionDefinitionDataBase" DB 에셋을 찾고, 없으면 folder 에 생성.</summary>
        public static ReactionDefinitionDataBaseSO FindOrCreateDatabase(string folder)
        {
            var guids = AssetDatabase.FindAssets("t:ReactionDefinitionDataBaseSO");
            if (guids.Length > 0)
            {
                if (guids.Length > 1)
                    Debug.LogWarning($"[ReactionAuthoring] ReactionDefinitionDataBaseSO 가 {guids.Length}개입니다. 첫 번째를 사용합니다.");
                return AssetDatabase.LoadAssetAtPath<ReactionDefinitionDataBaseSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            EnsureFolder(folder);
            var db = ScriptableObject.CreateInstance<ReactionDefinitionDataBaseSO>();
            AssetDatabase.CreateAsset(db, $"{folder}/ReactionDefinitionDataBase.asset");
            Debug.Log($"[ReactionAuthoring] DB 에셋이 없어 새로 생성: {folder}/ReactionDefinitionDataBase.asset");
            return db;
        }

        /// <summary>DB 의 _soList 에 정의들을 등록(Id 기준 중복 시 교체, null 정리).</summary>
        public static void RegisterInDatabase(ReactionDefinitionDataBaseSO db, IEnumerable<ReactionDefinitionSO> defs)
        {
            var sob = new SerializedObject(db);
            var list = sob.FindProperty("_soList");

            foreach (var def in defs)
            {
                if (def == null) continue;

                int found = -1;
                for (int i = 0; i < list.arraySize; i++)
                {
                    var cur = list.GetArrayElementAtIndex(i).objectReferenceValue as ReactionDefinitionSO;
                    if (cur != null && cur.Id == def.Id) { found = i; break; }
                }

                if (found >= 0)
                    list.GetArrayElementAtIndex(found).objectReferenceValue = def;
                else
                {
                    list.arraySize++;
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = def;
                }
            }

            // 깨진(null) 참조 정리 — 뒤에서부터
            for (int i = list.arraySize - 1; i >= 0; i--)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    list.DeleteArrayElementAtIndex(i);

            sob.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(db);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            var parts = folder.Split('/');
            string cur = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{cur}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}

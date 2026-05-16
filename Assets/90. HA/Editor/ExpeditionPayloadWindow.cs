using System.Collections.Generic;
using Bond.Expedition;
using RootVContainer;
using UnityEditor;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class ExpeditionPayloadWindow : EditorWindow
{
    private Vector2 _scroll;

    [System.Serializable]
    private class CharacterSnapshot
    {
        public BaseCharacter data;
    }

    private class CharacterDataWrapper : ScriptableObject
    {
        public CharacterSnapshot character;
    }

    // 캐시: 리스트 헤더 → (wrapper, serializedObject) 목록
    private readonly Dictionary<string, List<(CharacterDataWrapper wrapper, SerializedObject so)>> _cache = new();

    [MenuItem("Tools/Bond/Expedition Payload Debugger")]
    public static void Open() => GetWindow<ExpeditionPayloadWindow>("Expedition Payload");

    private void OnDisable() => ClearCache();

    private void ClearCache()
    {
        foreach (var list in _cache.Values)
            foreach (var (wrapper, _) in list)
                DestroyImmediate(wrapper);
        _cache.Clear();
    }

    private void OnGUI()
    {
        if (Application.isPlaying == false)
        {
            EditorGUILayout.HelpBox("플레이 모드에서만 동작합니다.", MessageType.Info);
            ClearCache();
            return;
        }

        var rootScope = LifetimeScope.Find<RootScope>();
        if (rootScope?.Container == null)
        {
            EditorGUILayout.HelpBox("RootScope를 찾을 수 없습니다.", MessageType.Warning);
            return;
        }

        var payload = rootScope.Container.Resolve<ExpeditionPayload>();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.LabelField("Dungeon ID", string.IsNullOrEmpty(payload.DungeonId) ? "(없음)" : payload.DungeonId, EditorStyles.label);
        EditorGUILayout.LabelField("Outcome", payload.Outcome.ToString(), EditorStyles.label);

        EditorGUILayout.Space();
        DrawCharacterList("Party", payload.Party);

        EditorGUILayout.Space();
        DrawCharacterList("Enemy Party", payload.EnemyParty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Supplies", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        bool hasItem = false;
        foreach (var slot in payload.Supplies.GetAll())
        {
            if (slot.IsEmpty) continue;
            EditorGUILayout.LabelField(slot.item.itemName, $"x{slot.quantity}", EditorStyles.label);
            hasItem = true;
        }
        if (hasItem == false) EditorGUILayout.LabelField("(없음)");
        EditorGUI.indentLevel--;

        EditorGUILayout.EndScrollView();
    }

    private void DrawCharacterList(string header, IReadOnlyList<BaseCharacter> list)
    {
        EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        if (list == null || list.Count == 0)
        {
            EditorGUILayout.LabelField("(없음)");
            EditorGUI.indentLevel--;
            return;
        }

        // 리스트 크기가 달라지면 캐시 무효화
        if (_cache.TryGetValue(header, out var cached) == false || cached.Count != list.Count)
        {
            if (cached != null)
                foreach (var (wrapper, _) in cached)
                    DestroyImmediate(wrapper);

            cached = new List<(CharacterDataWrapper, SerializedObject)>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                var wrapper = ScriptableObject.CreateInstance<CharacterDataWrapper>();
                wrapper.hideFlags = HideFlags.DontSave;
                wrapper.character = new CharacterSnapshot { data = list[i]};
                var so = new SerializedObject(wrapper);
                CollapseRecursive(so.FindProperty("character"));
                cached.Add((wrapper, so));
            }
            _cache[header] = cached;
        }

        for (int i = 0; i < list.Count; i++)
        {
            var (_, so) = cached[i];
            so.Update();
            EditorGUILayout.PropertyField(so.FindProperty("character"), new GUIContent(list[i].Name ?? "(이름 없음)"), true);
        }

        EditorGUI.indentLevel--;
    }

    private static void CollapseRecursive(SerializedProperty prop)
    {
        prop.isExpanded = false;
        var iter = prop.Copy();
        var end = prop.GetEndProperty();
        if (iter.NextVisible(true) == false) return;
        while (SerializedProperty.EqualContents(iter, end) == false)
        {
            if (iter.hasChildren) iter.isExpanded = false;
            if (iter.NextVisible(false) == false) break;
        }
    }
}
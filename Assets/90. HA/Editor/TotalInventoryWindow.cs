using RootVContainer;
using UnityEditor;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class TotalInventoryWindow : EditorWindow
{
    private Vector2 _scroll;

    [MenuItem("Tools/Bond/Total Inventory Debugger")]
    public static void Open() => GetWindow<TotalInventoryWindow>("Total Inventory");

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("플레이 모드에서만 동작합니다.", MessageType.Info);
            return;
        }

        var townScope = LifetimeScope.Find<TownScope>();
        if (townScope?.Container == null)
        {
            EditorGUILayout.HelpBox("TownScope를 찾을 수 없습니다.", MessageType.Warning);
            return;
        }

        var inventory = townScope.Container.Resolve<TotalInventory>();
        var slots = inventory.GetAll();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.LabelField("Total Inventory", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        bool hasItem = false;
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.IsEmpty) continue;
            EditorGUILayout.LabelField($"[{i}] {slot.item.itemName}", $"x{slot.quantity}", EditorStyles.label);
            hasItem = true;
        }
        if (!hasItem) EditorGUILayout.LabelField("(없음)");

        EditorGUI.indentLevel--;
        EditorGUILayout.EndScrollView();
    }
}
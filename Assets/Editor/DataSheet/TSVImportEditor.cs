using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class TSVImportWindow : EditorWindow
{
    [MenuItem("Tools/DataSheet/TSV Import Window", priority = 0)]
    public static void Open() => GetWindow<TSVImportWindow>("TSV Importer");

    // 각 파일의 선택 상태
    private Dictionary<string, bool> _selection = new();
    private Vector2 _scroll;

    private void OnEnable() => RefreshFileList();

    private void RefreshFileList()
    {
        var existing = new HashSet<string>(_selection.Keys);

        // TSV 파일 목록 갱신 (새 파일은 기본 true)
        // var files = Directory.GetFiles(
        //     TSVImportCoordinator.DATA_ROOT, "*.tsv");

        var sheets = TSVImportCoordinator.GetAllEntries().ToArray();

        foreach (var file in sheets)
        {
            string key = file.SheetName.ToLower();
            if (_selection.ContainsKey(key) == false)
                _selection[key] = false;
        }

        // 삭제된 항목 제거
        foreach (var key in existing)
            if (sheets.Any(f => f.SheetName.ToLower() == key) == false)
                _selection.Remove(key);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(4);

        // 전체 선택/해제
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("전체 선택", GUILayout.Width(80)))
            foreach (var k in _selection.Keys.ToList()) _selection[k] = true;
        if (GUILayout.Button("전체 해제", GUILayout.Width(80)))
            foreach (var k in _selection.Keys.ToList()) _selection[k] = false;
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("↻ 목록 갱신", GUILayout.Width(80)))
            RefreshFileList();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // 파일 목록
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var key in _selection.Keys.ToList())
        {
            _selection[key] = EditorGUILayout.ToggleLeft(key, _selection[key]);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(4);

        bool anySelected = _selection.Values.Any(v => v);
        GUI.enabled = anySelected;

        if (GUILayout.Button("선택한 시트 다운로드", GUILayout.Height(30)))
            RunDownload();

        if (GUILayout.Button("선택한 시트 임포트", GUILayout.Height(30)))
            RunImport();

        if (GUILayout.Button("선택한 시트 다운로드 + 임포트", GUILayout.Height(30)))
            RunDownloadAndImport();

        GUI.enabled = true;
    }

    private IEnumerable<string> Selected()
        => _selection.Where(kv => kv.Value).Select(kv => kv.Key);

    private void RunDownload()
    {
        TSVImportCoordinator.Download(Selected());
    }
    
    private void RunImport()
    {
        TSVImportCoordinator.Import(Selected());
    }

    private void RunDownloadAndImport()
    {
        TSVImportCoordinator.Download(Selected());
        TSVImportCoordinator.Import(Selected());
    }
}
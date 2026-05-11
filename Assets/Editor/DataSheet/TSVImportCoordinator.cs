using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

public static class TSVImportCoordinator
{
    public const string DATA_ROOT   = "Assets/Data/TSV/";
    private const string OUTPUT_ROOT = "Assets/Data/GeneratedSO/";
    private const string CONFIG_PATH = "Assets/Data/GoogleSheetConfig/";

    private static Dictionary<string, ITSVParser> BuildParserMap()
    {
        return System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(ITSVParser).IsAssignableFrom(t)
                        && !t.IsAbstract && !t.IsInterface)
            .Select(t => (ITSVParser)System.Activator.CreateInstance(t))
            .ToDictionary(p => p.TargetFileName.ToLower());
    }

    public static IEnumerable<GoogleSheetEntry> GetAllEntries()
    {
        var configs = Directory.GetFiles(CONFIG_PATH, "*.asset");

        StringBuilder debugStr = new StringBuilder();
        
        debugStr.AppendLine($"Found {configs.Length} Configs");
        foreach (var config in configs)
            debugStr.AppendLine(config);
        Debug.Log(debugStr.ToString());
        debugStr.Clear();

        if (configs.Length == 0)
        {
            Debug.LogError($"GoogleSheetConfig not found at: {CONFIG_PATH}");
            return null;
        }

        var list = new List<GoogleSheetEntry>();
        
        foreach (var file in configs)
        {
            var entries = AssetDatabase.LoadAssetAtPath<GoogleSheetConfig>(file).GetEntries();
            list.AddRange(entries);
        }

        var comparer = new GoogleSheetEntry.SheetPageComparer();
        var distinct = list.Distinct(comparer).ToList();
        var duplicated = list.Except(distinct).ToList();
        
        if (duplicated.Count > 0)
        {
            debugStr.AppendLine($"Excluded due to duplication:");
            foreach (var entry in duplicated)
                debugStr.AppendLine($"{entry}");
            Debug.LogWarning(debugStr.ToString());
            debugStr.Clear();
        }

        debugStr.AppendLine($"Found {distinct.Count} Entries");
        foreach (var entry in distinct)
            debugStr.AppendLine(entry.ToString());
        Debug.Log(debugStr);

        return distinct;
    }

    [MenuItem("Tools/DataSheet/Download from Google Sheets")]
    public static void DownloadAll()
    {
        var entries = GetAllEntries();
        if (entries == null)
            return;

        GoogleSheetDownloader.DownloadAll(entries, DATA_ROOT);
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/DataSheet/Download and Import All")]
    public static void DownloadAndImport()
    {
        DownloadAll();
        ImportAll();
    }

    [MenuItem("Tools/DataSheet/Import All TSV Data")]
    public static void ImportAll() => Import(null); // null = 전체

    // 선택된 파일명 목록만 처리
    public static void Import(IEnumerable<string> targets)
    {
        var parsers = BuildParserMap();
        var files   = Directory.GetFiles(DATA_ROOT, "*.tsv");

        // targets가 null이면 전체, 아니면 필터링
        var targetSet = targets != null
            ? new HashSet<string>(targets.Select(t => t.ToLower()))
            : null;

        foreach (var file in files)
        {
            string key = Path.GetFileNameWithoutExtension(file).ToLower();

            if (targetSet != null && targetSet.Contains(key) == false) continue;

            if (parsers.TryGetValue(key, out var parser) == false)
            {
                Debug.LogWarning($"파서 없음: {key}.tsv");
                continue;
            }

            string outDir = $"{OUTPUT_ROOT}{key}/";
            if (Directory.Exists(outDir) == false) Directory.CreateDirectory(outDir);

            parser.ParseAndImport(file, outDir);
        }

        AssetDatabase.Refresh();
    }

    // 다운로드도 동일하게
    public static void Download(IEnumerable<string> targets = null)
    {
        var entries = GetAllEntries();
        if (entries == null)
            return;

        var targetSet = targets != null
            ? new HashSet<string>(targets.Select(t => t.ToLower()))
            : null;

        foreach (var entry in entries)
        {
            if (targetSet != null && targetSet.Contains(entry.SheetName.ToLower()) == false) continue;

            string savePath = Path.Combine(DATA_ROOT, $"{entry.SheetName}.tsv");
            GoogleSheetDownloader.Download(entry, savePath);
        }

        AssetDatabase.Refresh();
    }
}
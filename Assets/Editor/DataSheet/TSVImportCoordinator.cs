using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class TSVImportCoordinator
{
    public const string DATA_ROOT   = "Assets/Data/TSV/";
    private const string OUTPUT_ROOT = "Assets/Data/GeneratedSO/";
    private const string CONFIG_PATH = "Assets/Data/GoogleSheetConfig.asset";

    private static Dictionary<string, ITSVParser> BuildParserMap()
    {
        return System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(ITSVParser).IsAssignableFrom(t)
                        && !t.IsAbstract && !t.IsInterface)
            .Select(t => (ITSVParser)System.Activator.CreateInstance(t))
            .ToDictionary(p => p.TargetFileName.ToLower());
    }

    [MenuItem("Tools/DataSheet/Download from Google Sheets")]
    public static void DownloadOnly()
    {
        var config = AssetDatabase.LoadAssetAtPath<GoogleSheetConfig>(CONFIG_PATH);
        if (config == null)
        {
            Debug.LogError($"GoogleSheetConfig not found at: {CONFIG_PATH}");
            return;
        }

        GoogleSheetDownloader.DownloadAll(config, DATA_ROOT);
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/DataSheet/Download and Import All")]
    public static void DownloadAndImport()
    {
        DownloadOnly();
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
    public static void DownloadAndImport(IEnumerable<string> targets = null)
    {
        var config = AssetDatabase.LoadAssetAtPath<GoogleSheetConfig>(CONFIG_PATH);
        if (config == null) { Debug.LogError("GoogleSheetConfig not found."); return; }

        var targetSet = targets != null
            ? new HashSet<string>(targets.Select(t => t.ToLower()))
            : null;

        foreach (var entry in config.sheets)
        {
            if (targetSet != null && targetSet.Contains(entry.sheetName.ToLower()) == false) continue;

            string savePath = Path.Combine(DATA_ROOT, $"{entry.sheetName}.tsv");
            GoogleSheetDownloader.Download(config, entry, savePath);
        }

        AssetDatabase.Refresh();
        Import(targets);
    }
}
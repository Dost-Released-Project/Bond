using System.Collections.Generic;
using System.IO;
using CsvHelper.Configuration;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class TutorialDTO
{
    public string ID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public TutorialSequence SequenceType { get; set; }
    public string TargetUiKey { get; set; }
    public int Frontier { get; set; }
    public int Wood { get; set; }
    public int Ore { get; set; }
    public string ItemIds { get; set; }
    public string ItemCounts { get; set; }
}

public class TutorialParser : TSVParserBase<TutorialDTO, TutorialStepSO>
{
    public override string TargetFileName => "tutorial";
    protected override string GetAssetName(TutorialDTO dto) => $"TutorialStepSO/{dto.ID}";
    protected override ClassMap<TutorialDTO> GetClassMap() => new TutorialMap();
    
    protected override void Populate(TutorialStepSO so, TutorialDTO dto)
    {
        so.SetData(dto.ID, dto.Name, dto.Description, dto.SequenceType, dto.TargetUiKey, dto.Frontier, dto.Wood ,dto.Ore, dto.ItemIds, dto.ItemCounts);
    }

    protected override void OnPostImport(string outputDir)
    {
        string subDir = Path.Combine(outputDir, "TutorialStepSO").Replace("\\", "/");
        if (subDir.EndsWith("/")) subDir = subDir.Substring(0, subDir.Length - 1);

        string[] guids = AssetDatabase.FindAssets("t:TutorialStepSO", new[] { subDir });
        var assets = new List<BaseSO>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<TutorialStepSO>(path);
            if (asset != null) assets.Add(asset);
        }

        string dbPath = Path.Combine(outputDir, "TutorialStep.asset").Replace("\\", "/");
        var db = AssetDatabase.LoadAssetAtPath<TutorialDataBaseSO>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<TutorialDataBaseSO>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        var field = typeof(DataBaseSO).GetField("_soList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(db, assets);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssetIfDirty(db);

        AddressableHelper.RegisterToAddressable(dbPath);

        Debug.Log($"[SupplyParser] 통합 보급 DB 업데이트 완료: {assets.Count}개 번들 조립 완료 (경로: {dbPath})");
    }
}

public sealed class TutorialMap : ClassMap<TutorialDTO>
{
    public TutorialMap()
    {
        Map(m => m.ID).Name("ID");
        Map(m => m.Name).Name("Name");
        Map(m => m.Description).Name("Description");
        Map(m => m.SequenceType).Name("SequenceType");
        Map(m => m.TargetUiKey).Name("TargetUiKey");
        Map(m => m.Frontier).Name("Frontier");
        Map(m => m.Wood).Name("Wood");
        Map(m => m.Ore).Name("Ore");
        Map(m => m.ItemIds).Name("ItemIds");
        Map(m => m.ItemCounts).Name("ItemCounts");
    }
}
using System.Collections.Generic;
using System.IO;
using CsvHelper.Configuration;
using UnityEditor;
using UnityEngine;

public class StatModifierDTO
{
    public string ID { get; set; }
    public string Name { get; set; }
    public StatType Type { get; set; }
    public ModifierMode Mode { get; set; }
    public float Value { get; set; }
}

public class StatModifierParser : TSVParserBase<StatModifierDTO, StatModifierDataSO>
{
    public override string TargetFileName => "statModifier";
    protected override string GetAssetName(StatModifierDTO dto) => $"StatModifierDataSO/MOD_{dto.ID}";
    protected override ClassMap<StatModifierDTO> GetClassMap() => new StatModifierMap();

    protected override void Populate(StatModifierDataSO so, StatModifierDTO dto)
    {
        so.SetData(dto.ID, dto.Name, dto.Type, dto.Mode, dto.Value);
    }

    protected override void OnPostImport(string outputDir)
    {
        // 1. 하위 데이터(StatModifierDataSO)가 저장된 경로 설정 및 정리
        string subDir = Path.Combine(outputDir, "StatModifierDataSO").Replace("\\", "/");
        if (subDir.EndsWith("/")) subDir = subDir.Substring(0, subDir.Length - 1);

        // 2. 해당 경로에서 생성된 모든 StatModifierDataSO 에셋 검색
        string[] guids = AssetDatabase.FindAssets("t:StatModifierDataSO", new[] { subDir });
        var assets = new List<BaseSO>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<StatModifierDataSO>(path);
            if (asset != null) assets.Add(asset);
        }

        // 3. 통합 데이터베이스(StatModifierDataBaseSO) 에셋 로드 또는 생성
        string dbPath = Path.Combine(outputDir, "StatModifierDataBase.asset").Replace("\\", "/");
        var db = AssetDatabase.LoadAssetAtPath<StatModifierDataBaseSO>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<StatModifierDataBaseSO>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        // 4. 리플렉션을 이용해 DataBaseSO의 _soList 필드에 데이터 주입
        var field = typeof(DataBaseSO).GetField("_soList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(db, assets);

        // 5. 변경사항 저장
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssetIfDirty(db);

        // 6. 어드레서블 자동 등록 (DB 에셋을 등록하여 런타임에 ID로 검색 가능하게 함)
        AddressableHelper.RegisterToAddressable(dbPath);

        Debug.Log($"[StatModifierParser] 통합 DB 업데이트 완료: {assets.Count}개 등록 (경로: {dbPath})");
    }
}

public sealed class StatModifierMap : ClassMap<StatModifierDTO>
{
    public StatModifierMap() {
        Map(m => m.ID).Name("ID");
        Map(m => m.Name).Name("Name");
        Map(m => m.Type).Name("Type");
        Map(m => m.Mode).Name("Mode");
        Map(m => m.Value).Name("Value");
    }
}
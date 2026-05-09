using System.Collections.Generic;
using System.IO;
using CsvHelper.Configuration;
using UnityEditor;
using UnityEngine;

/// <summary>
/// TSV 데이터와 매핑될 DTO 클래스.
/// </summary>
public class SkillDTO
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public SkillType Type { get; set; }
    public SkillTarget Target { get; set; }
    public float Value { get; set; }
    public int CoolTime { get; set; }
    public int Duration { get; set; }
    public int UseableClasses { get; set; }
    public int UseableSlots { get; set; }
    public int EnemyTargetMask { get; set; }
    public int AllyTargetMask { get; set; }
    public string IconAddress { get; set; }
}

/// <summary>
/// SkillData SO를 생성하고 Skill/SkillDataSO/ 폴더에 저장하며, 
/// Skill/ 폴더의 SkillDataBaseSO에 자동 등록하는 파서.
/// </summary>
public class SkillDataParser : TSVParserBase<SkillDTO, SkillData>
{
    // TSV 파일명을 'Skill.tsv'로 가정합니다.
    public override string TargetFileName => "Skill";

    // 개별 SO는 SkillDataSO 하위 폴더로 지정
    protected override string GetAssetName(SkillDTO dto) => $"SkillDataSO/SK_{dto.Id}";

    protected override ClassMap<SkillDTO> GetClassMap() => new SkillDataMap();

    protected override void Populate(SkillData so, SkillDTO dto)
    {
        var raw = new SkillRawData
        {
            SkillId = dto.Id,
            SkillName = dto.DisplayName,
            Description = dto.Description,
            Type = dto.Type,
            Target = dto.Target,
            Value = dto.Value,
            CoolTime = dto.CoolTime,
            Duration = dto.Duration,
            UseableClasses = dto.UseableClasses,
            UseableSlots = dto.UseableSlots,
            EnemyTargetMask = dto.EnemyTargetMask,
            AllyTargetMask = dto.AllyTargetMask,
            IconAddress = dto.IconAddress
        };

        so.SetData(raw);
    }

    protected override void OnPostImport(string outputDir)
    {
        // 1. SkillDataSO 하위 폴더에서 모든 SkillData 에셋 수집
        string subDir = Path.Combine(outputDir, "SkillDataSO");
        if (!Directory.Exists(subDir)) return;

        var assetPaths = Directory.GetFiles(subDir, "*.asset", SearchOption.TopDirectoryOnly);
        var skillAssets = new List<BaseSO>();

        foreach (var path in assetPaths)
        {
            var skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (skill != null) skillAssets.Add(skill);
        }

        // 2. 상위 폴더(outputDir)에 통합 DB 에셋 찾기 또는 생성
        string dbPath = Path.Combine(outputDir, "SkillDataBase.asset");
        var db = AssetDatabase.LoadAssetAtPath<SkillDataBaseSO>(dbPath);

        if (db == null)
        {
            db = ScriptableObject.CreateInstance<SkillDataBaseSO>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        // 3. 리스트 업데이트
        db.SetSOList(skillAssets);
        
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        // 어드레서블 자동 등록 추가
        AddressableHelper.RegisterToAddressable(dbPath);
        
        Debug.Log($"[SkillDataParser] 통합 DB 업데이트 완료: {skillAssets.Count}개의 스킬을 Skill/SkillDataSO/ 에서 수집하여 등록");
    }
}

public sealed class SkillDataMap : ClassMap<SkillDTO>
{
    public SkillDataMap()
    {
        Map(m => m.Id).Name("ID");
        Map(m => m.DisplayName).Name("Name");
        Map(m => m.Description).Name("Description");
        Map(m => m.Type).Name("Type");
        Map(m => m.Target).Name("Target");
        Map(m => m.Value).Name("Value");
        Map(m => m.CoolTime).Name("CoolTime");
        Map(m => m.Duration).Name("Duration");
        Map(m => m.UseableClasses).Name("UseableClasses");
        Map(m => m.UseableSlots).Name("UseableSlots");
        Map(m => m.EnemyTargetMask).Name("EnemyTargetMask");
        Map(m => m.AllyTargetMask).Name("AllyTargetMask");
        Map(m => m.IconAddress).Name("IconAddress");
    }
}

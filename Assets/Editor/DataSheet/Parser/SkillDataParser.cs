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
        string subDir = Path.Combine(outputDir, "SkillDataSO").Replace("\\", "/");
        if (subDir.EndsWith("/")) subDir = subDir.Substring(0, subDir.Length - 1);

        string[] guids = AssetDatabase.FindAssets("t:SkillData", new[] { subDir });
        var skillAssets = new List<BaseSO>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
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
        AssetDatabase.SaveAssetIfDirty(db);

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
        Map(m => m.DisplayName).Name("Name").Optional(); 
        Map(m => m.DisplayName).Name("DisplayName").Optional(); 
        Map(m => m.Description).Name("Description").Optional();
        Map(m => m.Type).Name("Type").Default(SkillType.OFFENSIVE);
        Map(m => m.Target).Name("Target").Default(SkillTarget.Enemy);
        Map(m => m.Value).Name("Value", " Value").Default(0f); // 공백 포함 헤더 대응
        Map(m => m.CoolTime).Name("CoolTime").Default(0);
        Map(m => m.Duration).Name("지속 시간").Default(0);
        Map(m => m.UseableClasses).Name("Useable").Default(0);
        
        // 비트마스크 필드: 2진수 문자열(0011, 1100 등)을 실제 정수로 변환
        Map(m => m.UseableSlots).Name("사용 가능 칸").Default(0).TypeConverter<BinaryMaskConverter>();
        Map(m => m.EnemyTargetMask).Name("적 진영").Default(0).TypeConverter<BinaryMaskConverter>();
        Map(m => m.AllyTargetMask).Name("아군 진영").Default(0).TypeConverter<BinaryMaskConverter>();
        
        Map(m => m.IconAddress).Name("아이콘 ID").Optional();
    }

    /// <summary>
    /// TSV의 2진수 문자열("0011")을 정수 비트마스크(3)로 변환하는 컨버터
    /// </summary>
    private class BinaryMaskConverter : CsvHelper.TypeConversion.DefaultTypeConverter
    {
        public override object ConvertFromString(string text, CsvHelper.IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            
            string cleaned = text.Trim();
            try
            {
                // 2진수 포맷인 경우 변환 시도
                return System.Convert.ToInt32(cleaned, 2);
            }
            catch
            {
                // 2진수 변환 실패 시 일반 10진수 숫자로 재시도
                if (int.TryParse(cleaned, out int result)) return result;
                return 0;
            }
        }
    }
}

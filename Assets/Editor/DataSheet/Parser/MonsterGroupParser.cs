using System.Collections.Generic;
using System.IO;
using CsvHelper.Configuration;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MonsterGroup.tsv 를 파싱하여 MonsterGroupData ScriptableObject 를 생성하고
/// MonsterGroupDataBaseSO 에 자동 등록하는 파서.
/// </summary>
public class MonsterGroupParser : TSVParserBase<MonsterGroupDTO, MonsterGroupData>
{
    public override string TargetFileName => "MonsterGroup";

    protected override string GetAssetName(MonsterGroupDTO dto) => $"MonsterGroupDataSO/MG_{dto.Id}";

    protected override ClassMap<MonsterGroupDTO> GetClassMap() => new MonsterGroupDataMap();

    protected override void Populate(MonsterGroupData so, MonsterGroupDTO dto)
    {
        // 세미콜론 구분 문자열 → List<string> 변환
        // 명시적 반복문 사용: 람다 사용 시 근거 주석 필수 (코딩 컨벤션)
        List<string> monsterIds = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.MonsterIdsRaw) == false)
        {
            string[] split = dto.MonsterIdsRaw.Split(';');
            foreach (string entry in split)
            {
                string trimmed = entry.Trim();
                if (string.IsNullOrEmpty(trimmed) == false)
                {
                    monsterIds.Add(trimmed);
                }
            }
        }

        so.SetData(
            id:          dto.Id,
            displayName: dto.DisplayName,
            description: dto.Description,
            monsterIds:  monsterIds,
            minLayer:    dto.MinLayer,
            maxLayer:    dto.MaxLayer,
            isElite:     dto.IsElite
        );
    }

    protected override void OnPostImport(string outputDir)
    {
        string subDir = Path.Combine(outputDir, "MonsterGroupDataSO");
        if (Directory.Exists(subDir) == false) return;

        string[] assetPaths = Directory.GetFiles(subDir, "*.asset", SearchOption.TopDirectoryOnly);
        List<BaseSO> assets = new List<BaseSO>();

        foreach (string path in assetPaths)
        {
            MonsterGroupData asset = AssetDatabase.LoadAssetAtPath<MonsterGroupData>(path);
            if (asset != null) assets.Add(asset);
        }

        string dbPath = Path.Combine(outputDir, "MonsterGroupDataBase.asset");
        MonsterGroupDataBaseSO db = AssetDatabase.LoadAssetAtPath<MonsterGroupDataBaseSO>(dbPath);

        if (db == null)
        {
            db = ScriptableObject.CreateInstance<MonsterGroupDataBaseSO>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        // DataBaseSO._soList 는 private 이므로 리플렉션으로 주입한다.
        // 팀 공통 방식: BuildingParser, AccessoryParser, ConsumableParser 와 동일.
        System.Reflection.FieldInfo field = typeof(DataBaseSO).GetField(
            "_soList",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        field?.SetValue(db, assets);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        AddressableHelper.RegisterToAddressable(dbPath);

        Debug.Log($"[MonsterGroupParser] 통합 DB 업데이트 완료: {assets.Count}개 등록");
    }
}

/// <summary>
/// MonsterGroup.tsv 컬럼 매핑.
/// </summary>
public sealed class MonsterGroupDataMap : ClassMap<MonsterGroupDTO>
{
    public MonsterGroupDataMap()
    {
        Map(m => m.Id).Name("ID");
        Map(m => m.DisplayName).Name("DisplayName").Optional();
        Map(m => m.Description).Name("Description").Optional();
        Map(m => m.MonsterIdsRaw).Name("MonsterIds").Optional();
        Map(m => m.MinLayer).Name("MinLayer").Default(0);
        Map(m => m.MaxLayer).Name("MaxLayer").Default(0);
        Map(m => m.IsElite).Name("IsElite").Default(false);
    }
}

/// <summary>
/// MonsterGroup.tsv 한 행을 담는 DTO.
/// MonsterIds 는 세미콜론 구분 문자열로 수신 후 Populate 에서 분리한다.
/// </summary>
public class MonsterGroupDTO
{
    public string Id            { get; set; }
    public string DisplayName   { get; set; }
    public string Description   { get; set; }
    public string MonsterIdsRaw { get; set; } // "MO00001;MO00002" 형태
    public int    MinLayer      { get; set; }
    public int    MaxLayer      { get; set; }
    public bool   IsElite       { get; set; }
}

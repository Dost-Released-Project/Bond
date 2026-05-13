using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using UnityEditor;
using UnityEngine;

public class BuildingDTO
{
    public string ID { get; set; }
    public string BuildingName { get; set; }
    public BuildingType BuildingType { get; set; }
    public string Description { get; set; }
    public string SpritePath { get; set; }
}

public class BuildingLevelDTO
{
    public string ID { get; set; }
    public int Level { get; set; }
    public int FrontierCost { get; set; }
    public int WoodCost { get; set; }
    public int OreCost { get; set; }
    public int EffectValue { get; set; }
    public int SlotExpansion { get; set; }
    public int FrontierCapAdd { get; set; }
    public int MaterialCapAdd { get; set; }
}

public class BuildingParser : TSVParserBase<BuildingDTO, BuildingData>
{
    public override string TargetFileName => "Building"; // Building.tsv 로드

    protected override string GetAssetName(BuildingDTO dto) => $"BuildingDataSO/BU_{dto.ID}";

    protected override ClassMap<BuildingDTO> GetClassMap() => new BuildingMap();

    private Dictionary<string, List<BuildingLevelData>> _levelCache;

    // 레벨 테이블(BuildingLevel.tsv)을 수동으로 로드하는 메서드
    private void EnsureLevelCacheLoaded()
    {
        if (_levelCache != null) return;
        _levelCache = new Dictionary<string, List<BuildingLevelData>>();

        string levelPath = Path.Combine(Application.dataPath, "Data/TSV/BuildingLevel.tsv");
        if (!File.Exists(levelPath)) return;

        string[] lines = File.ReadAllLines(levelPath);

        // 1. 엑셀 1~8행(인덱스 0~7) 무시하고 9행(인덱스 8)부터 가져옴
        var filteredLines = lines.Skip(8).ToList(); 
        
        string trimmed = string.Join("\n", filteredLines);

        using var reader = new StringReader(trimmed);
        using var csv = new CsvReader(reader, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            Delimiter = "\t",
            HasHeaderRecord = true, // 이제 첫 줄이 "BuildingID	Level..." 헤더임
            HeaderValidated = null,
            MissingFieldFound = null,
        });

        // [중요] 맵핑 클래스의 이름이 TSV 헤더와 정확히 일치해야 합니다.
        // 이미지에 BuildingID라고 되어있으므로 Map도 확인해야 합니다.
        csv.Context.RegisterClassMap<BuildingLevelMap>();
        var records = csv.GetRecords<BuildingLevelDTO>().ToList();

        foreach (var rec in records)
        {
            if (string.IsNullOrEmpty(rec.ID)) continue;
        
            string cleanID = rec.ID.Trim();
            if (!_levelCache.ContainsKey(cleanID))
                _levelCache[cleanID] = new List<BuildingLevelData>();

            _levelCache[cleanID].Add(new BuildingLevelData
            {
                level = rec.Level,
                frontierCost = rec.FrontierCost,
                woodCost = rec.WoodCost,
                oreCost = rec.OreCost,
                effectValue = rec.EffectValue,
                slotExpansion = rec.SlotExpansion,
                frontierCapAdd = rec.FrontierCapAdd,
                materialCapAdd = rec.MaterialCapAdd
            });
        }
        Debug.Log($"[BuildingParser] 레벨 캐시 로드 완료. 총 {_levelCache.Count}개 건물의 데이터 저장됨.");
    }
    protected override void Populate(BuildingData so, BuildingDTO dto)
    {
        if (dto == null || string.IsNullOrEmpty(dto.ID)) return;

        EnsureLevelCacheLoaded();

        string cleanDTOID = dto.ID.Trim(); // [보완] 공백 제거
        Sprite sprite = null;
        if (!string.IsNullOrEmpty(dto.SpritePath))
        {
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/{dto.SpritePath}.png");
        }

        List<BuildingLevelData> myLevels = new List<BuildingLevelData>();
        if (_levelCache != null && _levelCache.TryGetValue(cleanDTOID, out var foundLevels))
        {
            myLevels = foundLevels;
        }
        else
        {
            // [중요 로그] 여기서 로그가 찍힌다면 ID가 서로 안 맞는 겁니다.
            Debug.LogWarning($"[BuildingParser] ID {cleanDTOID} 에 해당하는 레벨 데이터를 찾을 수 없습니다!");
        }

        so.SetBuildingData(dto.ID, dto.BuildingName, dto.Description, dto.BuildingType, myLevels, sprite);
    }

    protected override void OnPostImport(string outputDir)
    {
        // DB 업데이트 로직 (BuildingDataBaseSO가 있어야 함)
        string subDir = Path.Combine(outputDir, "BuildingDataSO").Replace("\\", "/");
        if (subDir.EndsWith("/")) subDir = subDir.Substring(0, subDir.Length - 1);

        string[] guids = AssetDatabase.FindAssets("t:BuildingData", new[] { subDir });
        var assets = new List<BaseSO>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
            if (asset != null) assets.Add(asset);
        }

        string dbPath = Path.Combine(outputDir, "BuildingDataBase.asset");
        var db = AssetDatabase.LoadAssetAtPath<BuildingDataBaseSO>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<BuildingDataBaseSO>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        // 리플렉션으로 _soList 채우기 (팀 공통 방식)
        var field = typeof(DataBaseSO).GetField("_soList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(db, assets);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssetIfDirty(db);
        Debug.Log($"[BuildingParser] 건물 DB 통합 완료: {assets.Count}개 등록");
    }
}

public sealed class BuildingMap : ClassMap<BuildingDTO>
{
    public BuildingMap()
    {
        Map(m => m.ID).Name("ID");
        Map(m => m.BuildingName).Name("BuildingName");
        Map(m => m.BuildingType).Name("BuildingType");
        Map(m => m.Description).Name("Description");
        Map(m => m.SpritePath).Name("SpritePath");
    }
}

public sealed class BuildingLevelMap : ClassMap<BuildingLevelDTO>
{
    public BuildingLevelMap()
    {
        Map(m => m.ID).Name("ID");
        Map(m => m.Level).Name("Level");
        Map(m => m.FrontierCost).Name("FrontierCost");
        Map(m => m.WoodCost).Name("WoodCost");
        Map(m => m.OreCost).Name("OreCost");
        Map(m => m.EffectValue).Name("EffectValue");
        Map(m => m.SlotExpansion).Name("SlotExpansion");
        Map(m => m.FrontierCapAdd).Name("FrontierCapAdd");
        Map(m => m.MaterialCapAdd).Name("MaterialCapAdd");
    }
}
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using UnityEditor;
using UnityEngine;

/// <summary>
/// EventData.tsv 를 파싱하여 EventData ScriptableObject 를 생성하고
/// EventDataBaseSO 에 자동 등록하는 파서.
/// EventData.tsv : 이벤트 기본 정보 (ID, DisplayName, Description)
/// EventChoice.tsv : 선택지 및 효과 상세 정보 (EventId 외래키로 EventData 와 연결)
/// </summary>
public class EventDataParser : TSVParserBase<EventDataDTO, EventData>
{
    public override string TargetFileName => "EventData";

    protected override string GetAssetName(EventDataDTO dto) => $"EventDataSO/EV_{dto.Id}";

    protected override ClassMap<EventDataDTO> GetClassMap() => new EventDataMap();

    // EventChoice 캐시: EventId → List<EventChoice>
    private Dictionary<string, List<EventChoice>> _choiceCache;

    /// <summary>
    /// EventChoice.tsv 를 수동 파싱하여 _choiceCache 를 구성한다.
    /// BuildingParser.EnsureLevelCacheLoaded() 와 동일한 패턴.
    /// </summary>
    private void EnsureChoiceCacheLoaded()
    {
        if (_choiceCache != null) return;

        _choiceCache = new Dictionary<string, List<EventChoice>>();

        string choicePath = Path.Combine(
            Application.dataPath, "Data/TSV/EventChoice.tsv"
        );

        if (File.Exists(choicePath) == false)
        {
            Debug.LogWarning($"[EventDataParser] EventChoice.tsv 를 찾을 수 없습니다: {choicePath}");
            return;
        }

        CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter             = "\t",
            Mode                  = CsvMode.NoEscape,
            HasHeaderRecord       = true,
            HeaderValidated       = null,
            MissingFieldFound     = null,
            PrepareHeaderForMatch = args => args.Header.Trim(),
        };

        // TSVParserBase 와 동일한 헤더 스킵 규칙 적용 (8줄 스킵)
        string[] lines = File.ReadAllLines(choicePath);
        List<string> filteredLines = lines.Skip(8).ToList();
        string trimmedContent = string.Join("\n", filteredLines);

        List<EventChoiceDTO> records;
        using (StringReader reader = new StringReader(trimmedContent))
        using (CsvReader csv = new CsvReader(reader, config))
        {
            csv.Context.RegisterClassMap<EventChoiceMap>();
            records = csv.GetRecords<EventChoiceDTO>().ToList();
        }

        foreach (EventChoiceDTO dto in records)
        {
            if (_choiceCache.ContainsKey(dto.EventId) == false)
            {
                _choiceCache[dto.EventId] = new List<EventChoice>();
            }

            // 세미콜론 구분 아이템 풀 분리
            List<string> itemPool = new List<string>();
            if (string.IsNullOrWhiteSpace(dto.ItemPoolRaw) == false)
            {
                string[] poolEntries = dto.ItemPoolRaw.Split(';');
                foreach (string entry in poolEntries)
                {
                    string poolEntry = entry.Trim();
                    if (string.IsNullOrEmpty(poolEntry) == false)
                    {
                        itemPool.Add(poolEntry);
                    }
                }
            }

            EventEffectData effect = new EventEffectData();
            effect.SetData(
                effectType:        dto.EffectType,
                targetType:        dto.TargetType,
                hpChangeAmount:    dto.HpChangeAmount,
                statusEffectId:    dto.StatusEffectId,
                itemRewardType:    dto.ItemRewardType,
                guaranteedItemId:  dto.GuaranteedItemId,
                itemProbability:   dto.ItemProbability,
                probabilityItemId: dto.ProbabilityItemId,
                itemPool:          itemPool
            );

            EventChoice choice = new EventChoice();
            choice.SetData(dto.Label, dto.OutcomeDescription, effect);

            _choiceCache[dto.EventId].Add(choice);
        }
    }

    protected override void Populate(EventData so, EventDataDTO dto)
    {
        EnsureChoiceCacheLoaded();

        List<EventChoice> choices = _choiceCache.ContainsKey(dto.Id)
            ? _choiceCache[dto.Id]
            : new List<EventChoice>();

        so.SetData(dto.Id, dto.DisplayName, dto.Description, choices);
    }

    protected override void OnPostImport(string outputDir)
    {
        string subDir = Path.Combine(outputDir, "EventDataSO");
        if (Directory.Exists(subDir) == false) return;

        string[] assetPaths = Directory.GetFiles(subDir, "*.asset", SearchOption.TopDirectoryOnly);
        List<BaseSO> assets = new List<BaseSO>();

        foreach (string path in assetPaths)
        {
            EventData asset = AssetDatabase.LoadAssetAtPath<EventData>(path);
            if (asset != null) assets.Add(asset);
        }

        string dbPath = Path.Combine(outputDir, "EventDataBase.asset");
        EventDataBaseSO db = AssetDatabase.LoadAssetAtPath<EventDataBaseSO>(dbPath);

        if (db == null)
        {
            db = ScriptableObject.CreateInstance<EventDataBaseSO>();
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

        Debug.Log($"[EventDataParser] 통합 DB 업데이트 완료: {assets.Count}개 등록");
    }
}

/// <summary>
/// EventData.tsv 한 행 DTO.
/// </summary>
public class EventDataDTO
{
    public string Id          { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
}

/// <summary>
/// EventChoice.tsv 한 행 DTO.
/// ItemPool 은 세미콜론 구분 문자열로 수신 후 EnsureChoiceCacheLoaded 내부에서 분리한다.
/// </summary>
public class EventChoiceDTO
{
    public string         EventId            { get; set; }
    public string         Label              { get; set; }
    public string         OutcomeDescription { get; set; }
    public EffectType     EffectType         { get; set; }
    public TargetType     TargetType         { get; set; }
    public int            HpChangeAmount     { get; set; }
    public string         StatusEffectId     { get; set; }
    public ItemRewardType ItemRewardType     { get; set; }
    public string         GuaranteedItemId   { get; set; }
    public float          ItemProbability    { get; set; }
    public string         ProbabilityItemId  { get; set; }
    public string         ItemPoolRaw        { get; set; } // "IT00001;IT00002" 형태
}

/// <summary>
/// EventData.tsv 컬럼 매핑.
/// </summary>
public sealed class EventDataMap : ClassMap<EventDataDTO>
{
    public EventDataMap()
    {
        Map(m => m.Id).Name("ID");
        Map(m => m.DisplayName).Name("DisplayName").Optional();
        Map(m => m.Description).Name("Description").Optional();
    }
}

/// <summary>
/// EventChoice.tsv 컬럼 매핑.
/// </summary>
public sealed class EventChoiceMap : ClassMap<EventChoiceDTO>
{
    public EventChoiceMap()
    {
        Map(m => m.EventId).Name("EventId");
        Map(m => m.Label).Name("Label").Optional();
        Map(m => m.OutcomeDescription).Name("OutcomeDescription").Optional();
        Map(m => m.EffectType).Name("EffectType").Default(EffectType.None);
        Map(m => m.TargetType).Name("TargetType").Default(TargetType.None);
        Map(m => m.HpChangeAmount).Name("HpChangeAmount").Default(0);
        Map(m => m.StatusEffectId).Name("StatusEffectId").Optional();
        Map(m => m.ItemRewardType).Name("ItemRewardType").Default(ItemRewardType.Guaranteed);
        Map(m => m.GuaranteedItemId).Name("GuaranteedItemId").Optional();
        Map(m => m.ItemProbability).Name("ItemProbability").Default(0f);
        Map(m => m.ProbabilityItemId).Name("ProbabilityItemId").Optional();
        Map(m => m.ItemPoolRaw).Name("ItemPool").Optional();
    }
}

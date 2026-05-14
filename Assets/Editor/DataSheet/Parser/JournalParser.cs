using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bond.WT.Journal;
using CsvHelper.Configuration;
using UnityEditor;
using UnityEngine;

public class JournalDTO
{
    public string ID { get; set; }
    public string Paragraphs { get; set; }
    public string Options { get; set; }
    public string IconID { get; set; }
}

public class JournalParser : TSVParserBase<JournalDTO, JournalDataSO>
{
    public override string TargetFileName => "JournalData"; // JournalData.tsv 로드

    protected override string GetAssetName(JournalDTO dto) => $"JournalData/JO_{dto.ID}";

    protected override ClassMap<JournalDTO> GetClassMap() => new JournalMap();

    protected override void Populate(JournalDataSO so, JournalDTO dto)
    {
        if (dto == null || string.IsNullOrEmpty(dto.ID)) return;

        // Paragraphs 파싱 (세미콜론 구분)
        List<string> paragraphs = new List<string>();
        if (!string.IsNullOrEmpty(dto.Paragraphs))
        {
            paragraphs = dto.Paragraphs.Split(';').Select(p => p.Trim()).ToList();
        }

        // Options 파싱 (Key:Text;Key:Text 형식)
        List<JournalOption> options = new List<JournalOption>();
        if (!string.IsNullOrEmpty(dto.Options))
        {
            var pairStrings = dto.Options.Split(';');
            foreach (var pair in pairStrings)
            {
                var split = pair.Split(':');
                if (split.Length >= 2)
                {
                    options.Add(new JournalOption
                    {
                        actionKey = split[0].Trim(),
                        text = split[1].Trim()
                    });
                }
            }
        }

        // Addressable을 위해 ID만 저장
        string iconId = dto.IconID?.Trim();

        so.SetData(dto.ID, paragraphs, options, iconId);
    }

    protected override void OnPostImport(string outputDir)
    {
        // JournalDataBase 업데이트
        string subDir = Path.Combine(outputDir, "JournalData").Replace("\\", "/");
        if (subDir.EndsWith("/")) subDir = subDir.Substring(0, subDir.Length - 1);

        string[] guids = AssetDatabase.FindAssets("t:JournalDataSO", new[] { subDir });
        var assets = new List<BaseSO>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<JournalDataSO>(path);
            if (asset != null) assets.Add(asset);
        }

        // JournalDataBaseSO 전용 DB 생성
        string dbPath = Path.Combine(outputDir, "JournalDataBase.asset");
        var db = AssetDatabase.LoadAssetAtPath<JournalDataBaseSO>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<JournalDataBaseSO>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        var field = typeof(DataBaseSO).GetField("_soList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(db, assets);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssetIfDirty(db);

        // 어드레서블 자동 등록 복구
        AddressableHelper.RegisterToAddressable(dbPath);

        Debug.Log($"[JournalParser] 일지 DB 통합 완료: {assets.Count}개 등록");
    }
}

public sealed class JournalMap : ClassMap<JournalDTO>
{
    public JournalMap()
    {
        Map(m => m.ID).Name("ID");
        Map(m => m.Paragraphs).Name("Paragraphs");
        Map(m => m.Options).Name("Options");
        Map(m => m.IconID).Name("IconID");
    }
}

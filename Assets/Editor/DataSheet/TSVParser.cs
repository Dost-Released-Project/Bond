using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using UnityEditor;
using UnityEngine;

public interface ITSVParser
{
    string TargetFileName { get; }
    void ParseAndImport(string tsvPath, string outputDir);
}

public abstract class TSVParserBase<TDTO, TSO> : ITSVParser
    where TDTO : new()
    where TSO  : ScriptableObject
{
    private const int HEADER_ROW_INDEX = 8; // 0-based

    public abstract string TargetFileName { get; }

    // 서브클래스가 ClassMap 제공
    protected abstract ClassMap<TDTO> GetClassMap();

    // DTO → SO 필드 복사
    protected abstract void Populate(TSO so, TDTO dto);

    // 에셋 파일명 결정
    protected abstract string GetAssetName(TDTO dto);

    // 임포트 완료 후 호출되는 훅
    protected virtual void OnPostImport(string outputDir) { }

    public void ParseAndImport(string tsvPath, string outputDir)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = "\t",
            Mode = CsvMode.NoEscape,
            HasHeaderRecord = true,
            HeaderValidated = null,
            MissingFieldFound = null,
        };

        string[] lines = File.ReadAllLines(tsvPath);
        if (lines.Length <= HEADER_ROW_INDEX)
        {
            Debug.LogError($"[{TargetFileName}] 파일 라인 수가 헤더 위치({HEADER_ROW_INDEX})보다 적습니다.");
            return;
        }

        // 헤더 이전 줄 제거
        var filteredLines = lines.Skip(HEADER_ROW_INDEX).ToList();
        
        // 헤더 바로 아래의 '타입 가이드 행' 제거 (존재할 경우)
        if (filteredLines.Count > 1)
        {
            filteredLines.RemoveAt(1);
        }

        string trimmed = string.Join("\n", filteredLines);

        List<TDTO> records;
        try
        {
            using var reader = new StringReader(trimmed);
            using var csv    = new CsvReader(reader, config);
            csv.Context.RegisterClassMap(GetClassMap());
            records = csv.GetRecords<TDTO>().ToList();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[{TargetFileName}] CSV 파싱 실패: {e.Message}");
            return;
        }

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var dto in records)
            {
                string name = GetAssetName(dto);
                string assetPath = $"{outputDir}{name}.asset";

                // 하위 폴더 경로가 포함된 경우 디렉토리 자동 생성
                string directory = Path.GetDirectoryName(assetPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var so = AssetDatabase.LoadAssetAtPath<TSO>(assetPath);
                bool isNew = so == null;
                if (isNew) so = ScriptableObject.CreateInstance<TSO>();

                Populate(so, dto);

                if (isNew) AssetDatabase.CreateAsset(so, assetPath);
                else EditorUtility.SetDirty(so);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TableReader] {tsvPath}.tsv 로딩 실패: {e.Message}");
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        OnPostImport(outputDir);

        Debug.Log($"[{TargetFileName}] 임포트 완료: {records.Count}개");
    }
}
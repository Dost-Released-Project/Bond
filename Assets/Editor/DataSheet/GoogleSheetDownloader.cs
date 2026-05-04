using System.IO;
using System.Net.Http;
using UnityEditor;
using UnityEngine;

public static class GoogleSheetDownloader
{
    private static readonly HttpClient _http = new HttpClient();

    // 단일 시트 다운로드
    public static bool Download(GoogleSheetConfig config,
        GoogleSheetConfig.SheetEntry entry,
        string savePath)
    {
        string url = config.GetExportUrl(entry);

        try
        {
            // Editor이므로 동기 호출 허용
            var response = _http.GetAsync(url).Result;

            if (response.IsSuccessStatusCode == false)
            {
                Debug.LogError($"[{entry.sheetName}] 다운로드 실패: {response.StatusCode}\n" +
                               $"시트가 '링크 공유'로 설정되어 있는지 확인하세요.");
                return false;
            }

            string content = response.Content.ReadAsStringAsync().Result;

            // 개행 통일 (Windows \r\n → \n)
            content = content.Replace("\r\n", "\n").Replace("\r", "\n");

            File.WriteAllText(savePath, content, System.Text.Encoding.UTF8);
            Debug.Log($"[{entry.sheetName}] 다운로드 완료: {savePath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[{entry.sheetName}] 네트워크 오류: {e.Message}");
            return false;
        }
    }

    // 전체 시트 일괄 다운로드
    public static void DownloadAll(GoogleSheetConfig config, string tsvRoot)
    {
        if (Directory.Exists(tsvRoot) == false)
            Directory.CreateDirectory(tsvRoot);

        foreach (var entry in config.sheets)
        {
            string savePath = Path.Combine(tsvRoot, $"{entry.sheetName}.tsv");
            Download(config, entry, savePath);
        }
    }
}
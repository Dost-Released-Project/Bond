using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using UnityEditor;
using UnityEngine;

public static class GoogleSheetDownloader
{
    private static readonly HttpClient _http = new HttpClient();

    // 단일 시트 다운로드
    public static bool Download(GoogleSheetEntry entry,
        string savePath)
    {
        string url = entry.GetExportUrl();

        try
        {
            // Editor이므로 동기 호출 허용
            var response = _http.GetAsync(url).Result;

            if (response.IsSuccessStatusCode == false)
            {
                Debug.LogError($"[{entry.SheetName}] 다운로드 실패: {response.StatusCode}\n" +
                               $"시트가 '링크 공유'로 설정되어 있는지 확인하세요.");
                return false;
            }

            string content = response.Content.ReadAsStringAsync().Result;

            // 개행 통일 (Windows \r\n → \n)
            content = content.Replace("\r\n", "\n").Replace("\r", "\n");

            File.WriteAllText(savePath, content, System.Text.Encoding.UTF8);
            Debug.Log($"[{entry.SheetName}] 다운로드 완료: {savePath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[{entry.SheetName}] 네트워크 오류: {e.Message}");
            return false;
        }
    }

    // 전체 시트 일괄 다운로드
    public static void DownloadAll(IEnumerable<GoogleSheetEntry> entries, string tsvRoot)
    {
        if (Directory.Exists(tsvRoot) == false)
            Directory.CreateDirectory(tsvRoot);

        foreach (var entry in entries)
        {
            string savePath = Path.Combine(tsvRoot, $"{entry.SheetName}.tsv");
            Download(entry, savePath);
        }
    }
}
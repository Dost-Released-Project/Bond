using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// CI 환경에서 Addressables 빌드 후 플레이어를 빌드하는 스크립트
/// </summary>
public static class CIBuildScript
{
    /// <summary>
    /// CI 빌드 진입점. GameCI의 buildMethod로 호출된다.
    /// </summary>
    public static void Build()
    {
        BuildAddressables();
        BuildPlayer();
    }

    private static void BuildAddressables()
    {
        AddressableAssetSettings settings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(
            "Assets/AddressableAssetsData/AddressableAssetSettings.asset");
        if (settings == null)
        {
            throw new Exception("AddressableAssetSettings를 찾을 수 없습니다.");
        }

        AddressableAssetSettingsDefaultObject.Settings = settings;
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        if (string.IsNullOrEmpty(result.Error) == false)
        {
            throw new Exception($"Addressables 빌드 실패: {result.Error}");
        }

        Debug.Log("Addressables 빌드 완료.");
    }

    private static void BuildPlayer()
    {
        string buildPath = Environment.GetEnvironmentVariable("BUILD_PATH") ?? "build/StandaloneWindows64";
        string buildName = Environment.GetEnvironmentVariable("BUILD_NAME") ?? "StandaloneWindows64";

        // Application.dataPath = <project>/Assets → 한 단계 위가 프로젝트 루트
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputPath = Path.Combine(projectRoot, buildPath, buildName);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Failed)
        {
            throw new Exception("플레이어 빌드 실패.");
        }

        Debug.Log($"플레이어 빌드 완료: {options.locationPathName}");
    }

    private static string[] GetEnabledScenes()
    {
        return new string[] { "Assets/01. Scenes/Title_Scene.unity" };
    }
}

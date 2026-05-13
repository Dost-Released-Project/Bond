using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class AddressableHelper
{
    private const string DEFAULT_GROUP_NAME = "Data";

    /// <summary>
    /// 에셋을 어드레서블 그룹에 등록하고 어드레스(주소)를 설정합니다.
    /// </summary>
    /// <param name="assetPath">에셋의 프로젝트 상대 경로 (Assets/...)</param>
    /// <param name="address">사용할 어드레서블 주소. null일 경우 에셋 파일명을 사용합니다.</param>
    /// <param name="groupName">등록할 그룹 이름. 기본값은 "Data" 입니다.</param>
    public static void RegisterToAddressable(string assetPath, string address = null, string groupName = DEFAULT_GROUP_NAME)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogWarning("[AddressableHelper] AddressableAssetSettings를 찾을 수 없습니다. 어드레서블 설정을 먼저 생성해주세요.");
            return;
        }

        // 1. 에셋 파일명 추출 (주소가 없을 경우 대비)
        if (string.IsNullOrEmpty(address))
        {
            address = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        }

        // 2. 그룹 찾기 또는 생성
        var group = settings.FindGroup(groupName);
        if (group == null)
        {
            group = settings.CreateGroup(groupName, false, false, true, null);
            Debug.Log($"[AddressableHelper] 새로운 그룹 생성: {groupName}");
        }

        // 3. 에셋 GUID 확인
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogError($"[AddressableHelper] 에셋을 찾을 수 없습니다: {assetPath}");
            return;
        }

        // 4. 엔트리 생성 또는 이동
        var entry = settings.CreateOrMoveEntry(guid, group);
        if (entry != null)
        {
            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AddressableHelper] '{address}' 등록 완료 (Group: {groupName})");
        }
    }
}

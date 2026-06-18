using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// skillId → sceneId(Addressables 키) 매핑 테이블을 보관하는 ScriptableObject.
/// Inspector에서 엔트리를 추가해 컷씬이 연결된 스킬 ID와 씬 주소를 등록한다.
/// 생성 위치: Assets 우클릭 → Create → Bond → SkillCutSceneConfig
/// </summary>
[CreateAssetMenu(menuName = "Bond/SkillCutSceneConfig", fileName = "SkillCutSceneConfig")]
public class SkillCutSceneConfig : ScriptableObject
{
    [SerializeField] private List<SkillCutSceneEntry> _entries = new List<SkillCutSceneEntry>();

    /// <summary>등록된 모든 엔트리 목록. 읽기 전용.</summary>
    public IReadOnlyList<SkillCutSceneEntry> Entries => _entries;

    /// <summary>
    /// skillId에 대응하는 sceneId를 반환한다. 없으면 null.
    /// </summary>
    public bool TryGetSceneId(string skillId, out string sceneId)
    {
        sceneId = null;

        foreach (SkillCutSceneEntry entry in _entries)
        {
            if (entry.SkillId == skillId)
            {
                sceneId = entry.SceneId;
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// skillId 와 Addressables sceneId 의 단일 매핑 엔트리.
/// </summary>
[Serializable]
public class SkillCutSceneEntry
{
    [Tooltip("컷씬을 트리거할 스킬 ID")]
    public string SkillId;

    [Tooltip("Addressables 씬 주소 (sceneId)")]
    public string SceneId;
}

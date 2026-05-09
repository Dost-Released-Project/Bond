using UnityEngine;

/// <summary>
/// [D] Pure DataBase: 모든 SkillData SO를 관리하는 통합 데이터베이스.
/// </summary>
[CreateAssetMenu(fileName = "SkillDataBase", menuName = "Bond/SkillSystem/SkillDataBase")]
public class SkillDataBaseSO : DataBaseSO
{
    /// <summary>
    /// 에디터 파서에서 수집된 SO 리스트를 강제로 주입하기 위한 메서드.
    /// </summary>
    public void SetSOList(System.Collections.Generic.List<BaseSO> list)
    {
        // DataBaseSO의 _soList 필드에 접근하기 위해 리플렉션을 사용하거나, 
        // DataBaseSO에 protected/public setter를 추가해야 합니다.
        // 여기서는 DataBaseSO의 구조를 존중하여 리플렉션으로 안전하게 주입합니다.
        var field = typeof(DataBaseSO).GetField("_soList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(this, list);
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
}

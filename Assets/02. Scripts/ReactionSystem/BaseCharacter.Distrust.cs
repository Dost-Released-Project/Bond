using System.Collections.Generic;
using UnityEngine;

// BaseCharacter 의 "특정 아군 불협조" 파트. 봉인/버프와 동일하게 전투 런타임 한정 상태이며
// 만료 tick(TickDistrust)도 자기 턴 시작에 봉인과 같은 타이밍으로 돈다.
// 불협조 대상 아군에게는:
//   1) 그 아군의 행동에 리액션하지 않고, 그 아군 대상 보조/보호 리액션도 발동하지 않는다
//      (ReactionSystem.Resolve 의 후보 제외).
//   2) 그 아군을 대상으로 한 보조/보호(DEFENSIVE/SUPPORT) 스킬을 선택할 수 없다
//      (FormationManager.GetValidSlots / HasAnyValidTarget 의 후보 제외 — 수동/자동 공통).
// 의심 많은(TRT_004) 등에서 사용.
public partial class BaseCharacter
{
    // 불협조 대상 아군 → 남은 자기 턴 수. (전투 런타임 한정 — 세이브 대상 아님)
    private readonly Dictionary<BaseCharacter, int> _distrust = new Dictionary<BaseCharacter, int>();

    /// <summary>대상 아군과 turns(리액터 자기 턴 수) 동안 불협조 상태로 만든다. 이미 있으면 더 긴 쪽으로 갱신.</summary>
    public void ApplyDistrust(BaseCharacter ally, int turns)
    {
        if (ally == null || ally == this || turns <= 0) return;
        if (_distrust.TryGetValue(ally, out var cur) && cur >= turns) return;
        _distrust[ally] = turns;
        Debug.Log($"<color=orange>[불협조]</color> {Name} 가 {ally.Name} 에게 {turns}턴 동안 비협조.");
    }

    /// <summary>이 캐릭터가 해당 아군에게 비협조 상태인지(그 아군에 대한 리액션·보조·보호 차단).</summary>
    public bool IsUncooperativeWith(BaseCharacter ally)
        => ally != null && _distrust.ContainsKey(ally);

    /// <summary>자기 턴 시작마다 호출. 불협조 지속을 1 감소시키고 만료분을 제거한다(TickSeals 와 동일 타이밍).</summary>
    public void TickDistrust()
    {
        if (_distrust.Count == 0) return;
        var keys = new List<BaseCharacter>(_distrust.Keys);
        foreach (var ally in keys)
            if (--_distrust[ally] <= 0) _distrust.Remove(ally);
    }
}

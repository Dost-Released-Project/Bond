using System;
using System.Collections.Generic;
using Buffs;
using Newtonsoft.Json;
using UnityEngine;

// BaseCharacter 의 전투 버프 파트. 활성 버프 보관·적용·만료를 담당한다.
// 버프는 전투 런타임 한정이라 세이브에 직렬화하지 않는다([JsonIgnore]).
public partial class BaseCharacter
{
    [JsonIgnore] private readonly List<ActiveBuff> _activeBuffs = new List<ActiveBuff>();

    /// <summary>현재 활성 버프 목록(읽기 전용). UI 표시용.</summary>
    [JsonIgnore] public IReadOnlyList<ActiveBuff> ActiveBuffs => _activeBuffs;

    /// <summary>버프 추가/만료로 활성 목록이 변할 때 발사. UI 갱신용.</summary>
    public event Action<BaseCharacter> OnBuffsChanged;

    /// <summary>
    /// 버프를 부여한다. 같은 Id 가 이미 있으면 스택하지 않고 남은 지속을 더 긴 쪽으로 갱신한다.
    /// DamageMultiplier / DamageReduction 모디파이어는 파이프라인이 라이브로 읽으므로 CalcStat 재계산은 불필요하다.
    /// </summary>
    public void ApplyBuff(ActiveBuff buff)
    {
        if (buff == null || buff.Modifiers == null) return;

        var existing = _activeBuffs.Find(b => b.Id == buff.Id);
        if (existing != null)
        {
            existing.RemainingTurns = Mathf.Max(existing.RemainingTurns, buff.RemainingTurns);
            OnBuffsChanged?.Invoke(this);
            return;
        }

        // 제거 시 매칭할 source 를 이 버프 인스턴스로 통일한다.
        foreach (var m in buff.Modifiers) m.source = buff;
        _activeBuffs.Add(buff);
        StatController.AddModifiers(buff.Modifiers);
        OnBuffsChanged?.Invoke(this);

        Debug.Log($"<color=cyan>[버프]</color> {Name} 에게 '{buff.Id}' 부여 ({buff.RemainingTurns}턴, 모디파이어 {buff.Modifiers.Count}개)");
    }

    /// <summary>
    /// 자기 턴 시작마다 호출. 모든 버프의 남은 지속을 1 감소시키고 만료된 것을 제거한다.
    /// 턴 시작(행동 전)에 돌기 때문에 이번 턴 행동 중 부여된 버프는 다음 자기 턴에야 감소한다.
    /// </summary>
    public void TickBuffs()
    {
        if (_activeBuffs.Count == 0) return;

        bool changed = false;
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            if (IsDead) break;

            var buff = _activeBuffs[i];

            // 도트 효과 적용 (피해: 음수, 회복: 양수)
            if (buff.HpChangePerTurn != 0)
            {
                int amount = Mathf.RoundToInt(buff.HpChangePerTurn);
                if (amount > 0)
                {
                    RecoverHp(amount, false);
                    Debug.Log($"<color=green>[도트 힐]</color> {Name} 이(가) '{buff.Id}' 로 인해 {amount} 회복되었습니다.");
                }
                else if (amount < 0)
                {
                    ReduceHP(-amount, false);
                    Debug.Log($"<color=red>[도트 피해]</color> {Name} 이(가) '{buff.Id}' 로 인해 {-amount} 피해를 입었습니다.");
                }
            }

            if (IsDead)
            {
                StatController.RemoveModifiersFromSource(buff, buff.Modifiers.Count);
                _activeBuffs.RemoveAt(i);
                changed = true;
                continue;
            }

            buff.RemainingTurns--;
            if (buff.RemainingTurns <= 0)
            {
                StatController.RemoveModifiersFromSource(buff, buff.Modifiers.Count);
                _activeBuffs.RemoveAt(i);
                changed = true;
                Debug.Log($"<color=gray>[버프 만료]</color> {Name} 의 '{buff.Id}' 가 만료되었습니다.");
            }
        }
        if (changed) OnBuffsChanged?.Invoke(this);
    }
}

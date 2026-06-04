using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PipeLine;
using Reactions;
using UnityEngine;

/// <summary>
/// BaseCharacter 의 자기 턴 돌발 행동(Anomaly) 파트.
/// OnSelfTurn 페이즈 트레잇은 자기 턴 시작에 평가되며, 발동 시 계획된 행동(스킬)을 대체한다.
/// 트리거 조건은 Subject(자신) 기반(HpBelow/HpAbove/StressAbove/PartyStressAverage 등)만 사용해야 한다
/// — 자기 턴에는 원본 스킬 컨텍스트가 없어 SkillType/Crit/Evade 등 BattleContext 의존 조건은 의미가 없다.
/// </summary>
public partial class BaseCharacter
{
    /// <summary>
    /// 자기 턴 시작에 호출. OnSelfTurn 트레잇 중 조건을 만족하는 첫 항목의 효과를 실행하고
    /// true(계획 행동 대체)를 반환한다. 없으면 false.
    /// </summary>
    public async UniTask<bool> TryRunSelfTurnAnomalyAsync()
    {
        var reaction = FindSelfTurnAnomaly();
        if (reaction == null) return false;

        if (!RollAnomaly())
        {
            Debug.Log($"<color=grey>[돌발 억제]</color> {Name} 의 성향이 발동하지 않았습니다 (확률 {GetAnomalyChance():P0}).");
            return false;
        }

        var ctx = new BattleContext(this, null, false); // 자기 턴 — 원본 스킬 없음
        var execution = new ReactionExecution(this, reaction, ReactionResult.Anomaly, new List<BaseCharacter> { this });

        Debug.Log($"<color=magenta>[돌발]</color> {Name} 의 성향이 발동해 계획 행동을 대체합니다. ({reaction.Effect?.Description})");

        if (reaction.Effect != null)
            await reaction.Effect.Apply(this, execution, ctx);

        await UniTask.Delay(1000); // 돌발 연출 마무리
        return true;
    }

    /// <summary>OnSelfTurn 트레잇 중 트리거 조건을 만족하는 첫 리액션을 반환. 없으면 null.</summary>
    private Reaction FindSelfTurnAnomaly()
    {
        var reactions = Reactions;
        if (reactions == null) return null;

        foreach (var r in reactions)
        {
            if (r == null || r.Phase != E_ReactionPhase.OnSelfTurn || r.Trigger == null) continue;

            try
            {
                var ctx = new BattleContext(this, null, false);
                if (r.Trigger.CheckCondition(this, ctx)) return r;
            }
            catch (Exception e)
            {
                // OnSelfTurn 에 BattleContext 의존 조건을 잘못 넣은 경우 등 — 크래시 대신 경고.
                Debug.LogWarning($"[Anomaly] {Name} 의 OnSelfTurn 조건 평가 실패(자기턴엔 Subject 기반 조건만 사용): {e.Message}");
            }
        }
        return null;
    }

    /// <summary>
    /// 돌발 행동 확률 = clamp([기본 + 스트레스 × 계수] − 지능 × 계수, 최저 5%, 1).
    /// 관계 보너스는 정책상 제외. 상수는 실제 Insanity/INT 범위에 맞춰 튜닝할 밸런스 값.
    /// </summary>
    public float GetAnomalyChance()
    {
        const float baseRate   = 0.20f;
        const float stressCoef = 0.005f; // Insanity(0~100) → 최대 +0.5
        const float intCoef    = 0.004f; // 지능 억제 계수 (INT 범위에 맞춰 튜닝)
        const float minRate    = 0.05f;  // 최저 발동 확률 하한선

        float chance = baseRate + Insanity * stressCoef - Stat.INT * intCoef;
        return Mathf.Clamp(chance, minRate, 1f);
    }

    /// <summary>돌발 행동 확률로 1회 굴림.</summary>
    public bool RollAnomaly() => UnityEngine.Random.value < GetAnomalyChance();

    /// <summary>해당 리액션이 이 캐릭터의 성향(트레잇) 리액션인지.</summary>
    public bool IsTraitReaction(Reaction reaction)
        => reaction != null && TraitReactions != null && System.Array.IndexOf(TraitReactions, reaction) >= 0;
}

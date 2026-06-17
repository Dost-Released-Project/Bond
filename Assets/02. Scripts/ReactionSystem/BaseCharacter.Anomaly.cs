using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PipeLine;
using Reactions;
using UnityEngine;

// BaseCharacter 의 자기 턴 돌발 행동(Anomaly) 파트.
// OnSelfTurn 페이즈 트레잇은 자기 턴 시작에 평가되며, 발동 시 계획된 행동(스킬)을 대체한다.
// 트리거 조건은 Subject(자신) 기반(HpBelow/HpAbove/StressAbove/PartyStressAverage 등)만 사용해야 한다
// — 자기 턴에는 원본 스킬 컨텍스트가 없어 SkillType/Crit/Evade 등 BattleContext 의존 조건은 의미가 없다.
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

        var subjects = new List<BaseCharacter> { this };
        var result = JudgeReaction(reaction, subjects);
        if (result == ReactionResult.Default)
        {
            // 평상시 — 계획된 행동을 그대로 수행(대체 미발동, 오버라이드 안 함).
            Debug.Log($"<color=grey>[자기턴 판정]</color> {Name} — 평상시 행동 유지(대체 미발동).");
            return false;
        }

        var ctx = new BattleContext(this, null, false); // 자기 턴 — 원본 스킬 없음
        var execution = new ReactionExecution(this, reaction, result, subjects);
        var eff = reaction.EffectFor(result);

        Debug.Log($"<color=magenta>[자기턴 대체:{result}]</color> {Name} 의 성향이 계획 행동을 대체합니다. ({eff?.Description})");
        if (result == ReactionResult.Anomaly) MarkAnomaly(); // 특이(돌발)만 아군 관찰 플래그 — 강화(각성)는 제외

        if (eff != null)
            await eff.Apply(this, execution, ctx);

        await UniTask.Delay(1000); // 대체 연출 마무리
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
    /// 리액션 판정. 트리거 충족 후 이 캐릭터가 어떤 분기를 실행할지 결정한다.<br/>
    /// - 역할 리액션: 관계가 낮을수록 확률적으로 특이(돌발) 행동 → ReactionResult.Anomaly.<br/>
    /// - 성향 리액션: 관계가 높을수록 확률적으로 강화 행동 → ReactionResult.BondAwakening.<br/>
    /// 굴림에 실패(=대체 안 함)하면 평상시 행동(ReactionResult.Default).<br/>
    /// 관계 수치는 RelationFor(subjects)(리액터↔관찰대상, 없으면 파티 평균)를 입력으로 쓴다.
    /// </summary>
    public ReactionResult JudgeReaction(Reaction reaction, IReadOnlyList<BaseCharacter> subjects)
    {
        // TODO: 수치 조정
        int relation = RelationFor(subjects);
        if (IsTraitReaction(reaction))
            return UnityEngine.Random.value < GetBondAwakeningChance(relation)
                ? ReactionResult.BondAwakening : ReactionResult.Default;

        return UnityEngine.Random.value < GetAnomalyChance(relation)
            ? ReactionResult.Anomaly : ReactionResult.Default;
    }

    /// <summary>
    /// 역할 리액션의 특이(돌발) 행동 확률 = clamp(기본 + 스트레스 − 지능 − 관계, 최저 5%, 1).<br/>
    /// 관계가 낮을수록·스트레스가 높을수록·지능이 낮을수록 높아진다.<br/>
    /// 상수는 실제 Insanity/INT/관계 범위에 맞춰 튜닝할 밸런스 값.
    /// </summary>
    public float GetAnomalyChance(int relation)
    {
        // TODO: 계산식 검토
        const float baseRate     = 0.20f;
        const float stressCoef   = 0.005f; // Insanity(0~100) → 최대 +0.5
        const float intCoef      = 0.004f; // 지능 억제 계수 (INT 범위에 맞춰 튜닝)
        const float relationCoef = 0.004f; // 관계↑ → 특이행동 억제 (관계 스케일에 맞춰 튜닝)
        const float minRate      = 0.05f;  // 최저 발동 확률 하한선

        float chance = baseRate + Insanity * stressCoef - Stat.INT * intCoef - relation * relationCoef;
        return Mathf.Clamp(chance, minRate, 1f);
    }

    /// <summary>
    /// 성향 리액션의 강화(유대적 각성) 행동 확률 = clamp(기본 + 관계 + 스트레스 + 지능, 최저 5%, 1).<br/>
    /// 관계가 높을수록·스트레스 임계일수록·지능이 높을수록(통제→각성 전환) 높아진다(GDD §4).<br/>
    /// 상수는 밸런스 튜닝 대상.
    /// </summary>
    public float GetBondAwakeningChance(int relation)
    {
        // TODO: 계산식 검토
        const float baseRate     = 0.10f;
        const float relationCoef = 0.006f; // 관계↑ → 각성↑ (관계 스케일에 맞춰 튜닝)
        const float stressCoef   = 0.003f; // 스트레스 임계에서 각성 보조
        const float intCoef      = 0.003f; // 지능(통제력) → 각성 전환 보정(GDD)
        const float minRate      = 0.05f;

        float chance = baseRate + relation * relationCoef + Insanity * stressCoef + Stat.INT * intCoef;
        return Mathf.Clamp(chance, minRate, 1f);
    }

    /// <summary>
    /// 판정에 쓰는 관계 수치. 리액터↔관찰 대상(subjects, 자신 제외)의 평균.<br/>
    /// 관찰 대상이 없으면(자기 대상 등) 같은 진영 파티 평균으로 폴백한다. 둘 다 없으면 0.
    /// </summary>
    private int RelationFor(IReadOnlyList<BaseCharacter> subjects)
    {
        if (subjects != null && subjects.Count > 0)
        {
            int sum = 0, n = 0;
            foreach (var s in subjects)
                if (s != null && s != this && Relation.TryGetValue(s, out var v)) { sum += v; n++; }
            if (n > 0) return sum / n;
        }

        int psum = 0, pn = 0;
        foreach (var ally in GetSameSideAllies(false))
            if (Relation.TryGetValue(ally, out var v)) { psum += v; pn++; }
        return pn > 0 ? psum / pn : 0;
    }

    /// <summary>해당 리액션이 이 캐릭터의 성향(트레잇) 리액션인지.</summary>
    public bool IsTraitReaction(Reaction reaction)
        => reaction != null && TraitReactions != null && System.Array.IndexOf(TraitReactions, reaction) >= 0;

    /// <summary>해당 리액션이 캐릭터의 긍정(Positive) 성향으로부터 유래했는지 여부.</summary>
    public bool IsPositiveReaction(Reaction reaction)
    {
        if (reaction == null || TraitReactions == null) return false;
        for (int i = 0; i < TraitReactions.Length; i++)
        {
            if (TraitReactions[i] == reaction)
            {
                var traitSO = GetTrait(i);
                if (traitSO != null && traitSO.Type == E_TraitType.Positive)
                    return true;
            }
        }
        return false;
    }

    // ── 최근 돌발 플래그 (아군 돌발 관찰용) ──────────────────────────
    // 돌발 발동 시 set, 자기 턴 시작에 clear → "마지막 자기 턴 이후 돌발했는지"를 나타낸다.
    private bool _hasRecentAnomaly;

    public bool HasRecentAnomaly => _hasRecentAnomaly;
    public void MarkAnomaly() => _hasRecentAnomaly = true;
    public void ClearRecentAnomaly() => _hasRecentAnomaly = false;
}

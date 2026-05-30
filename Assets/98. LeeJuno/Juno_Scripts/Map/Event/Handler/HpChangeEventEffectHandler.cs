using System.Collections.Generic;
using Bond.Expedition;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// EffectType.HpChange 를 처리하는 핸들러.
/// ExpeditionPayload 를 통해 파티에 HP 변화를 적용하고
/// EventLogAccumulator 에 변화 내역을 기록한다.
/// </summary>
public class HpChangeEventEffectHandler : IEventEffectHandler
{
    private readonly ExpeditionPayload _expeditionPayload;
    private readonly EventLogAccumulator _logAccumulator;

    [Inject]
    public HpChangeEventEffectHandler(ExpeditionPayload expeditionPayload, EventLogAccumulator logAccumulator)
    {
        _expeditionPayload = expeditionPayload;
        _logAccumulator    = logAccumulator;
    }

    public bool CanHandle(EffectType effectType) => effectType == EffectType.HpChange;

    public async UniTask HandleAsync(EventEffectData effect)
    {
        switch (effect.TargetType)
        {
            case TargetType.All:
                ApplyHpToAll(effect.HpChangeAmount);
                break;
            case TargetType.RandomOne:
                ApplyHpToRandom(effect.HpChangeAmount);
                break;
            case TargetType.ChooseOne:
                await ApplyHpToChooseOneAsync(effect.HpChangeAmount);
                break;
            default:
                break;
        }
    }

    private void ApplyHpToAll(int amount)
    {
        IReadOnlyList<BaseCharacter> party = _expeditionPayload.Party;
        if (party == null || party.Count == 0)
        {
            Debug.LogWarning("[HpChangeEventEffectHandler] ApplyHpToAll: 파티 데이터가 없습니다.");
            return;
        }

        string direction = amount > 0 ? "회복" : "감소";
        int absAmount    = Mathf.Abs(amount);

        foreach (BaseCharacter character in party)
        {
            if (amount > 0)
                character.RecoverHp(amount);
            else
                character.ReduceHP(-amount);

            Debug.Log($"[HpChangeEventEffectHandler] HP 변화 적용: {character.Name}, amount={amount}");

            // HP 변화 결과를 Pending Report 에 단락으로 덧붙인다 — EventSceneController 가 CommitPendingReport() 로 확정한다
            _logAccumulator?.AppendToPendingReport($"{character.Name}의 체력이 {absAmount} 만큼 {direction} 했다.");
        }
    }

    /// <summary>
    /// CharacterSelectChannel 을 통해 플레이어의 파티원 선택을 기다린 뒤 HP 를 적용한다.
    /// EventSceneController 가 채널 이벤트를 수신해 UI 선택 버튼을 표시한다.
    /// </summary>
    private async UniTask ApplyHpToChooseOneAsync(int amount)
    {
        IReadOnlyList<BaseCharacter> party = _expeditionPayload.Party;
        if (party == null || party.Count == 0)
        {
            Debug.LogWarning("[HpChangeEventEffectHandler] ApplyHpToChooseOneAsync: 파티 데이터가 없습니다.");
            return;
        }

        // 파티원 이름 목록을 채널에 전달하고 플레이어 선택을 대기한다
        List<string> names = new List<string>();
        foreach (BaseCharacter character in party)
            names.Add(character.Name);

        int selectedIndex = await CharacterSelectChannel.RequestAsync(names);

        if (selectedIndex < 0 || selectedIndex >= party.Count)
        {
            Debug.LogWarning($"[HpChangeEventEffectHandler] 유효하지 않은 인덱스({selectedIndex}) — 랜덤으로 대체 적용");
            ApplyHpToRandom(amount);
            return;
        }

        BaseCharacter target = party[selectedIndex];

        if (amount > 0)
            target.RecoverHp(amount);
        else
            target.ReduceHP(-amount);

        Debug.Log($"[HpChangeEventEffectHandler] 선택 HP 변화 적용: {target.Name}, amount={amount}");

        // 선택지 Label 단락("선택: 파티원 선택 → 홍길동")에 대상 이름을 덧붙인다
        _logAccumulator?.AppendChoiceLabelTarget(target.Name);

        string direction = amount > 0 ? "회복" : "감소";
        _logAccumulator?.AppendToPendingReport($"{target.Name}의 체력이 {Mathf.Abs(amount)} 만큼 {direction} 했다.");
    }

    private void ApplyHpToRandom(int amount)
    {
        IReadOnlyList<BaseCharacter> party = _expeditionPayload.Party;
        if (party == null || party.Count == 0)
        {
            Debug.LogWarning("[HpChangeEventEffectHandler] ApplyHpToRandom: 파티 데이터가 없습니다.");
            return;
        }

        int index = UnityEngine.Random.Range(0, party.Count);
        BaseCharacter target = party[index];

        if (amount > 0)
            target.RecoverHp(amount);
        else
            target.ReduceHP(-amount);

        Debug.Log($"[HpChangeEventEffectHandler] 랜덤 HP 변화 적용: {target.Name}, amount={amount}");

        // HP 변화 결과를 Pending Report 에 단락으로 덧붙인다 — EventSceneController 가 CommitPendingReport() 로 확정한다
        string direction = amount > 0 ? "증가" : "감소";
        _logAccumulator?.AppendToPendingReport($"{target.Name}의 체력이 {Mathf.Abs(amount)} 만큼 {direction} 했다.");
    }
}
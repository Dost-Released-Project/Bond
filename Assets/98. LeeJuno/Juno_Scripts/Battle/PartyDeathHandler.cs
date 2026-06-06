using System;
using System.Collections.Generic;
using Bond.Expedition;
using Bond.WT.Journal;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// 맵 씬 진입 시 ExpeditionPayload의 모든 파티원에 대해 OnDead를 구독한다.
/// 전투 씬뿐 아니라 이벤트 씬(HpChangeEventEffectHandler)에서 발생하는 사망도
/// 동일하게 처리하기 위해 OnBattle 토글 방식 대신 직접 구독 방식을 채택한다.
/// 사망 이벤트 수신 시 ExpeditionPayload에서 해당 캐릭터를 즉시 영구 제거한다.
/// 제거 후 파티가 비어있으면(전멸) SetResult(Failure) 후 SceneLoader.Load("Town")으로
/// 강제 퇴각한다.
/// </summary>
public class PartyDeathHandler : IStartable, IDisposable
{
    private readonly ExpeditionPayload _expeditionPayload;
    private readonly IStageLoader _stageLoader;
    private readonly EventLogAccumulator _logAccumulator;
    private readonly JournalModel _journalModel;
    private readonly List<BaseCharacter> _subscribedCharacters;
    private bool _isRetreating;

    public PartyDeathHandler(
        ExpeditionPayload expeditionPayload,
        IStageLoader stageLoader,
        EventLogAccumulator logAccumulator,
        JournalModel journalModel)
    {
        _expeditionPayload    = expeditionPayload;
        _stageLoader          = stageLoader;
        _logAccumulator       = logAccumulator;
        _journalModel         = journalModel;
        _subscribedCharacters = new List<BaseCharacter>();
        _isRetreating         = false;
    }

    void IStartable.Start()
    {
        IReadOnlyList<BaseCharacter> party = _expeditionPayload.Party;
        if (party == null || party.Count == 0)
        {
            Debug.LogWarning("[PartyDeathHandler] ExpeditionPayload.Party가 비어 있습니다.");
            return;
        }

        foreach (BaseCharacter character in party)
        {
            if (character == null) continue;
            character.OnDead += HandlePlayerDeath;
            _subscribedCharacters.Add(character);
        }

        Debug.Log($"[PartyDeathHandler] 파티원 {_subscribedCharacters.Count}명 OnDead 구독 완료.");
    }

    void IDisposable.Dispose()
    {
        foreach (BaseCharacter character in _subscribedCharacters)
        {
            if (character == null) continue;
            character.OnDead -= HandlePlayerDeath;
        }
        _subscribedCharacters.Clear();
    }

    private void HandlePlayerDeath(BaseCharacter deadCharacter)
    {
        bool removed = _expeditionPayload.RemoveMember(deadCharacter);
        Debug.Log(
            $"[PartyDeathHandler] {deadCharacter.Name} 사망 → " +
            $"ExpeditionPayload에서 제거 (결과: {removed})"
        );

        if (_logAccumulator != null)
            _logAccumulator.RecordCharacterDeath(deadCharacter.Name, _stageLoader.CurrentStageType);

        if (_expeditionPayload.Party.Count == 0)
        {
            Debug.LogWarning("[PartyDeathHandler] 파티 전멸 감지 — 강제 퇴각을 시작합니다.");
            // 람다식: 비동기 예외를 void 컨텍스트에서 명시적으로 기록하기 위해 사용한다
            ForceRetreatAsync().Forget(e => Debug.LogError($"[PartyDeathHandler] 강제 퇴각 중 예외: {e}"));
        }
    }

    private async UniTask ForceRetreatAsync()
    {
        if (_isRetreating)
            return;

        _isRetreating = true;

        _expeditionPayload.SetResult(ExpeditionOutcome.Failure);

        try
        {
            await _stageLoader.UnloadCurrentStage();
        }
        catch (Exception e)
        {
            Debug.LogError($"[PartyDeathHandler] 전멸 퇴각 중 씬 언로드 실패: {e.Message}");
        }

        if (_logAccumulator != null)
            _logAccumulator.RecordRetreat(isWipeout: true);

        if (_journalModel != null && _logAccumulator != null && _logAccumulator.HasLogs)
        {
            _journalModel.CurrentParagraph.Value = string.Empty;
            _journalModel.SetReports(_logAccumulator.AllLogs);
            _journalModel.TryNextReport();

            await UniTask.WaitUntil(
                () => _journalModel.IsJournalComplete.Value,
                PlayerLoopTiming.Update
            );
        }

        SceneLoader.Load("Town");
    }
}

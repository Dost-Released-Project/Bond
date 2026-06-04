using System;
using System.Collections.Generic;
using Bond.Expedition;
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

    // 구독 해제를 위해 초기 파티 스냅샷을 보관한다.
    // RemoveMember() 호출 후 Party 리스트가 변경되므로 원본 참조를 별도 저장한다.
    private readonly List<BaseCharacter> _subscribedCharacters;

    // 전멸 퇴각이 이미 시작되었는지 여부 — 중복 호출 방지
    private bool _isRetreating;

    public PartyDeathHandler(ExpeditionPayload expeditionPayload, IStageLoader stageLoader)
    {
        _expeditionPayload = expeditionPayload;
        _stageLoader = stageLoader;
        _subscribedCharacters = new List<BaseCharacter>();
        _isRetreating = false;
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
        // ExpeditionPayload.RemoveMember()는 IPartyController 구현으로 이미 존재한다.
        // 전투 종료 판단(BattleFlowManager.CheckBattleEnd)과 진형 제거(BattleManager)가
        // 먼저 구독되어 있으므로 해당 처리 완료 후 Payload에서 제거된다.
        bool removed = _expeditionPayload.RemoveMember(deadCharacter);
        Debug.Log(
            $"[PartyDeathHandler] {deadCharacter.Name} 사망 → " +
            $"ExpeditionPayload에서 제거 (결과: {removed})"
        );

        // 제거 후 파티가 비어있으면 전멸로 판단해 강제 퇴각을 시작한다.
        if (_expeditionPayload.Party.Count == 0)
        {
            Debug.LogWarning("[PartyDeathHandler] 파티 전멸 감지 — 강제 퇴각을 시작합니다.");
            // 람다식: 비동기 예외를 void 컨텍스트에서 명시적으로 기록하기 위해 사용한다
            ForceRetreatAsync().Forget(e => Debug.LogError($"[PartyDeathHandler] 강제 퇴각 중 예외: {e}"));
        }
    }

    /// <summary>
    /// 파티 전멸 시 강제 퇴각을 처리한다.
    /// MapUIController.RetreatToTownAsync()와 동일한 흐름으로 처리한다:
    ///   1. SetResult(Failure) 기록
    ///   2. 진행 중인 스테이지 씬 언로드
    ///   3. SceneLoader.Load("Town")
    /// _isRetreating 플래그로 중복 호출을 방지한다.
    /// </summary>
    private async UniTask ForceRetreatAsync()
    {
        if (_isRetreating)
            return;

        _isRetreating = true;

        // 전멸은 탐사 실패(Failure)로 기록한다.
        _expeditionPayload.SetResult(ExpeditionOutcome.Failure);

        // 진행 중인 스테이지 씬(전투/이벤트)이 있으면 언로드한다.
        // StageLoader.UnloadCurrentStage()는 씬이 없거나 로딩 중이면 즉시 반환하므로 안전하다.
        try
        {
            await _stageLoader.UnloadCurrentStage();
        }
        catch (Exception e)
        {
            Debug.LogError($"[PartyDeathHandler] 전멸 퇴각 중 씬 언로드 실패: {e.Message}");
            // 언로드 실패 시에도 마을 복귀를 시도한다
        }

        SceneLoader.Load("Town");
    }
}

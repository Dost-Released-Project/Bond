using System;
using BattleSystem.Interface;
using Cysharp.Threading.Tasks;
using PipeLine;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// 전투 시작 시점에 각 캐릭터의 onBattleAction을 래핑해
/// 컷씬이 등록된 스킬 사용 전 IStageLoader.LoadSkillCutScene()을 선행 호출한다.
///
/// IBattleFlowManager.OnBattle 구독 시점:
///   전투 시작 → 캐릭터 배열을 받아 래핑 적용
///   전투 종료 → 래핑 해제(캐시 초기화)
///
/// BattleManager.SubCharacter() 가 onBattleAction += ApplyAct 를 수행한 뒤
/// 이 클래스가 래핑을 덧씌우는 순서를 보장하기 위해,
/// OnBattle 수신 후 UniTask.Yield(LastPostLateUpdate) 로 1프레임 양보 후 래핑을 적용한다.
/// </summary>
public class SkillCutSceneInjector : IStartable, IDisposable
{
    private readonly IBattleFlowManager _battleFlowManager;
    private readonly IStageLoader _stageLoader;
    private readonly SkillCutSceneConfig _config;

    // 래핑 이전 원본 onBattleAction 을 캐릭터별로 보관한다
    private BaseCharacter[] _cachedPlayers;
    private BaseCharacter[] _cachedEnemies;

    public SkillCutSceneInjector(
        IBattleFlowManager battleFlowManager,
        IStageLoader stageLoader,
        SkillCutSceneConfig config)
    {
        _battleFlowManager = battleFlowManager;
        _stageLoader       = stageLoader;
        _config            = config;
        _cachedPlayers     = null;
        _cachedEnemies     = null;
    }

    void IStartable.Start()
    {
        if (_config == null)
        {
            Debug.LogWarning("[SkillCutSceneInjector] SkillCutSceneConfig 가 null 입니다. 컷씬 트리거가 비활성화됩니다.");
            return;
        }

        _battleFlowManager.OnBattle += HandleBattleToggle;
    }

    void IDisposable.Dispose()
    {
        if (_config == null)
        {
            return;
        }

        _battleFlowManager.OnBattle -= HandleBattleToggle;
    }

    private void HandleBattleToggle(BaseCharacter[] players, BaseCharacter[] enemies)
    {
        if (_cachedPlayers == null)
        {
            // 전투 시작 — BattleManager.SubCharacter() 완료 이후 래핑을 적용한다
            _cachedPlayers = players;
            _cachedEnemies = enemies;
            // 람다식: 비동기 래핑을 void 컨텍스트에서 Forget으로 실행하기 위해 사용한다
            WrapAfterFrameAsync(players, enemies).Forget();
        }
        else
        {
            // 전투 종료 — 캐시를 비운다 (onBattleAction 자체는 BattleManager.UnSubCharacter 가 -= 로 원복)
            _cachedPlayers = null;
            _cachedEnemies = null;
        }
    }

    private async UniTask WrapAfterFrameAsync(BaseCharacter[] players, BaseCharacter[] enemies)
    {
        // BattleManager.Start() 에서 SubCharacter() 까지 완료 후 래핑한다
        // SubCharacter 는 Start() 동기 내에서 호출되므로 1프레임 양보로 충분하다
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        WrapBattleActions(players);
        WrapBattleActions(enemies);
    }

    /// <summary>
    /// 배열 내 각 캐릭터의 onBattleAction 을 래핑한다.
    /// config 에 등록된 skillId 가 사용된 경우에만 컷씬을 삽입하고, 그 외에는 원본을 바로 호출한다.
    /// </summary>
    private void WrapBattleActions(BaseCharacter[] characters)
    {
        if (characters == null)
        {
            return;
        }

        foreach (BaseCharacter character in characters)
        {
            if (character == null)
            {
                continue;
            }

            // 래핑 시점의 원본을 클로저로 캡처한다
            // 람다식: 원본 delegate 참조를 클로저로 캡처해 래핑 전후 참조를 분리하기 위해 사용한다
            Func<BattleContext, UniTask> original = character.onBattleAction;

            // 람다식: 비동기 래퍼를 인라인으로 정의하기 위해 사용한다 — 별도 메서드로 분리하면 per-character 클로저를 만들 수 없다
            character.onBattleAction = async (BattleContext context) =>
            {
                if (context.runtimeSkill != null && context.runtimeSkill.Data != null)
                {
                    string skillId = context.runtimeSkill.Data.Id;
                    string sceneId;

                    if (_config.TryGetSceneId(skillId, out sceneId))
                    {
                        await _stageLoader.LoadSkillCutScene(skillId, sceneId);
                    }
                }

                if (original != null)
                {
                    await original.Invoke(context);
                }
            };
        }
    }
}

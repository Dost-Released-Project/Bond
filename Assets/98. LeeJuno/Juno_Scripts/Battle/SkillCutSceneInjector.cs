using System;
using Cysharp.Threading.Tasks;
using PipeLine;
using UnityEngine;

/// <summary>
/// 전투 시작 시점에 각 캐릭터의 onBattleAction 을 래핑해
/// 컷씬이 등록된 스킬 사용 전 CutSceneLoader.Load() 를 선행 호출한다.
///
/// IStartable / IDisposable 을 구현하지 않는다.
/// TurnManager.SwitchBattle() 에서 직접 WrapBattleActions() 를 호출한다.
/// </summary>
public class SkillCutSceneInjector
{
    private readonly CutSceneLoader _cutSceneLoader;
    private readonly SkillCutSceneConfig _config;

    public SkillCutSceneInjector(CutSceneLoader cutSceneLoader, SkillCutSceneConfig config)
    {
        _cutSceneLoader = cutSceneLoader;
        _config         = config;
    }

    /// <summary>
    /// players 와 enemies 배열 내 각 캐릭터의 onBattleAction 을 래핑한다.
    /// config 에 등록된 skillId 가 사용된 경우에만 컷씬을 삽입하고, 그 외에는 원본을 바로 호출한다.
    /// </summary>
    /// <param name="players">플레이어 캐릭터 배열.</param>
    /// <param name="enemies">적 캐릭터 배열.</param>
    public void WrapBattleActions(BaseCharacter[] players, BaseCharacter[] enemies)
    {
        if (_config == null)
        {
            Debug.LogWarning("[SkillCutSceneInjector] SkillCutSceneConfig 가 null 입니다. 컷씬 트리거가 비활성화됩니다.");
            return;
        }

        WrapArray(players);
        WrapArray(enemies);
    }

    /// <summary>
    /// 배열 내 각 캐릭터의 onBattleAction 을 래핑한다.
    /// </summary>
    private void WrapArray(BaseCharacter[] characters)
    {
        if (characters == null)
            return;

        foreach (BaseCharacter character in characters)
        {
            if (character == null)
                continue;

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
                        await _cutSceneLoader.Load(sceneId);
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

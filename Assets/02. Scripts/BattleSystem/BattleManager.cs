using System;
using System.Collections.Generic;
using System.Linq;
using BattleSystem.Interface;
using Cysharp.Threading.Tasks;
using PipeLine;
using Reactions;
using UnityEngine;
using VContainer.Unity;

namespace BattleSystem
{
    public class BattleManager : IBattleManager, IStartable, IDisposable
    {
        private readonly ReactionSystem m_reactionSystem;
        private readonly IBattlePipeLine m_skillApplyPipeline;
        private readonly IBattleFlowManager m_battleFlowManager;
        private readonly IFormationManager m_formationManager;
        private bool m_isBattle;
        
        public BattleManager(ReactionSystem reactionSystem, 
            IBattlePipeLine  skillApplyPipeline, IBattleFlowManager expeditionFlowManager,
            IFormationManager formationManager)
        {
            m_battleFlowManager = expeditionFlowManager;
            m_reactionSystem = reactionSystem;
            m_skillApplyPipeline = skillApplyPipeline;
            m_isBattle = false;
            m_formationManager = formationManager;
            Init();
        }
        
        void IStartable.Start()
        {
            m_battleFlowManager.OnBattle += Battle;
        }

        void IDisposable.Dispose()
        {
            m_battleFlowManager.OnBattle -= Battle;
        }
        
        private void Init()
        {
            m_skillApplyPipeline.SetReactionSystem(m_reactionSystem);
        }
        private void Battle(BaseCharacter[] players, BaseCharacter[] enemies)
        {
            // 모든 캐릭터 구독 / 해제
            m_isBattle = !m_isBattle;
            switch (m_isBattle)
            {
                case true:
                    SubCharacter(players);
                    SubCharacter(enemies);
                    break;
                case false:
                    UnSubCharacter(players);
                    UnSubCharacter(enemies);
                    break;
            }
        }

        private async UniTask ApplyAct(BattleContext battleContext)
        {
            var casterSlot = battleContext.caster.CurrentSlot;
            List<BaseCharacter> targets = new List<BaseCharacter>();

            try
            {
                // 1. 시전자 강조 (Hover)
                casterSlot.SetForceHover(true);
                
                // 2. 1000ms 대기
                await UniTask.Delay(1000);

                // 3. 타겟팅 처리 및 대상자 확정
                var enemySide = (casterSlot.side == E_BattleSide.Player) ? 
                    E_BattleSide.Enemy : 
                    E_BattleSide.Player;

                if (battleContext.runtimeSkill == null || battleContext.runtimeSkill.Data == null)
                {
                    Debug.LogError("Runtime skill or its data is null");
                    return;
                }

                if (battleContext.runtimeSkill.Data.TargetingType == TargetingType.AOE)
                {
                    // 광역 스킬: 마스크 범위 내 모든 생존 유닛 수집
                    switch (battleContext.runtimeSkill.Data.Target)
                    {
                        case SkillTarget.Enemy:
                            targets = GetTargets(enemySide, battleContext.runtimeSkill.Data.EnemyTargetMask);
                            break;
                        case SkillTarget.Party:
                            targets = GetTargets(casterSlot.side, battleContext.runtimeSkill.Data.AllyTargetMask);
                            break;
                        case SkillTarget.Self:
                            targets = GetTargets(casterSlot.side, (int)casterSlot.rank);
                            break;
                    }
                }
                else
                {
                    // 단일 스킬: 이미 결정된 타겟이 있다면 사용, 없다면 예외적으로 랜덤 선택(방어 로직)
                    if (battleContext.target != null && !battleContext.target.IsDead)
                    {
                        targets.Add(battleContext.target);
                    }
                    else
                    {
                        var fallbackTargets = new List<BaseCharacter>();
                        switch (battleContext.runtimeSkill.Data.Target)
                        {
                            case SkillTarget.Enemy:
                                fallbackTargets = GetTargets(enemySide, battleContext.runtimeSkill.Data.EnemyTargetMask);
                                break;
                            case SkillTarget.Party:
                                fallbackTargets = GetTargets(casterSlot.side, battleContext.runtimeSkill.Data.AllyTargetMask);
                                break;
                            case SkillTarget.Self:
                                fallbackTargets = GetTargets(casterSlot.side, (int)casterSlot.rank);
                                break;
                        }
                        if (fallbackTargets.Count > 0)
                        {
                            targets.Add(fallbackTargets[UnityEngine.Random.Range(0, fallbackTargets.Count)]);
                        }
                    }
                }

                // 4. 대상자 강조 (Click)
                Debug.Log($"Target Count: {targets.Count}");
                foreach (var target in targets)
                {
                    target.CurrentSlot.SetForceClick(true);
                }

                // 5. 1000ms 대기
                await UniTask.Delay(1000);

                // 6. 기술 실행 (개별 타겟 단위)
                foreach (var target in targets)
                {
                    BattleContext targetContext = new BattleContext(battleContext, target);
                    await SkillApplyLogic(targetContext);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BattleManager] ApplyAct Error: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                // 7. 연출 초기화 (시각적 피드백 유지 후 해제, 사망자 발생 대비 null 체크)
                if (casterSlot != null) casterSlot.SetForceHover(false);
                foreach (var target in targets)
                {
                    if (target != null && target.CurrentSlot != null)
                    {
                        target.CurrentSlot.SetForceClick(false);
                    }
                }
            }
        }

        private List<BaseCharacter> GetTargets(E_BattleSide side, int targetMask)
        {
            List<BaseCharacter> targetList = new List<BaseCharacter>();
            for (int i = 0; i < 4; i++)
            {
                if ((targetMask & (1 << i)) != 0)
                {
                    var target = m_formationManager.GetCharacterAt(side, (FormationMask)(1 << i));
                    if (target != null && !target.IsDead) 
                    {
                        targetList.Add(target);
                    }
                }
            }
            return targetList;
        }
        
        private async UniTask<BattleContext> SkillApplyLogic(BattleContext context)
        {
            // [입구] 로직 파이프라인 실행
            if (m_skillApplyPipeline != null)
            {
                return await m_skillApplyPipeline.Run(context);
            }

            return context;
        }
        
        #region 전투 시작 시 참여하는 캐릭터 구독 / 해제 하는 로직
        private void SubCharacter(BaseCharacter[] characters)
        {
            foreach (var character in characters)
            {
                character.onBattleAction += ApplyAct;
                character.OnDead += HandleCharacterDeath;
                m_reactionSystem.Register(character);
            }
        }

        private void UnSubCharacter(BaseCharacter[] characters)
        {
            foreach (var character in characters)
            {
                if (character == null) continue;
                character.onBattleAction -= ApplyAct;
                character.OnDead -= HandleCharacterDeath;
                m_reactionSystem.Unregister(character);
            }
        }

        private void HandleCharacterDeath(BaseCharacter deadCharacter)
        {
            Debug.Log($"<color=grey>[BattleManager] {deadCharacter.Name} 사망 처리: 진영에서 제거 및 타겟팅 제외</color>");
            m_formationManager.ClearCharacter(deadCharacter);
        }
        #endregion
    }
}

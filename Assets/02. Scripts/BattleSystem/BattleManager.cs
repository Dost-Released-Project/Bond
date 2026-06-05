using System;
using System.Collections.Generic;
using System.Linq;
using BattleSystem.Interface;
using Cysharp.Threading.Tasks;
using PipeLine;
using Reactions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer.Unity;

namespace BattleSystem
{
    public class BattleManager : IBattleManager, IStartable, IDisposable
    {
        private readonly ReactionSystem m_reactionSystem;
        private readonly IBattlePipeLine m_skillApplyPipeline;
        private readonly IBattleFlowManager m_battleFlowManager;
        private readonly IFormationManager m_formationManager;
        private readonly BattlePresentationManager m_presentationManager;
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
            m_presentationManager = new BattlePresentationManager();
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
                    
                    // 전투 종료 시 모든 캐릭터 슬롯 상태 초기화 및 리소스 해제
                    if (players != null)
                    {
                        foreach (var player in players)
                        {
                            if (player != null)
                            {
                                if (player.CurrentSlot != null)
                                    player.CurrentSlot.ResetAllStates();
                                
                                // 모든 어드레서블 리소스 해제 및 캐시 초기화
                                ReleaseCharacterPortraits(player);
                            }
                        }
                    }
                    if (enemies != null)
                    {
                        foreach (var enemy in enemies)
                        {
                            if (enemy != null)
                            {
                                if (enemy.CurrentSlot != null)
                                    enemy.CurrentSlot.ResetAllStates();
                                
                                // 모든 어드레서블 리소스 해제 및 캐시 초기화
                                ReleaseCharacterPortraits(enemy);
                            }
                        }
                    }
                    break;
            }
        }

        private void ReleaseCharacterPortraits(BaseCharacter character)
        {
            if (character.Portrait != null)
            {
                Addressables.Release(character.Portrait);
                character.Portrait = null;
            }
            if (character.IdlePortrait != null)
            {
                Addressables.Release(character.IdlePortrait);
                character.IdlePortrait = null;
            }
            if (character.AttackPortrait != null)
            {
                Addressables.Release(character.AttackPortrait);
                character.AttackPortrait = null;
            }
        }

        private async UniTask ApplyAct(BattleContext battleContext)
        {
            var casterSlot = battleContext.caster.CurrentSlot;
            List<BaseCharacter> targets = new List<BaseCharacter>();
            List<CharacterSlot> targetSlots = new List<CharacterSlot>(); // 사망 시 슬롯 정보 유실 방지용 캐싱

            try
            {
                // 1. 시전자 강조 및 공격 이미지 전환
                casterSlot.SetActing(true);
                casterSlot.SetImageType(SlotImageType.Attack);
                
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

                // 4. 대상자 강조 (Click) 및 피격 이미지 전환
                Debug.Log($"Target Count: {targets.Count}");
                foreach (var target in targets)
                {
                    if (target.CurrentSlot != null)
                    {
                        targetSlots.Add(target.CurrentSlot);
                        target.CurrentSlot.SetTargeted(true);
                        target.CurrentSlot.SetImageType(SlotImageType.Attack); // 확대 연출과 동시에 피격 자세로 전환
                    }
                }

                // 5. 전투 집중 연출 시작 (Dim 패널 및 캐릭터 확대 이동)
                await m_presentationManager.StartFocusEffect(casterSlot, targetSlots);
                
                // 연출 감상을 위한 추가 대기
                await UniTask.Delay(500);

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
                // 7. 연출 초기화 및 상태 복구 (축소 연출 시작 시점)
                if (casterSlot != null)
                {
                    casterSlot.SetActing(false);
                    casterSlot.SetImageType(SlotImageType.Idle); // 축소되며 Idle 복구
                }
                
                foreach (var slot in targetSlots)
                {
                    if (slot != null)
                    {
                        slot.SetTargeted(false);
                        slot.SetImageType(SlotImageType.Idle); // 축소되며 Idle 복구
                    }
                }

                // 전투 집중 연출 종료 (Dim 해제 및 원복)
                await m_presentationManager.EndFocusEffect(casterSlot, targetSlots);

                // 8. 연출 종료 후 진영 내 빈 공간 채우기 (캐릭터 사망 대비)
                m_formationManager.ConsolidationFormation(E_BattleSide.Player);
                m_formationManager.ConsolidationFormation(E_BattleSide.Enemy);
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

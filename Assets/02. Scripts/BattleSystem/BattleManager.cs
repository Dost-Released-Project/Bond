using System;
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
            // 1. 시전자 강조 (Hover)
            var casterSlot = battleContext.caster.CurrentSlot;
            casterSlot.SetForceHover(true);
            
            // 2. 1000ms 대기
            await UniTask.Delay(1000);

            // 3. 타겟팅 처리 및 대상자 확정
            var enemySide = (casterSlot.side == E_BattleSide.Player) ? 
                E_BattleSide.Enemy : 
                E_BattleSide.Player;

            switch (battleContext.runtimeSkill.Data.Target)
            {
                case SkillTarget.Enemy:
                    ProcessTargeting(battleContext, enemySide, battleContext.runtimeSkill.Data.EnemyTargetMask);
                    break;
                case SkillTarget.Party:
                    ProcessTargeting(battleContext, casterSlot.side, battleContext.runtimeSkill.Data.AllyTargetMask);
                    break;
                case SkillTarget.Self:
                    ProcessTargeting(battleContext, casterSlot.side, (int)casterSlot.rank);
                    break;
            }

            // 4. 대상자 강조 (Click)
            Debug.Log($"Target Count: {battleContext.targets.Count}");
            foreach (var target in battleContext.targets)
            {
                target.CurrentSlot.SetForceClick(true);
            }

            // 5. 1000ms 대기
            await UniTask.Delay(1000);

            // 6. 기술 실행
            SkillApplyLogic(battleContext);

            // 7. 연출 초기화 (시각적 피드백 유지 후 해제)
            casterSlot.SetForceHover(false);
            foreach (var target in battleContext.targets)
            {
                target.CurrentSlot.SetForceClick(false);
            }
        }

        private void ProcessTargeting(BattleContext context, E_BattleSide side, int targetMask)
        {
            bool isDead = false;
            foreach (var target in context.targets)
            {
                if (target.IsDead)
                {
                    isDead = true;
                } 
            }

            if (isDead)
            {
                Debug.Log("죽은 대상 지정");
                return;
            }
            
            context.targets.Clear();
            for (int i = 0; i < 4; i++)
            {
                if ((targetMask & (1 << i)) != 0)
                {
                    var target = m_formationManager.GetCharacterAt(side, (FormationMask)(1 << i));
                    if (target != null) context.targets.Add(target);
                }
            }
        }
        
        private BattleContext SkillApplyLogic(BattleContext context)
        {
            // [입구] 로직 파이프라인 실행
            if (m_skillApplyPipeline != null)
            {
                return m_skillApplyPipeline.Run(context);
            }

            return context;
        }
        
        #region 전투 시작 시 참여하는 캐릭터 구독 / 해제 하는 로직
        private void SubCharacter(BaseCharacter[] characters)
        {
            foreach (var character in characters)
            {
                character.onBattleAction += ApplyAct;
            }
        }

        private void UnSubCharacter(BaseCharacter[] characters)
        {
            foreach (var character in characters)
            {
                character.onBattleAction -= ApplyAct;
            }
        }
        #endregion
    }
}

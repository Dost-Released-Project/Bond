using System;
using System.Linq;
using BattleSystem.Interface;
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

        private void ApplyAct(BattleContext battleContext)
        {
            // BattleContext 적용 로직이 들어가야함
            var enemySide = (battleContext.caster.CurrentSlot.side == E_BattleSide.Player) ? 
                E_BattleSide.Enemy : 
                E_BattleSide.Player;

            switch (battleContext.runtimeSkill.Data.Target)
            {
                case SkillTarget.Enemy:
                    ProcessTargeting(battleContext, enemySide, battleContext.runtimeSkill.Data.EnemyTargetMask);
                    break;
                case SkillTarget.Party:
                    ProcessTargeting(battleContext, battleContext.caster.CurrentSlot.side, battleContext.runtimeSkill.Data.AllyTargetMask);
                    break;
                case SkillTarget.Self:
                    ProcessTargeting(battleContext, battleContext.caster.CurrentSlot.side, (int)battleContext.caster.CurrentSlot.rank);
                    break;
            }
            SkillApplyLogic(battleContext);
        }

        private void ProcessTargeting(BattleContext context, E_BattleSide side, int targetMask)
        {
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

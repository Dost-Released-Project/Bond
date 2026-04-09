using _03._PipeLine;
using Reactions;
using UnityEngine;
using VContainer;

namespace _02._Scripts.BattleSystem
{
    public class BattleManager : IBattleManager
    {
        private readonly ReactionSystem reactionSystem;
        private readonly IBattlePipeLine skillApplyPipeline;
        [Inject]
        public BattleManager(ReactionSystem reactionSystem, 
            IBattlePipeLine  skillApplyPipeline)
        {
            this.reactionSystem = reactionSystem;
            this.skillApplyPipeline = skillApplyPipeline;
            Init();
        }

        private void Init()
        {
            skillApplyPipeline.SetReactionSystem(reactionSystem);
        }

        public BattleContext SkillApplyLogic(BattleContext context)
        {
            // [입구] 로직 파이프라인 실행
            if (skillApplyPipeline != null)
            {
                return skillApplyPipeline.Run(context);
            }

            return context;
        }
    }
}

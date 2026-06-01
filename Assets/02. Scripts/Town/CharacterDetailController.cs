using System;
using System.Collections.Generic;
using System.Linq;
using Reactions;

namespace Bond.UI
{
    public class CharacterDetailController
    {
        private readonly CharacterItemService _itemService;

        private BaseCharacter _character;

        // 변경사항은 BaseCharacter 객체에 즉시 반영한다
        // 추후 트랜잭션/검증 레이어가 필요할 경우 이 계층에서 처리할 것

        public event Action<BaseCharacter> OnCharacterSet;
        public event Action<RoleType>      OnRoleChanged;
        public event Action<int>           OnReactionChanged;

        public CharacterDetailController(
            CharacterItemService itemService
            )
        {
            _itemService = itemService;
        }

        public void SetCharacter(BaseCharacter character)
        {
            _character = character;
            OnCharacterSet?.Invoke(_character);
        }

        public void SetRole(RoleType role)
        {
            if (_character == null) return;
            _character.SetRole(role);

            // 역할 변경 시 유효하지 않은 트리거가 설정된 역할 슬롯을 초기화한다
            var validTriggers = GetRoleTriggers(role);
            for (int i = 0; i < _character.RoleReactions.Length; i++)
            {
                var reaction = _character.RoleReactions[i];
                if (reaction?.Trigger == null) continue;
                if (validTriggers.All(t => t.GetType() != reaction.Trigger.GetType()))
                {
                    reaction.Trigger = null;
                    OnReactionChanged?.Invoke(i);
                }
            }

            OnRoleChanged?.Invoke(role);
        }

        // AccessoryItem은 인벤토리에서 관리되므로 목적지 IInventory를 Presenter에서 전달받는다
        public void UnequipAccessory(int index, IInventory targetInventory)
        {
            if (_character == null) return;
            _itemService.UnequipToInventory(_character, index, targetInventory);
        }

        public void SetReactionTarget(int slotIndex, string characterId)
        {
            var reaction = GetReaction(slotIndex);
            if (reaction == null) return;

            reaction.SubjectCharacterId = characterId;

            OnReactionChanged?.Invoke(slotIndex);
        }

        // 역할 슬롯(0~1)만 트리거 설정 가능. 성향 슬롯은 고정값이므로 호출하지 않는다
        public void SetReactionTrigger(int slotIndex, ITrigger trigger)
        {
            if (slotIndex >= _character.RoleReactions.Length) return;
            var reaction = _character.RoleReactions[slotIndex];
            if (reaction == null) return;
            reaction.Trigger = trigger;
            OnReactionChanged?.Invoke(slotIndex);
        }

        // SkillBase를 Skills[] 배열에서 역탐색해 SkillCastReactionEffect.SkillIndex 로 저장한다
        public void SetReactionSkill(int slotIndex, SkillBase skill)
        {
            if (_character == null || skill == null) return;
            var reaction = GetReaction(slotIndex);
            if (reaction == null) return;

            int idx = Array.IndexOf(_character.Skills, skill);
            if (idx < 0) return;

            // 기존 Effect 가 SkillCastReactionEffect 면 인덱스만 갱신, 아니면 새로 생성
            if (reaction.Effect is SkillCastReactionEffect cast)
            {
                cast.SkillIndex = idx;
            }
            else
            {
                reaction.Effect = new SkillCastReactionEffect { SkillIndex = idx };
            }
            OnReactionChanged?.Invoke(slotIndex);
        }

        // 캐릭터가 현재 보유한 스킬 목록 반환
        public List<SkillBase> GetAvailableSkills()
        {
            if (_character == null) return new List<SkillBase>();
            return _character.Skills.Where(s => s != null).ToList();
        }

        // 직업이 보유 가능한 전체 스킬 목록 반환 (스킬 그리드용)
        // TODO: 스태틱하게 db 접근 후에 그걸 기반으로 반환하도록 구현
        // public List<SkillData> GetAllProfessionSkills()
        // {
        //     if (_character?.Profession == null) return new List<SkillData>();
        //     int profId = _character.Profession.Id;
        //     return _skillDb.Query<SkillData>(s => s.UseableClasses == profId).ToList();
        // }

        // 역할별 유효 트리거 목록 반환
        // 역할 코멘트 기준: Tanker(피격·아군위기), Dealer(처치·치명타), Supporter(턴종료·상태이상)
        public List<ITrigger> GetRoleTriggers(RoleType role)
        {
            return role switch
            {
                RoleType.Tanker    => new List<ITrigger> { new Trigger() },
                RoleType.Dealer    => new List<ITrigger> { new Trigger() },
                RoleType.Supporter => new List<ITrigger> { new Trigger() },
                _                  => new List<ITrigger>()
            };
            // TODO: 역할별 트리거 분류 기준이 확정되면 실제 ICondition 타입으로 분류할 것
            // 현재는 모든 역할에 빈 Trigger를 반환하여 UI 구조만 유지
        }

        // TODO: 파티 혹은 로스터 멤버 반환
        public List<BaseCharacter> GetObserveCandidates() => new List<BaseCharacter>();

        public BaseCharacter CurrentCharacter => _character;

        // slotIndex: 0~1 역할 슬롯, 2~5 성향 슬롯 (RoleReactions[0~1] + TraitReactions[0~3])
        private Reaction GetReaction(int slotIndex)
        {
            if (_character == null) return null;
            if (slotIndex < _character.RoleReactions.Length)
                return _character.RoleReactions[slotIndex];
            int traitIdx = slotIndex - _character.RoleReactions.Length;
            if (traitIdx < _character.TraitReactions.Length)
                return _character.TraitReactions[traitIdx];
            return null;
        }
    }
}

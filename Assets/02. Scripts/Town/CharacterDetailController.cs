using System;
using System.Collections.Generic;
using System.Linq;
using Bond.Embark;
using Reactions;

namespace Bond.UI
{
    public class CharacterDetailController
    {
        private readonly CharacterItemService _itemService;
        private readonly IPartyController _party;

        private BaseCharacter _character;

        // 변경사항은 BaseCharacter 객체에 즉시 반영한다
        // 추후 트랜잭션/검증 레이어가 필요할 경우 이 계층에서 처리할 것

        public event Action<BaseCharacter> OnCharacterSet;
        public event Action<RoleType>      OnRoleChanged;
        public event Action<int>           OnReactionChanged;

        public CharacterDetailController(
            CharacterItemService itemService,
            IPartyController party)
        {
            _itemService = itemService;
            _party = party;
        }

        public void SetCharacter(BaseCharacter character)
        {
            _character = character;
            _character?.SyncTraitReactions();
            OnCharacterSet?.Invoke(_character);
        }

        public void SetRole(RoleType role)
        {
            if (_character == null) return;
            _character.SetRole(role);

            for (int i = 0; i < _character.RoleReactions.Length; i++)
            {
                var reaction = _character.RoleReactions[i];
                if (reaction == null || string.IsNullOrEmpty(reaction.DefinitionId)) continue;
                var def = DBSORegistry.GetSO<ReactionDefinitionSO>(reaction.DefinitionId);
                if (def != null && def.Role != role)
                {
                    _character.ClearRoleReaction(i);
                    OnReactionChanged?.Invoke(i);
                }
            }

            OnRoleChanged?.Invoke(role);
        }

        /// <summary>자동(AI)/수동(플레이어) 전투 전환. 영속 트리거는 BaseCharacter 내부에서 발화된다.</summary>
        public void SetPlayable(bool playable)
        {
            if (_character == null) return;
            _character.SetPlayable(playable);
        }

        // AccessoryItem은 인벤토리에서 관리되므로 목적지 IInventory를 Presenter에서 전달받는다
        public void UnequipAccessory(int index, IInventory targetInventory)
        {
            if (_character == null) return;
            _itemService.UnequipToInventory(_character, index, targetInventory);
        }

        // ── 스킬 편성 (스킬 그리드) ──────────────────────────────────────

        /// <summary>현재 캐릭터 직업이 사용 가능한 전체 스킬 목록(그리드 표시용). DBSORegistry 정적 조회.</summary>
        public IReadOnlyList<SkillData> GetProfessionSkills()
        {
            if (_character?.Profession == null) return Array.Empty<SkillData>();
            int profId = _character.Profession.Id;
            return DBSORegistry.QuerySO<SkillData>(s => s != null && s.UseableClasses == profId).ToList();
        }

        /// <summary>그리드 토글: 편성↔해제. 추가는 최대 슬롯에서 차단(BaseCharacter). 변경 시 true.</summary>
        public bool ToggleSkill(SkillData data)
        {
            if (_character == null || data == null) return false;
            return _character.ToggleSkill(data);
        }

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

        // ── 역할 리액션 카탈로그 (신규 모델) ──────────────────────────────

        /// <summary>
        /// 역할 슬롯(0~1)에서 선택 가능한 리액션 정의 목록.
        /// 현재 캐릭터 역할(RoleType)에 맞고, 다른 역할 슬롯에서 이미 선택된 정의(DefinitionId)는 제외.
        /// </summary>
        public IReadOnlyList<ReactionDefinitionSO> GetRoleReactionCatalog(int roleSlotIndex)
        {
            if (_character == null || _character.RoleType == RoleType.None)
                return Array.Empty<ReactionDefinitionSO>();

            var usedElsewhere = new HashSet<string>();
            for (int i = 0; i < _character.RoleReactions.Length; i++)
            {
                if (i == roleSlotIndex) continue;
                var id = _character.RoleReactions[i]?.DefinitionId;
                if (!string.IsNullOrEmpty(id)) usedElsewhere.Add(id);
            }

            return DBSORegistry.QuerySO<ReactionDefinitionSO>(d => d.Role == _character.RoleType)
                .Where(d => !usedElsewhere.Contains(d.Id))
                .ToList();
        }

        /// <summary>정의 선택 → 런타임 Reaction 생성해 역할 슬롯에 할당(편집 빈칸은 미설정 상태).</summary>
        public void SelectRoleReaction(int roleSlotIndex, ReactionDefinitionSO definition)
        {
            if (_character == null || definition == null) return;
            if (roleSlotIndex < 0 || roleSlotIndex >= _character.RoleReactions.Length) return;
            _character.SetRoleReaction(roleSlotIndex, definition.CreateRuntimeReaction());
            OnReactionChanged?.Invoke(roleSlotIndex);
        }

        /// <summary>역할 슬롯 비우기.</summary>
        public void ClearRoleReaction(int roleSlotIndex)
        {
            if (_character == null) return;
            if (roleSlotIndex < 0 || roleSlotIndex >= _character.RoleReactions.Length) return;
            _character.ClearRoleReaction(roleSlotIndex);
            OnReactionChanged?.Invoke(roleSlotIndex);
        }

        /// <summary>슬롯에 할당된 리액션의 정의(DefinitionId 역조회). 없으면 null.</summary>
        public ReactionDefinitionSO GetSlotDefinition(int slotIndex)
        {
            var reaction = GetReaction(slotIndex);
            if (reaction == null || string.IsNullOrEmpty(reaction.DefinitionId)) return null;
            return DBSORegistry.GetSO<ReactionDefinitionSO>(reaction.DefinitionId);
        }

        /// <summary>슬롯 리액션이 관찰대상 편집슬롯을 갖는가(빈칸 표시 여부 판단용).</summary>
        public bool HasObserveEditable(int slotIndex) => GetObserveSlot(slotIndex) != null;

        /// <summary>슬롯 리액션이 행동스킬 편집슬롯을 갖는가.</summary>
        public bool HasSkillEditable(int slotIndex) => GetActionSkillSlot(slotIndex) != null;

        private ObserveTargetEditableSlot GetObserveSlot(int slotIndex)
            => GetSlotDefinition(slotIndex)?.EditableSlots?.OfType<ObserveTargetEditableSlot>().FirstOrDefault();

        private ActionSkillEditableSlot GetActionSkillSlot(int slotIndex)
            => GetSlotDefinition(slotIndex)?.EditableSlots?.OfType<ActionSkillEditableSlot>().FirstOrDefault();

        /// <summary>관찰대상 편집슬롯이 있을 때 후보 파티 아군(ExcludeSelf 반영). 없으면 빈 목록.</summary>
        public IReadOnlyList<BaseCharacter> GetObserveTargetCandidates(int slotIndex)
        {
            var slot = GetObserveSlot(slotIndex);
            if (slot == null || _party == null) return Array.Empty<BaseCharacter>();
            var party = _party.GetCurrentParty() ?? new List<BaseCharacter>();
            return party.Where(c => c != null && (!slot.ExcludeSelf || c != _character)).ToList();
        }

        /// <summary>관찰대상(아군 Id) 지정.</summary>
        public void SetObserveTarget(int slotIndex, string allyId)
        {
            var reaction = GetReaction(slotIndex);
            var slot = GetObserveSlot(slotIndex);
            if (reaction == null || slot == null) return;
            slot.Apply(reaction, allyId);
            _character.RaiseReactionsChanged();
            OnReactionChanged?.Invoke(slotIndex);
        }

        /// <summary>행동스킬 편집슬롯이 있을 때 후보(장착 + 타입제약). 없으면 빈 목록.</summary>
        public IReadOnlyList<(int index, SkillBase skill)> GetActionSkillCandidates(int slotIndex)
        {
            var slot = GetActionSkillSlot(slotIndex);
            if (slot == null || _character == null) return Array.Empty<(int, SkillBase)>();
            return slot.ResolveCandidates(_character).ToList();
        }

        /// <summary>행동스킬 지정(Skills 배열 인덱스).</summary>
        public void SetActionSkill(int slotIndex, int skillIndex)
        {
            var reaction = GetReaction(slotIndex);
            var slot = GetActionSkillSlot(slotIndex);
            if (reaction == null || slot == null) return;
            slot.Apply(reaction, skillIndex);
            _character.RaiseReactionsChanged();
            OnReactionChanged?.Invoke(slotIndex);
        }

        /// <summary>슬롯 리액션의 모든 편집 빈칸이 채워졌는지(작동 가능). 빈 슬롯이면 false, 정의 못 찾으면 true.</summary>
        public bool IsSlotComplete(int slotIndex)
        {
            var reaction = GetReaction(slotIndex);
            if (reaction == null) return false;
            var def = GetSlotDefinition(slotIndex);
            if (def == null) return true;
            return def.AllEditablesFilled(reaction);
        }

        /// <summary>가방 인벤토리 슬롯에서 장신구 슬롯 방향으로 드래그 장착/스왑을 수행합니다.</summary>
        public void EquipAccessoryFromDrag(IInventory sourceInventory, int invIndex, int charSlotIndex)
        {
            if (_character == null) return;
            _itemService.EquipFromDrag(sourceInventory, invIndex, charSlotIndex);
        }
    }
}

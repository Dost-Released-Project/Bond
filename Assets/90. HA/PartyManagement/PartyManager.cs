using System.Collections.Generic;

namespace Bond.PartyManagement
{
    public enum RoleType
    {
        None,
        Tanker,   // 활성 트리거: TRG_DEF_TG_IN (피격 시), TRG_SIT_ALLY_CRISIS (아군 위기 시)
        Dealer,   // 활성 트리거: TRG_OFF_KILL (적 처치 시), TRG_OFF_CRIT (치명타 시)
        Supporter // 활성 트리거: TRG_SIT_ALLY_TURN_END (아군 턴 종료 시), TRG_DEF_STATUS (상태이상 시)
    }

    public interface IRoleAssigner
    {
        void AssignRole(BaseCharacter character, RoleType role);
        RoleType GetRole(BaseCharacter character);
        IReadOnlyList<string> GetRoleTriggerKeys(RoleType role);  // 편집 UI가 선택 가능한 트리거 목록 표시 시 사용
    }
    
    public interface IPartyProvider
    {
        /// <summary>
        /// 현재 파티 구성원 목록을 읽기전용으로 반환합니다.
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<BaseCharacter> GetCurrentParty();
        bool IsInParty(BaseCharacter character);
        bool IsFull();
    }

    public interface IPartyController
    {
        bool TryAddMember(BaseCharacter character);
        bool RemoveMember(BaseCharacter character);
    }
    
    public class PartyManager : IPartyProvider, IPartyController, IRoleAssigner
    {
        private readonly List<BaseCharacter> currentParty = new List<BaseCharacter>();
        private readonly Dictionary<BaseCharacter, RoleType> roles = new Dictionary<BaseCharacter, RoleType>();
        private readonly Dictionary<RoleType, List<string>> roleTriggerDef = new Dictionary<RoleType, List<string>>();
        private const int MaxPartySize = 4;

        public void SetRoleTriggerDef(IEnumerable<RoleTriggerSO> roleTriggers)
        {
            foreach (var roleTriggerSo in roleTriggers)
            {
                roleTriggerDef[roleTriggerSo.Role] = roleTriggerSo.TriggerKeys;
            }
        }
        
        // 파티 편성
        public bool TryAddMember(BaseCharacter character)
        {
            if (currentParty.Count >= MaxPartySize || currentParty.Contains(character))
            {
                return false;
            }
            else
            {
                currentParty.Add(character);
                return true;
            }
        }

        public bool RemoveMember(BaseCharacter character)
        {
            return currentParty.Remove(character);
        }
        
        public bool IsInParty(BaseCharacter character) => currentParty.Contains(character);
        public bool IsFull() => currentParty.Count >= MaxPartySize;

        // 조회
        public IReadOnlyList<BaseCharacter> GetCurrentParty() => currentParty.AsReadOnly();
        public int GetPartyCount() => currentParty.Count;
        
        // IRoleAssigner
        public void AssignRole(BaseCharacter character, RoleType role)
        {
            roles[character] = role;
        }

        public RoleType GetRole(BaseCharacter character)
        {
            return roles[character];
        }

        public IReadOnlyList<string> GetRoleTriggerKeys(RoleType role)
        {
            return roleTriggerDef[role];
        }
    }
}
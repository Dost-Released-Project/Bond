using System.Collections.Generic;
using System.Linq;
using Bond.Persistence;

namespace Bond.Embark
{
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
        void Clear();
    }
    
    public class PartyManager : IPartyProvider, IPartyController, ISaveable
    {
        private List<BaseCharacter> currentParty = new List<BaseCharacter>();
        private const int MaxPartySize = 4;
        
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
        public void Clear() => currentParty.Clear();

        // 조회
        public IReadOnlyList<BaseCharacter> GetCurrentParty() => currentParty.AsReadOnly();
        public IReadOnlyList<BaseCharacterData> GetCurrentPartyDataOnly()
        {
            return currentParty.Select((character => character.Data)).ToList().AsReadOnly();
        }
        public int GetPartyCount() => currentParty.Count;
        
        
        public string Key => this.GetHashCode().ToString();
        public object Data => currentParty;
        public void Restore(object data)
        {
            currentParty = (List<BaseCharacter>)data;
        }
    }
}
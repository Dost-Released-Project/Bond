using System.Collections.Generic;

namespace Buffs
{
    /// <summary>
    /// 캐릭터에게 일정 턴 동안 부여되는 능력치 버프/디버프 1개.
    /// Modifiers 는 기존 StatController 경로(AddModifiers / RemoveModifiersFromSource)로 적용·해제된다.
    /// 공격력↑ 은 StatType.DamageMultiplier, 방어력↑ 은 StatType.DamageReduction 모디파이어를 쓰면
    /// 파이프라인(EntryStep / DefenseStep)이 라이브로 읽어 데미지 계산에 반영하므로 CalcStat 재계산이 필요 없다.
    /// 지속은 "버프 받은 캐릭터의 자기 턴" 수로 카운트한다(BaseCharacter 자기 턴 시작마다 TickBuffs 가 1 감소).
    /// </summary>
    public class ActiveBuff
    {
        /// <summary>중복/스택 판정용 식별자. 같은 Id 재부여 시 스택하지 않고 남은 지속만 갱신한다.</summary>
        public string Id;

        /// <summary>적용할 능력치 모디파이어. source 는 BaseCharacter.ApplyBuff 가 이 ActiveBuff 인스턴스로 채운다.</summary>
        public List<StatModifier> Modifiers = new List<StatModifier>();

        /// <summary>남은 지속(버프 받은 캐릭터의 자기 턴 수). 0 이하가 되면 만료·제거된다.</summary>
        public int RemainingTurns;

        public ActiveBuff() { }

        public ActiveBuff(string id, List<StatModifier> modifiers, int durationTurns)
        {
            Id = id;
            Modifiers = modifiers;
            RemainingTurns = durationTurns;
        }
    }
}

using UnityEngine;
using Reactions;

public enum E_TraitType
{
    None,
    Positive,
    Neutral,
    Negative,
}

/// <summary>
/// 성향(Trait) 데이터. 디자이너가 인스펙터에서 저작한다.
/// Id/DisplayName/Description 은 BaseSO 제공. 성향에 1:1로 고정된 리액션을 ReactionDefinition 으로 연결.
/// </summary>
[CreateAssetMenu(fileName = "Trait", menuName = "Bond/Trait/Trait")]
public class TraitSO : BaseSO
{
    [Header("성향")]
    [Tooltip("긍정/부정/중립 분류")]
    public E_TraitType Type = E_TraitType.None;

    [Tooltip("이 성향에 1:1로 고정된 리액션 정의. 비우면 리액션 없는 성향. " +
             "편집 가능 슬롯(관찰대상/행동스킬)이 있으면 플레이어가 채운다. " +
             "이 정의는 ReactionDefinitionDataBase 등 'DBSO' 라벨 DB에 포함돼 있어야 편집 UI가 해석 가능.")]
    public ReactionDefinitionSO ReactionDefinition;
}
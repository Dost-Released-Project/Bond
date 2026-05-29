using UnityEngine;

namespace Reactions
{
    /// <summary>
    /// ReactionDefinitionSO 들을 담는 DB. Addressables 에 "DBSO" 라벨을 붙이면
    /// 부트스트랩 PreloadByLabelAsync("DBSO") 에 자동 포함된다.
    /// 컨벤션 조회 키 = "ReactionDefinitionDataBase".
    /// </summary>
    [CreateAssetMenu(fileName = "ReactionDefinitionDataBase", menuName = "Bond/DBSO/ReactionDefinitionDataBase")]
    public class ReactionDefinitionDataBaseSO : DataBaseSO { }
}

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface ISkillEffectPool
{
    // 파티 스킬 사전 생성 — 맵씬 진입 시 호출
    UniTask WarmUpAsync(IReadOnlyList<BaseCharacter> party, CancellationToken cancellationToken = default);
    // 몬스터 스킬 추가 생성 — 전투 시작 직전, 적 캐릭터가 확정된 시점에 호출
    UniTask AddCharactersAsync(IReadOnlyList<BaseCharacter> characters, CancellationToken cancellationToken = default);
    void Play(string prefabAddress, Transform slotTransform);
    void ReturnAll();
    void Clear();
}

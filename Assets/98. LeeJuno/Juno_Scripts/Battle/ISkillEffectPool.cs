using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface ISkillEffectPool
{
    UniTask WarmUpAsync(IReadOnlyList<BaseCharacter> party, CancellationToken cancellationToken = default);
    void Play(string prefabAddress, Vector3 worldPosition);
    void ReturnAll();
    void Clear();
}

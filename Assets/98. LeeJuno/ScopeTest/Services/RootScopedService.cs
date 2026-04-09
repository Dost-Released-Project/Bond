using System;
using UnityEngine;

/// <summary>
/// RootScope에 Lifetime.Scoped로 등록되는 서비스.
///
/// 검증 목표:
///   - 씬 A Scope와 씬 B Scope가 각자 새 인스턴스를 받는지 (ID가 다른지)
///   - 씬 전환 시 씬 Scope 소멸과 함께 파괴되는지
///
/// 예상 결과: Scoped는 "이 컨테이너 안에서 1개" 규칙이므로
///   RootScope 자체에서 resolve하면 Root 컨테이너 소속 1개,
///   씬 Scope에서 resolve하면 씬 컨테이너 소속으로 새 인스턴스 생성.
/// </summary>
public class RootScopedService : IDisposable
{
    public string InstanceId { get; } = Guid.NewGuid().ToString()[..8];

    public RootScopedService()
    {
        Debug.Log($"[RootScopedService] 생성됨 - ID: {InstanceId}");
    }

    public void Dispose()
    {
        Debug.Log($"[RootScopedService] 파괴됨 - ID: {InstanceId}");
    }
}

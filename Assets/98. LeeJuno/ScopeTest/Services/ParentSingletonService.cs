using System;
using UnityEngine;

/// <summary>
/// ScopeTestParentScope(씬A)에 Lifetime.Singleton으로 등록되는 서비스.
///
/// 검증 목표:
///   - 자식 ScopeTestChildScope에서 동일 인스턴스를 받는지 (ID 동일 여부)
///   - 씬 전환(씬A 소멸) 시 파괴되는지
/// </summary>
public class ParentSingletonService : IDisposable
{
    public string InstanceId { get; } = Guid.NewGuid().ToString()[..8];

    public ParentSingletonService()
    {
        Debug.Log($"[ParentSingletonService] 생성됨 - ID: {InstanceId}");
    }

    public void Dispose()
    {
        Debug.Log($"[ParentSingletonService] 파괴됨 - ID: {InstanceId}");
    }
}

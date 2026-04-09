using System;
using UnityEngine;

/// <summary>
/// 부모 Scope에 Lifetime.Scoped로 등록되는 서비스.
/// 자식 Scope에서 오버라이드하지 않아도,
/// Scoped는 각 Scope 컨테이너마다 새 인스턴스를 생성한다.
///
/// 실측 결과: 부모/자식 InstanceId가 다름
/// → Lifetime.Scoped = "이 Scope 컨테이너 안에서 1개"
///    자식은 별개의 컨테이너이므로 자신의 Scoped 인스턴스를 따로 만든다.
/// cf. Lifetime.Singleton은 부모/자식 동일 인스턴스 (앱 전체에서 1개)
/// </summary>
public class ScopedService
{
    public string InstanceId { get; } = Guid.NewGuid().ToString()[..8];

    public ScopedService()
    {
        Debug.Log($"[ScopedService] 인스턴스 생성됨 - ID: {InstanceId}");
    }
}

using UnityEngine;

/// <summary>
/// 자식 Scope에만 등록되는 서비스.
/// 부모 Scope에서는 resolve 불가 → 자식 전용 의존성 검증용.
/// </summary>
public class ChildOnlyService
{
    public ChildOnlyService()
    {
        Debug.Log("[ChildOnlyService] 자식 Scope 전용 서비스 생성됨");
    }

    public void DoSomething() => Debug.Log("[ChildOnlyService] 자식 전용 기능 실행");
}

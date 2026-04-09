using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 자식 Scope EntryPoint.
/// 부모로부터 상속된 의존성과 자식 전용 의존성을 각각 검증한다.
///
/// 검증 항목:
///   1. SharedService.InstanceId  → 부모와 동일한 ID (Singleton 상속)
///   2. ScopedService.InstanceId  → 부모와 동일한 ID (Scoped, 오버라이드 없음 → 부모 인스턴스 공유)
///   3. ICounter 타입명           → CounterB (오버라이드 성공)
///   4. ChildOnlyService          → null 아님 (자식 전용 등록)
/// </summary>
public class ChildScopeEntryPoint : IStartable
{
    private readonly SharedService _shared;       // 부모 Scope에서 상속 (Singleton)
    private readonly ScopedService _scoped;       // 부모 Scope에서 상속 (Scoped, 오버라이드 없음)
    private readonly ChildOnlyService _childOnly; // 자식 Scope 전용
    private readonly ICounter _counter;           // 오버라이드된 CounterB

    [Inject]
    public ChildScopeEntryPoint(SharedService shared, ScopedService scoped,
        ChildOnlyService childOnly, ICounter counter)
    {
        _shared = shared;
        _scoped = scoped;
        _childOnly = childOnly;
        _counter = counter;
    }

    public void Start()
    {
        Debug.Log("========== [자식 Scope 검증] ==========");

        // 1. Singleton 상속
        Debug.Log($"[자식] SharedService ID : {_shared.InstanceId}  → 부모와 같아야 함 (Singleton)");

        // 2. Scoped 오버라이드 없음 → 자식 Scope가 독립적으로 새 인스턴스를 생성함 (실측 확인)
        Debug.Log($"[자식] ScopedService ID : {_scoped.InstanceId}  → 부모와 다름 (각 Scope 컨테이너마다 별도 인스턴스)");

        // 3. ICounter 오버라이드
        string counterType = _counter.GetType().Name;
        bool overrideSuccess = counterType == "CounterB";
        Debug.Log($"[자식] ICounter 타입    : {counterType}  → 기대값: CounterB  [{(overrideSuccess ? "성공" : "실패")}]");
        _counter.Increment();

        // 4. 자식 전용 서비스
        bool childOnlyExists = _childOnly != null;
        Debug.Log($"[자식] ChildOnlyService : {childOnlyExists}  → 기대값: True  [{(childOnlyExists ? "성공" : "실패")}]");
        _childOnly?.DoSomething();

        Debug.Log("========================================");
    }
}

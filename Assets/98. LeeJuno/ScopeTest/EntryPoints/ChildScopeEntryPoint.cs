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
    private readonly SharedService _shared;
    private readonly RootScopedService _rootScoped;
    private readonly ParentSingletonService _parentSingleton;
    private readonly ScopedService _scoped;
    private readonly ChildOnlyService _childOnly;
    private readonly ICounter _counter;

    [Inject]
    public ChildScopeEntryPoint(SharedService shared, RootScopedService rootScoped,
        ParentSingletonService parentSingleton, ScopedService scoped,
        ChildOnlyService childOnly, ICounter counter)
    {
        _shared = shared;
        _rootScoped = rootScoped;
        _parentSingleton = parentSingleton;
        _scoped = scoped;
        _childOnly = childOnly;
        _counter = counter;
    }

    public void Start()
    {
        Debug.Log("========== [자식 Scope 검증] ==========");

        // Singleton → Root/부모/자식 모두 동일
        Debug.Log($"<color=green>[자식] SharedService          ID: {_shared.InstanceId}         (Root_Singleton)   ← Root·부모와 동일해야 함</color>");
        Debug.Log($"<color=green>[자식] RootScopedService      ID: {_rootScoped.InstanceId}     (Root_Scoped)      ← 부모(씬A)와 같으면 부모 컨테이너 공유, 다르면 자식 컨테이너가 새로 생성</color>");
        Debug.Log($"<color=yellow>[자식] ParentSingletonService ID: {_parentSingleton.InstanceId} (Parent_Singleton) ← 부모와 동일해야 함 (Singleton 상속)</color>");
        Debug.Log($"<color=cyan>[자식] ScopedService          ID: {_scoped.InstanceId}         (Parent_Scoped)    ← 부모와 다름 (각 컨테이너마다 별도)</color>");

        string counterType = _counter.GetType().Name;
        Debug.Log($"[자식] ICounter 타입       : {counterType}  → 기대값: CounterB (오버라이딩)  [{(counterType == "CounterB" ? "성공" : "실패")}]");
        _counter.Increment();

        bool childOnlyExists = _childOnly != null;
        Debug.Log($"[자식] ChildOnlyService    : {childOnlyExists}  [{(childOnlyExists ? "성공" : "실패")}]");
        _childOnly?.DoSomething();

        Debug.Log("========================================");
    }
}

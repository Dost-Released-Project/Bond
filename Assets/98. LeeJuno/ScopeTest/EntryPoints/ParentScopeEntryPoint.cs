using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 부모 Scope EntryPoint.
/// 부모 Scope에서 resolve된 의존성 정보를 로그로 출력한다.
/// </summary>
public class ParentScopeEntryPoint : IStartable
{
    private readonly SharedService _shared;
    private readonly RootScopedService _rootScoped;
    private readonly ParentSingletonService _parentSingleton;
    private readonly ScopedService _scoped;
    private readonly ICounter _counter;

    [Inject]
    public ParentScopeEntryPoint(SharedService shared, RootScopedService rootScoped,
        ParentSingletonService parentSingleton, ScopedService scoped, ICounter counter)
    {
        _shared = shared;
        _rootScoped = rootScoped;
        _parentSingleton = parentSingleton;
        _scoped = scoped;
        _counter = counter;
    }

    public void Start()
    {
        Debug.Log("========== [씬 A Scope 검증] ==========");
        Debug.Log($"<color=green>[씬A] SharedService       ID: {_shared.InstanceId}         (Root_Singleton)   ← Root와 동일해야 함</color>");
        Debug.Log($"<color=green>[씬A] RootScopedService   ID: {_rootScoped.InstanceId}     (Root_Scoped)      ← Root와 다르면 씬 Scope가 새 인스턴스 생성</color>");
        Debug.Log($"<color=yellow>[씬A] ParentSingletonService ID: {_parentSingleton.InstanceId} (Parent_Singleton) ← 자식과 동일해야 함, 씬 전환 시 파괴</color>");
        Debug.Log($"<color=cyan>[씬A] ScopedService       ID: {_scoped.InstanceId}         (Parent_Scoped)    ← 씬 B와 달라야 함 (씬 전환 시 파괴)</color>");
        Debug.Log($"[씬A] ICounter 타입       : {_counter.GetType().Name}  → 기대값: CounterA");
        _counter.Increment();
    }
}

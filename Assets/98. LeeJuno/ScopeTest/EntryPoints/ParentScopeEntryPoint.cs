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
    private readonly ScopedService _scoped;
    private readonly ICounter _counter;

    [Inject]
    public ParentScopeEntryPoint(SharedService shared, ScopedService scoped, ICounter counter)
    {
        _shared = shared;
        _scoped = scoped;
        _counter = counter;
    }

    public void Start()
    {
        Debug.Log("========== [부모 Scope 검증] ==========");
        Debug.Log($"[부모] SharedService ID : {_shared.InstanceId} (Singleton) <- 자식과비교");
        Debug.Log($"[부모] ScopedService ID : {_scoped.InstanceId}  (Scoped) ← 자식과 비교");
        Debug.Log($"[부모] ICounter 타입    : {_counter.GetType().Name}  → 기대값: CounterA");
        _counter.Increment();
        Debug.Log("========================================");
    }
}

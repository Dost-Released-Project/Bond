using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// Root Scope EntryPoint.
/// 앱 시작 시 1회 실행되며, 씬이 바뀌어도 다시 실행되지 않는다.
/// SharedService ID를 출력해두면 씬 B에서 동일 ID인지 비교할 수 있다.
/// </summary>
public class RootScopeEntryPoint : IStartable
{
    private readonly SharedService _shared;
    private readonly RootScopedService _rootScoped;

    [Inject]
    public RootScopeEntryPoint(SharedService shared, RootScopedService rootScoped)
    {
        _shared = shared;
        _rootScoped = rootScoped;
    }

    public void Start()
    {
        Debug.Log("<color=green>========== 여기서 부터 보면됨 [Root Scope 시작] ==========</color>");
        Debug.Log($"<color=green>[Root] SharedService    ID: {_shared.InstanceId}      (Root_Singleton) ← 씬 전환 후에도 동일해야 함</color>");
        Debug.Log($"<color=green>[Root] RootScopedService ID: {_rootScoped.InstanceId} (Root_Scoped)    ← 씬 Scope에서는 새 인스턴스인지 확인</color>");
    }
}

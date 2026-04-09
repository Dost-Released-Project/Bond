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

    [Inject]
    public RootScopeEntryPoint(SharedService shared)
    {
        _shared = shared;
    }

    public void Start()
    {
        Debug.Log("========== [Root Scope 시작] ==========");
        Debug.Log($"[Root] SharedService ID: {_shared.InstanceId}  ← 씬 전환 후에도 이 ID가 유지되어야 함");
        Debug.Log("=======================================");
    }
}

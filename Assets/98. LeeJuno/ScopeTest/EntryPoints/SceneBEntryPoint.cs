using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 씬 B EntryPoint.
/// 씬 전환 후 SharedService의 ID를 출력한다.
/// Console에서 씬 A의 ID와 비교하면 Singleton 파괴 여부를 확인할 수 있다.
/// </summary>
public class SceneBEntryPoint : IStartable
{
    private readonly SharedService _shared;
    private readonly RootScopedService _rootScoped;

    [Inject]
    public SceneBEntryPoint(SharedService shared, RootScopedService rootScoped)
    {
        _shared = shared;
        _rootScoped = rootScoped;
    }

    public void Start()
    {
        Debug.Log("==========세로운 씬 [씬 B Scope 검증] ==========");
        Debug.Log($"<color=green>[씬B] SharedService     ID: {_shared.InstanceId}     (Root_Singleton) ← 씬 A와 동일해야 함</color>");
        Debug.Log($"<color=green>[씬B] RootScopedService ID: {_rootScoped.InstanceId} (Root_Scoped)    ← 씬 A와 다르면 씬 전환 시 파괴 후 새 인스턴스 생성</color>");
        Debug.Log("=======================================");
    }
}

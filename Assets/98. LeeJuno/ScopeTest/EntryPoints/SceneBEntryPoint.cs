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

    [Inject]
    public SceneBEntryPoint(SharedService shared)
    {
        _shared = shared;
    }

    public void Start()
    {
        Debug.Log("========== [씬 B - Singleton 생존 검증] ==========");
        Debug.Log($"[씬 B] SharedService ID: {_shared.InstanceId}");
        Debug.Log("  → 씬 A의 ID와 같으면 Singleton 생존, 다르면 파괴 후 재생성");
        Debug.Log("==================================================");
    }
}

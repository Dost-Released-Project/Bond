using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 
/// 씬 전환 후 SharedService의 ID를 출력한다.
/// 
/// </summary>
public class SceneCEntryPoint : IStartable
{
    private readonly SharedService _shared;

    [Inject]
    public SceneCEntryPoint(SharedService shared)
    {
        _shared = shared;
    }

    public void Start()
    {
        Debug.Log("========== [씬 C - Singleton 생존 검증] ==========");
        Debug.Log($"[씬 C] SharedService ID: {_shared.InstanceId}");
        Debug.Log("  → 씬 Root의 ID와 같으면 Singleton 생존, 다르면 파괴 후 재생성");
        Debug.Log("==================================================");
    }
}

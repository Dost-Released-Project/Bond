using System;
using UnityEngine;

public class SharedService : IDisposable
{
    public string InstanceId { get; } = Guid.NewGuid().ToString()[..8];

    public SharedService()
    {
        Debug.Log($"[SharedService] 생성됨 - ID: {InstanceId}");
    }

    public void Hello() => Debug.Log($"[SharedService] Hello from {InstanceId}");

    // VContainer는 Scope 파괴 시 IDisposable을 자동 호출
    public void Dispose()
    {
        Debug.Log($"[SharedService] 파괴됨 - ID: {InstanceId}  ← Scope가 사라졌음");
    }
}

using UnityEngine;

/// <summary>
/// 부모 Scope에 등록되는 ICounter 구현체.
/// 자식 Scope에서 CounterB로 오버라이드되는지 검증한다.
/// </summary>
public class CounterA : ICounter
{
    public int Count { get; private set; }

    public CounterA()
    {
        Debug.Log("[CounterA] 부모 Scope의 ICounter 구현체 생성됨");
    }

    public void Increment()
    {
        Count++;
        Debug.Log($"[CounterA] Count = {Count}");
    }
}

using UnityEngine;

/// <summary>
/// 자식 Scope에서 ICounter를 재등록(오버라이드)하는 구현체.
/// 자식 컨테이너 내에서 CounterA 대신 CounterB가 resolve되어야 한다.
/// </summary>
public class CounterB : ICounter
{
    public int Count { get; private set; }

    public CounterB()
    {
        Debug.Log("[CounterB] 자식 Scope의 ICounter 오버라이드 구현체 생성됨");
    }

    public void Increment()
    {
        Count += 10; // CounterA와 구분되는 동작
        Debug.Log($"[CounterB] Count = {Count} (+10씩 증가)");
    }
}

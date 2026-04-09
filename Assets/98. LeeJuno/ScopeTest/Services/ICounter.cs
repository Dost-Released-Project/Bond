/// <summary>
/// 부모/자식 Scope에서 동일 인터페이스를 다른 구현체로 등록하는 오버라이드 테스트용 인터페이스.
/// </summary>
public interface ICounter
{
    int Count { get; }
    void Increment();
}

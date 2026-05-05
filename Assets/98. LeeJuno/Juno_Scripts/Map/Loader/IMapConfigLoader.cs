using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// Config ScriptableObject 를 Addressables 로 비동기 로드하고
/// MapGenerator 에 전달할 패키지를 반환하는 인터페이스.
/// 로드 완료 후 반드시 ReleaseConfigs() 를 호출해 핸들을 해제한다.
/// </summary>
public interface IMapConfigLoader
{
    /// <summary>
    /// 모든 Config SO 를 Addressables 로 비동기 로드한다.
    /// cancellation 이 취소되면 Addressables 로드 작업도 함께 중단된다.
    /// 로드 실패 시 OperationCanceledException 또는 Exception 을 던진다.
    /// </summary>
    UniTask LoadAsync(CancellationToken cancellation = default);

    /// <summary>
    /// 로드된 Config 들을 MapGenerator 에 전달 가능한 패키지로 반환한다.
    /// LoadAsync() 완료 전에 호출하면 null 을 반환한다.
    /// </summary>
    MapConfigPackage GetPackage();

    /// <summary>
    /// 모든 AsyncOperationHandle 을 해제한다.
    /// MapData 생성 완료 직후 또는 챕터 종료 시점에 호출한다.
    /// </summary>
    void ReleaseConfigs();
}

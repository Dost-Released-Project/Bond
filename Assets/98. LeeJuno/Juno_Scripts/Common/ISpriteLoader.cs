using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Addressables 에서 Sprite 를 비동기 로드하고 AsyncOperationHandle 을 반환하는 인터페이스.
/// 핸들의 소유권은 호출자에게 있으므로, 호출자가 OnDestroy 등에서 Addressables.Release() 를 호출해야 한다.
/// </summary>
public interface ISpriteLoader
{
    /// <summary>
    /// address 에 해당하는 Sprite 를 Addressables 로 비동기 로드한다.
    /// 반환된 핸들은 호출자가 보관하고 사용 완료 후 Release 해야 한다.
    /// </summary>
    /// <param name="address">Addressables 키.</param>
    /// <returns>로드 완료된 AsyncOperationHandle. Status 로 성공/실패 여부를 판단한다.</returns>
    UniTask<AsyncOperationHandle<Sprite>> LoadAsync(string address);
}

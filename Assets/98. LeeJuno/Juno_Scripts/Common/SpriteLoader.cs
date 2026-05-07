using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Addressables 를 사용해 Sprite 를 비동기 로드하는 ISpriteLoader 구현체.
/// RootScope 에 Singleton 으로 등록되어 모든 하위 스코프에서 주입받을 수 있다.
/// 상태를 보유하지 않으므로 Singleton 수명에 문제없다.
/// </summary>
public class SpriteLoader : ISpriteLoader
{
    /// <summary>
    /// address 에 해당하는 Sprite 를 Addressables 로 비동기 로드한다.
    /// 핸들을 반환하며 소유권은 호출자에게 있다.
    /// 예외를 throw 하지 않고 핸들을 그대로 반환한다 — 호출자가 Status 로 성공/실패를 판단한다.
    /// </summary>
    public async UniTask<AsyncOperationHandle<Sprite>> LoadAsync(string address)
    {
        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(address);
        try
        {
            await handle.ToUniTask();
        }
        catch
        {
            // 예외를 삼키고 실패한 핸들을 반환한다 — 호출자가 Status 로 성공/실패를 판단한다.
        }
        return handle;
    }
}

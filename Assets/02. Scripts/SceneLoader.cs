using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using System;

public static class SceneLoader
{
    // 로드된 씬의 핸들을 보관 (나중에 언로드할 때 필요)
    private static AsyncOperationHandle<SceneInstance> _currentSceneHandle;

    /// <summary>
    /// 주소를 통해 씬을 로드합니다.
    /// </summary>
    /// <param name="address">어드레서블 주소</param>
    /// <param name="onComplete">로드 완료 후 실행할 콜백</param>
    public static void Load(string address, Action onComplete = null)
    {
        // 1. 이미 로드 중인 씬이 있다면 상황에 따라 처리 (선택 사항)
        if (_currentSceneHandle.IsValid())
        {
            // 필요하다면 이전 씬 메모리 해제 로직 추가
            // Addressables.UnloadSceneAsync(_currentSceneHandle);
        }

        // 2. 씬 로드 시작
        var handle = Addressables.LoadSceneAsync(address, LoadSceneMode.Single);
        
        handle.Completed += (op) =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                _currentSceneHandle = op;
                onComplete?.Invoke();
                Debug.Log($"[SceneLoader] {address} 로드 성공");
            }
            else
            {
                Debug.LogError($"[SceneLoader] {address} 로드 실패");
            }
        };
    }

    /// <summary>
    /// 현재 씬의 로딩 진행률을 가져옵니다 (0 ~ 1)
    /// </summary>
    public static float GetProgress()
    {
        if (_currentSceneHandle.IsValid() && !_currentSceneHandle.IsDone)
            return _currentSceneHandle.PercentComplete;
        
        return 1f;
    }
}
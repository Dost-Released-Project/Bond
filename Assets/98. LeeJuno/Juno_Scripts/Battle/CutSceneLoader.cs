using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// 스킬 컷씬 씬의 로드·재생 대기·언로드를 전담하는 서비스.
/// VContainer 의존성 없이 단독으로 사용하며 CutSceneCompletionChannel 을 통해
/// SkillCutSceneController 의 완료 신호를 수신한다.
///
/// 사용 흐름:
///   1. SkillCutSceneInjector → Load(sceneId) 호출
///   2. Load() 내부에서 시간 정지·컴포넌트 비활성화 후 씬 Additive 로드
///   3. SkillCutSceneController 가 CutSceneCompletionChannel.Invoke() 호출
///   4. Load() 내부 대기 해제 → 씬 언로드 → 컴포넌트 복구·시간 재개
///
/// 이중 호출 방지: _isLoading 플래그로 동시 호출을 차단한다.
/// </summary>
public class CutSceneLoader
{
    private bool _isLoading;

    // 컷씬 로드 중 비활성화한 컴포넌트 목록 — 언로드 후 복구한다
    private readonly List<Behaviour> _disabledBehaviours = new List<Behaviour>();
    private readonly List<UIDocument> _hiddenUIDocuments = new List<UIDocument>();

    private SceneInstance _cutSceneInstance;
    private bool _hasCutScene;

    public CutSceneLoader()
    {
        _isLoading  = false;
        _hasCutScene = false;
    }

    /// <summary>
    /// sceneId 에 대응하는 컷씬 씬을 Additive 로 로드하고 타임라인 완료까지 대기한 뒤 언로드한다.
    /// 반환 시점에 씬 로드·재생·언로드가 모두 완료되어 있음이 보장된다(자기완결형).
    /// </summary>
    /// <param name="sceneId">Addressables 씬 주소.</param>
    public async UniTask Load(string sceneId)
    {
        if (_isLoading)
        {
            Debug.LogWarning($"[CutSceneLoader] Load — 이미 컷씬 로딩 중입니다. sceneId={sceneId}");
            return;
        }

        _isLoading = true;

        // 람다식: 정적 채널 콜백을 로컬 UniTaskCompletionSource 와 연결하기 위해 사용한다
        //         별도 메서드로 분리하면 TCS 참조를 클로저로 캡처할 수 없다
        UniTaskCompletionSource cutSceneTcs = new UniTaskCompletionSource();
        Action onCompleted = () => cutSceneTcs.TrySetResult();

        try
        {
            Debug.Log($"[CutSceneLoader] 컷씬 로드 시작 — sceneId={sceneId}");

            // 잔류 컷씬이 있으면 먼저 정리한다 (비정상 종료 복구)
            if (_hasCutScene)
                await UnloadInternal();

            // 시간 정지 및 다른 씬 컴포넌트 비활성화
            Time.timeScale = 0f;
            DisableSceneComponents();

            // 완료 신호 채널에 콜백 등록
            CutSceneCompletionChannel.Register(onCompleted);

            // 컷씬 씬 Additive 로드
            AsyncOperationHandle<SceneInstance> handle =
                Addressables.LoadSceneAsync(sceneId, LoadSceneMode.Additive);

            try
            {
                _cutSceneInstance = await handle.ToUniTask();
                _hasCutScene = true;
            }
            catch (Exception e)
            {
                CutSceneCompletionChannel.Unregister();
                RestoreSceneComponents();
                Time.timeScale = 1f;
                Debug.LogError($"[CutSceneLoader] 컷씬 로드 실패: {e.Message}");
                _hasCutScene = false;
                throw;
            }

            // SkillCutSceneController 가 CutSceneCompletionChannel.Invoke() 를 호출할 때까지 대기
            await cutSceneTcs.Task;
            Debug.Log($"[CutSceneLoader] 컷씬 완료 신호 수신 — sceneId={sceneId}");

            await UnloadInternal();
        }
        finally
        {
            // 예외·정상 경로 모두에서 시간·컴포넌트를 반드시 복구한다
            RestoreSceneComponents();
            Time.timeScale = 1f;
            _isLoading = false;
        }
    }

    /// <summary>
    /// 컷씬 씬 언로드 내부 구현.
    /// CutSceneCompletionChannel 스택에서 콜백을 Pop 하고 씬을 언로드한다.
    /// </summary>
    private async UniTask UnloadInternal()
    {
        CutSceneCompletionChannel.Unregister();

        if (_hasCutScene == false)
            return;

        try
        {
            await Addressables.UnloadSceneAsync(_cutSceneInstance).ToUniTask();
        }
        catch (Exception e)
        {
            Debug.LogError($"[CutSceneLoader] 컷씬 언로드 실패: {e.Message}");
            throw;
        }
        finally
        {
            _hasCutScene = false;
        }
    }

    /// <summary>
    /// 현재 로드된 모든 씬의 Canvas, UIDocument, AudioListener, EventSystem 을 비활성화한다.
    /// 컷씬 씬이 화면을 독점할 수 있도록 다른 씬 UI·사운드·입력을 차단한다.
    /// 비활성화한 컴포넌트는 _disabledBehaviours 에 캐시해 복구 시 사용한다.
    /// </summary>
    private void DisableSceneComponents()
    {
        _disabledBehaviours.Clear();

        UIDocument[] uiDocuments = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        foreach (UIDocument uiDocument in uiDocuments)
        {
            if (uiDocument.rootVisualElement == null) continue;
            if (uiDocument.rootVisualElement.style.display == DisplayStyle.None) continue;

            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
            _hiddenUIDocuments.Add(uiDocument);
        }

        AudioListener[] audioListeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        foreach (AudioListener audioListener in audioListeners)
        {
            if (audioListener.enabled)
            {
                audioListener.enabled = false;
                _disabledBehaviours.Add(audioListener);
            }
        }

        EventSystem[] eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        foreach (EventSystem eventSystem in eventSystems)
        {
            if (eventSystem.enabled)
            {
                eventSystem.enabled = false;
                _disabledBehaviours.Add(eventSystem);
            }
        }
    }

    /// <summary>
    /// DisableSceneComponents() 로 비활성화한 컴포넌트를 복구한다.
    /// 씬 언로드 완료 후 또는 로드 실패 시 호출한다.
    /// </summary>
    private void RestoreSceneComponents()
    {
        foreach (Behaviour behaviour in _disabledBehaviours)
        {
            if (behaviour != null)
                behaviour.enabled = true;
        }
        _disabledBehaviours.Clear();

        foreach (UIDocument uiDocument in _hiddenUIDocuments)
        {
            if (uiDocument != null && uiDocument.rootVisualElement != null)
                uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        }
        _hiddenUIDocuments.Clear();
    }
}

using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 컷씬 씬에 배치하는 MonoBehaviour.
/// PlayableDirector 의 재생 완료를 감지해 CutSceneCompletionChannel.Invoke() 를 호출한다.
///
/// 완료 감지 방법:
///   1. PlayableDirector.stopped 이벤트 (기본 경로)
///   2. Update 폴백: state == Paused && time >= duration - 0.05 (이벤트 누락 대비)
///
/// timeUpdateMode 를 UnscaledGameTime 으로 강제 설정해
/// CutSceneLoader 가 Time.timeScale = 0f 로 설정해도 타임라인이 정상 재생된다.
/// </summary>
public class SkillCutSceneController : MonoBehaviour
{
    [SerializeField] private PlayableDirector _director;
    [SerializeField] private SpriteRenderer[] _enemyRenderers;

    private bool _hasInvoked;

    private void Awake()
    {
        if (_director == null)
            _director = FindFirstObjectByType<PlayableDirector>();

        if (_director == null)
        {
            Debug.LogError("[SkillCutSceneController] PlayableDirector 를 찾을 수 없습니다.");
            return;
        }

        // Time.timeScale = 0f 상태에서도 타임라인이 재생되도록 UnscaledGameTime 으로 고정한다
        _director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
        _director.stopped += OnDirectorStopped;
        _hasInvoked = false;
    }

    private void OnDestroy()
    {
        // Time.timeScale = 0 상태에서 설정한 오버라이드를 기본값으로 복원한다
        CinemachineCore.UniformDeltaTimeOverride = -1f;

        if (_director != null)
            _director.stopped -= OnDirectorStopped;
    }

    private void Update()
    {
        // Time.timeScale = 0 상태에서 Cinemachine 노이즈(카메라 흔들림)가 멈추는 문제를 방지한다.
        // UniformDeltaTimeOverride를 unscaledDeltaTime으로 설정하면 timeScale과 무관하게 노이즈가 업데이트된다.
        CinemachineCore.UniformDeltaTimeOverride = Time.unscaledDeltaTime;

        // stopped 이벤트가 누락된 경우를 대비한 폴백 감지
        // Paused 상태이고 재생 시간이 전체 길이에 근접하면 완료로 판정한다
        if (_hasInvoked)
            return;

        if (_director == null)
            return;

        bool isNearEnd = _director.state == PlayState.Paused
                         && _director.duration > 0d
                         && _director.time >= _director.duration - 0.05d;

        if (isNearEnd)
            NotifyCompletion();
    }

    public void SetEnemySprites(Sprite[] sprites)
    {
        if (_enemyRenderers == null || _enemyRenderers.Length == 0)
        {
            Debug.LogWarning("[SkillCutSceneController] _enemyRenderers 가 연결되지 않았습니다. Inspector 에서 SpriteRenderer 배열을 연결하세요.");
            return;
        }

        for (int i = 0; i < _enemyRenderers.Length; i++)
        {
            if (i >= sprites.Length) break;
            if (_enemyRenderers[i] == null) continue;
            _enemyRenderers[i].sprite = sprites[i];
        }
    }

    private void OnDirectorStopped(PlayableDirector director)
    {
        NotifyCompletion();
    }

    /// <summary>
    /// _hasInvoked 가드로 중복 호출을 방지하고 CutSceneCompletionChannel.Invoke() 를 호출한다.
    /// </summary>
    private void NotifyCompletion()
    {
        if (_hasInvoked)
            return;

        _hasInvoked = true;
        CutSceneCompletionChannel.Invoke();
    }
}

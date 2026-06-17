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
    [SerializeField] private SpriteRenderer _enemyRenderer;

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
        if (_director != null)
            _director.stopped -= OnDirectorStopped;
    }

    private void Update()
    {
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

    public void SetEnemySprite(Sprite sprite)
    {
        if (_enemyRenderer == null)
        {
            Debug.LogWarning("[SkillCutSceneController] _enemyRenderer 가 연결되지 않았습니다. Inspector 에서 SpriteRenderer 를 연결하세요.");
            return;
        }

        _enemyRenderer.sprite = sprite;
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

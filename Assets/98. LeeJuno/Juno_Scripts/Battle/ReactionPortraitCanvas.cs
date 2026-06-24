using Cysharp.Threading.Tasks;
using Reactions;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// 리액션 발동 시 활성화되는 초상화 캔버스의 제어 컴포넌트.
/// IReactionPortraitCanvas 구현체. ReactionPortraitCanvas 프리팹에 부착한다.
/// </summary>
public class ReactionPortraitCanvas : MonoBehaviour, IReactionPortraitCanvas
{
    /// <summary>
    /// 초상화 스프라이트를 표시할 Image 컴포넌트. Inspector 에서 연결한다.
    /// </summary>
    [SerializeField] private Image _portraitImage;

    /// <summary>
    /// 리액션 결과 대사를 표시할 TMP_Text 컴포넌트. Inspector 에서 연결한다.
    /// </summary>
    [SerializeField] private TextMeshProUGUI _reactionText;

    /// <summary>
    /// 초상화가 표시되는 시간(초). Inspector 에서 조정한다.
    /// </summary>
    [SerializeField] private float _displayDuration = 1.5f;

    private ISpriteLoader _spriteLoader;
    private AsyncOperationHandle<Sprite> _currentHandle;

    /// <summary>
    /// VContainer 필드 인젝션. ISpriteLoader 는 RootScope Singleton.
    /// </summary>
    [Inject]
    public void Construct(ISpriteLoader spriteLoader)
    {
        _spriteLoader = spriteLoader;
    }

    /// <summary>
    /// 지정한 Addressables 키로 초상화 스프라이트를 로드해 캔버스를 활성화한다.
    /// ReactionResult 에 따라 대사 텍스트를 설정한다.
    /// Time.timeScale 을 0 으로 멈춘 뒤 _displayDuration 이 지나면 자동으로 비활성화하고 timeScale 을 복원한다.
    /// </summary>
    /// <param name="imageAddress">BaseCharacter.ImageAddress — Addressables 키.</param>
    /// <param name="result">리액션 판정 결과. 텍스트 설정에 사용된다.</param>
    public async UniTask ShowAsync(string imageAddress, ReactionResult result)
    {
        ReleaseCurrentHandle();

        if (string.IsNullOrEmpty(imageAddress) == false && _spriteLoader != null)
        {
            _currentHandle = await _spriteLoader.LoadAsync(imageAddress);
            if (_currentHandle.Status == AsyncOperationStatus.Succeeded)
            {
                _portraitImage.sprite = _currentHandle.Result;
            }
        }

        SetReactionText(result);

        gameObject.SetActive(true);
        Time.timeScale = 0f;

        try
        {
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(_displayDuration),
                DelayType.UnscaledDeltaTime);
        }
        finally
        {
            Time.timeScale = 1f;
            gameObject.SetActive(false);
            ReleaseCurrentHandle();
        }
    }

    /// <summary>
    /// 초상화 캔버스를 즉시 비활성화하고 timeScale 을 복원한다. 비정상 종료 시 정리용.
    /// </summary>
    public void Hide()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
        ReleaseCurrentHandle();
    }

    private void OnDestroy()
    {
        ReleaseCurrentHandle();
    }

    /// <summary>
    /// ReactionResult 값에 따라 대사 텍스트를 설정한다.
    /// </summary>
    /// <param name="result">리액션 판정 결과.</param>
    private void SetReactionText(ReactionResult result)
    {
        if (_reactionText == null)
        {
            return;
        }

        // switch 식을 사용해 결과별 대사를 간결하게 매핑한다.
        _reactionText.text = result switch
        {
            ReactionResult.BondAwakening => "긍정!",
            ReactionResult.Anomaly       => "부정!",
            _                            => "보통!",   // Default 및 미정의 값
        };
    }

    /// <summary>
    /// 보유 중인 Addressables 핸들을 해제한다.
    /// </summary>
    private void ReleaseCurrentHandle()
    {
        if (_currentHandle.IsValid())
        {
            Addressables.Release(_currentHandle);
        }
    }
}

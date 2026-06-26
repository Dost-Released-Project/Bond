using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// SilhouetteTrack의 믹서. SpriteRenderer 전용.
/// 정적 공유 머티리얼로 교체 후 MaterialPropertyBlock으로 per-renderer 색상을 설정한다.
/// 머티리얼은 SilhouetteTrack.silhouetteMaterial에서 주입받는다. Shader.Find를 사용하지 않으므로 빌드에서 안전하다.
/// </summary>
public class SilhouetteMixerBehaviour : PlayableBehaviour
{
    // SilhouetteTrack.CreateTrackMixer에서 SetMaterial()로 주입된다
    private static Material _sharedSilhouetteMaterial;

    // 매 프레임 string 룩업을 피하기 위해 PropertyID를 정적으로 캐싱한다
    private static readonly int SilhouetteColorId = Shader.PropertyToID("_SilhouetteColor");
    private static readonly int BlendFactorId     = Shader.PropertyToID("_BlendFactor");

    private SpriteRenderer _spriteRenderer;
    private Material _originalMaterial;
    private MaterialPropertyBlock _mpb;
    private bool _initialized;
    private bool _isSilhouetteActive;

    /// <summary>
    /// 매 프레임 Timeline이 호출한다. playerData는 트랙에 바인딩된 SpriteRenderer다.
    /// </summary>
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        SpriteRenderer boundRenderer = playerData as SpriteRenderer;

        if (boundRenderer == null)
        {
            return;
        }

        if (_initialized == false)
        {
            CacheTarget(boundRenderer);
        }

        int inputCount = playable.GetInputCount();
        float totalWeight = 0f;
        SilhouetteColorMode activeColorMode = SilhouetteColorMode.Black;

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);

            if (weight <= 0f)
            {
                continue;
            }

            ScriptPlayable<SilhouetteClipBehaviour> inputPlayable =
                (ScriptPlayable<SilhouetteClipBehaviour>)playable.GetInput(i);

            SilhouetteClipBehaviour clipBehaviour = inputPlayable.GetBehaviour();

            float clipTime     = (float)inputPlayable.GetTime();
            float clipDuration = (float)inputPlayable.GetDuration();
            float fadeWeight   = CalculateFadeWeight(clipTime, clipDuration, clipBehaviour.fadeInDuration, clipBehaviour.fadeOutDuration);

            activeColorMode = clipBehaviour.colorMode;
            totalWeight    += weight * fadeWeight;
        }

        if (totalWeight > 0f)
        {
            ApplyColor(activeColorMode, totalWeight);
        }
        else
        {
            RestoreOriginalColor();
        }
    }

    /// <summary>
    /// Timeline 정지 또는 트랙 바인딩 해제 시 호출된다.
    /// 원본 머티리얼 복원 및 PropertyBlock 초기화를 수행한다.
    /// 정적 머티리얼은 해제하지 않는다.
    /// </summary>
    public override void OnPlayableDestroy(Playable playable)
    {
        // _isSilhouetteActive 플래그 대신 실제 material 참조를 비교한다.
        // 에디터 PlayableGraph 재빌드 타이밍에 플래그가 동기화되지 않아도
        // renderer가 실루엣 머티리얼을 사용 중이라면 반드시 복원한다.
        if (_spriteRenderer != null && _sharedSilhouetteMaterial != null
            && _spriteRenderer.sharedMaterial == _sharedSilhouetteMaterial)
        {
            _spriteRenderer.sharedMaterial = _originalMaterial;
            _spriteRenderer.SetPropertyBlock(null);
        }

        _isSilhouetteActive = false;
        _spriteRenderer     = null;
        _originalMaterial   = null;
        _mpb                = null;
        _initialized        = false;
    }

    /// <summary>
    /// SilhouetteTrack.CreateTrackMixer에서 호출한다.
    /// Inspector에서 연결한 머티리얼을 정적 필드에 주입한다.
    /// </summary>
    /// <param name="material">SilhouetteTrack.silhouetteMaterial.</param>
    public void SetMaterial(Material material)
    {
        if (material != null)
            _sharedSilhouetteMaterial = material;
        else
            Debug.LogError("[SilhouetteMixerBehaviour] silhouetteMaterial이 null입니다. SilhouetteTrack Inspector에서 머티리얼을 연결하세요.");
    }

    /// <summary>
    /// 바인딩 SpriteRenderer의 원본 sharedMaterial을 저장하고 MaterialPropertyBlock을 준비한다.
    /// </summary>
    private void CacheTarget(SpriteRenderer target)
    {
        _spriteRenderer   = target;
        _originalMaterial = target.sharedMaterial;
        _mpb              = new MaterialPropertyBlock();
        _initialized      = true;
    }

    /// <summary>
    /// 실루엣 머티리얼로 교체하고 MaterialPropertyBlock으로 색상·BlendFactor를 설정한다.
    /// 머티리얼 교체는 비활성 상태일 때 1회만 수행한다.
    /// </summary>
    private void ApplyColor(SilhouetteColorMode colorMode, float totalWeight)
    {
        if (_isSilhouetteActive == false)
        {
            _spriteRenderer.sharedMaterial = _sharedSilhouetteMaterial;
            _isSilhouetteActive            = true;
        }

        Color silhouetteColor = colorMode == SilhouetteColorMode.Black ? Color.black : Color.white;
        _mpb.SetColor(SilhouetteColorId, silhouetteColor);
        // totalWeight(0~1)을 BlendFactor로 사용해 원본↔실루엣 색상을 보간한다
        _mpb.SetFloat(BlendFactorId, totalWeight);
        _spriteRenderer.SetPropertyBlock(_mpb);
    }

    /// <summary>
    /// 원본 머티리얼로 복원하고 PropertyBlock을 초기화한다.
    /// 클립 구간 밖(totalWeight == 0)에서 호출된다.
    /// </summary>
    private void RestoreOriginalColor()
    {
        if (_isSilhouetteActive == false)
        {
            return;
        }

        if (_spriteRenderer != null && _originalMaterial != null)
        {
            _spriteRenderer.sharedMaterial = _originalMaterial;
            _spriteRenderer.SetPropertyBlock(null);
        }

        _isSilhouetteActive = false;
    }

    /// <summary>
    /// 클립 내부 시간 기반으로 페이드 weight를 계산한다.
    /// fadeIn/fadeOut이 0이면 즉시 전환(1 반환).
    /// </summary>
    private float CalculateFadeWeight(float clipTime, float clipDuration, float fadeIn, float fadeOut)
    {
        if (fadeIn > 0f && clipTime < fadeIn)
        {
            return Mathf.Clamp01(clipTime / fadeIn);
        }

        if (fadeOut > 0f && clipTime > clipDuration - fadeOut)
        {
            return Mathf.Clamp01((clipDuration - clipTime) / fadeOut);
        }

        return 1f;
    }
}

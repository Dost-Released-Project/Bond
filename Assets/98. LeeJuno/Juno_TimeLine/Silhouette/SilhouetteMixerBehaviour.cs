using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// SilhouetteTrack의 믹서. SpriteRenderer 전용.
/// 바인딩된 SpriteRenderer에 per-인스턴스 머티리얼을 사용해 실루엣 색상을 전환한다.
/// 머티리얼은 CacheTarget()에서 1회 생성하고 OnPlayableDestroy()에서 해제한다.
/// MaterialPropertyBlock을 사용하지 않으므로 SRP Batcher와 호환된다.
/// </summary>
public class SilhouetteMixerBehaviour : PlayableBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private Material _originalMaterial;
    private Material _silhouetteMaterial;
    private bool _originalColorSaved;
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

        if (_originalColorSaved == false)
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
    /// 원본 머티리얼 복원, 생성한 인스턴스 머티리얼 해제, 참조 초기화를 수행한다.
    /// </summary>
    public override void OnPlayableDestroy(Playable playable)
    {
        // _isSilhouetteActive 플래그 대신 실제 material 참조를 비교한다.
        // 에디터 PlayableGraph 재빌드 타이밍에 플래그가 동기화되지 않아도
        // renderer가 실루엣 머티리얼을 사용 중이라면 Destroy 전에 반드시 복원한다.
        if (_spriteRenderer != null && _silhouetteMaterial != null
            && _spriteRenderer.sharedMaterial == _silhouetteMaterial)
        {
            _spriteRenderer.sharedMaterial = _originalMaterial;
        }

        _isSilhouetteActive = false;

        if (_silhouetteMaterial != null)
        {
            // CacheTarget에서 new Material()로 생성한 인스턴스를 해제한다
            Object.Destroy(_silhouetteMaterial);
            _silhouetteMaterial = null;
        }

        _spriteRenderer     = null;
        _originalMaterial   = null;
        _originalColorSaved = false;
    }

    /// <summary>
    /// 바인딩 SpriteRenderer의 원본 sharedMaterial을 저장하고
    /// 실루엣용 머티리얼 인스턴스를 1회 생성한다.
    /// </summary>
    private void CacheTarget(SpriteRenderer target)
    {
        _spriteRenderer   = target;
        _originalMaterial = target.sharedMaterial;
        // per-인스턴스 머티리얼 — MPB 없이 SRP Batcher 호환
        _silhouetteMaterial  = new Material(Shader.Find("JunoTimeline/SilhouetteOverride"));
        _originalColorSaved  = true;
    }

    /// <summary>
    /// 실루엣 머티리얼로 교체하고 색상·BlendFactor를 설정한다.
    /// 머티리얼 교체는 비활성 상태일 때 1회만 수행한다.
    /// </summary>
    private void ApplyColor(SilhouetteColorMode colorMode, float totalWeight)
    {
        if (_isSilhouetteActive == false)
        {
            _spriteRenderer.material = _silhouetteMaterial;
            _isSilhouetteActive      = true;
        }

        Color silhouetteColor = colorMode == SilhouetteColorMode.Black ? Color.black : Color.white;
        _silhouetteMaterial.SetColor("_SilhouetteColor", silhouetteColor);
        // totalWeight(0~1)을 BlendFactor로 사용해 원본↔실루엣 색상을 보간한다
        _silhouetteMaterial.SetFloat("_BlendFactor", totalWeight);
    }

    /// <summary>
    /// 원본 머티리얼로 복원한다. 클립 구간 밖(totalWeight == 0)에서 호출된다.
    /// </summary>
    private void RestoreOriginalColor()
    {
        if (_isSilhouetteActive == false)
        {
            return;
        }

        // _originalMaterial이 null이면 설정하지 않는다 (None 방지)
        if (_spriteRenderer != null && _originalMaterial != null)
        {
            _spriteRenderer.sharedMaterial = _originalMaterial;
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

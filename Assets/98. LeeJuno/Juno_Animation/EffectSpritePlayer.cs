using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class EffectSpritePlayer : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Sprite[] _sprites;
    [SerializeField] private float _fps = 12f;
    [SerializeField] private bool _loop = false;
    [SerializeField] private bool _playOnStart = true;
    [SerializeField] private bool _ignoreTimeScale = false;

    private CancellationToken _destroyToken;
    private int _playGeneration;

    private void Awake()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        }

        _destroyToken = destroyCancellationToken;
    }

    private void OnEnable()
    {
        if (_playOnStart == false) return;
        this.enabled = true;
        Play();
    }

    private void OnDisable()
    {
        _playGeneration++;
    }

    public void Play()
    {
        if (_sprites == null || _sprites.Length == 0) return;

        _playGeneration++;
        int generation = _playGeneration;
        PlayAsync(generation).Forget();
    }

    private async UniTaskVoid PlayAsync(int generation)
    {
        float interval = 1f / _fps;

        while (true)
        {
            for (int i = 0; i < _sprites.Length; i++)
            {
                if (_playGeneration != generation) return;

                _spriteRenderer.sprite = _sprites[i];

                DelayType delayType = _ignoreTimeScale ? DelayType.UnscaledDeltaTime : DelayType.DeltaTime;
                bool isCancelled = await UniTask.Delay(TimeSpan.FromSeconds(interval), delayType, cancellationToken: _destroyToken)
                    .SuppressCancellationThrow();

                if (isCancelled) return;
            }

            if (_playGeneration != generation) return;

            if (_loop == false)
            {
                this.enabled = false;
                return;
            }
        }
    }
}
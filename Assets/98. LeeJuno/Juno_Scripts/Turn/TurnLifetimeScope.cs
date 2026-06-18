using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using juno_Test;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class TurnLifetimeScope : LifetimeScope
{
    [SerializeField] private TurnUI _ui;
    [SerializeField] private SkillCutSceneConfig _skillCutSceneConfig;
    [SerializeField] private Canvas _portraitCanvas;
    [SerializeField] private float _portraitDuration = 1.5f;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterEntryPoint<TurnManager>(Lifetime.Singleton).AsSelf();

        // 씬에 배치된 MonoBehaviour 를 DI 대상으로 등록
        if (_ui != null)
            builder.RegisterComponent(_ui);
        else
            Debug.LogError("[TurnLifetimeScope] _ui 가 연결되지 않았습니다. Inspector 에서 TurnUI 를 연결하세요.", this);

        // 스킬 컷씬 관련 서비스 — 전투씬에서만 사용하므로 TurnLifetimeScope 에 등록한다
        if (_skillCutSceneConfig != null)
            builder.RegisterInstance(_skillCutSceneConfig);
        else
            Debug.LogError("[TurnLifetimeScope] _skillCutSceneConfig 가 연결되지 않았습니다.", this);

        builder.Register<CutSceneLoader>(Lifetime.Singleton)
            .WithParameter("portraitCanvas", _portraitCanvas)
            .WithParameter("portraitDuration", _portraitDuration);
        builder.Register<SkillCutSceneInjector>(Lifetime.Singleton);
    }
}
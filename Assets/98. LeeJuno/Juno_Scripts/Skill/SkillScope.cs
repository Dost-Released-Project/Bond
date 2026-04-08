using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// SkillManager를 VContainer Singleton으로 등록하는 LifetimeScope.
/// Inspector에서 모든 SkillData 에셋을 _allSkillData 배열에 직접 할당한다.
///
/// [씬 설정 방법]
///  TurnLifetimeScope의 Parent 필드에 이 SkillScope 오브젝트를 지정한다.
///    → TurnManager가 생성자를 통해 ISkillManager를 주입받을 수 있게 된다.
/// </summary>
public class SkillScope : LifetimeScope
{
    [Header("스킬 데이터 — 모든 SkillData 에셋을 여기에 할당")]
    [SerializeField] private SkillData[] _allSkillData;

    protected override void Configure(IContainerBuilder builder)
    {
        // SkillData 배열을 생성자 인자로 전달해 SkillManager를 Singleton으로 등록
        builder.Register<SkillManager>(Lifetime.Singleton)
               .WithParameter(_allSkillData)
               .As<ISkillManager>();
    }
}

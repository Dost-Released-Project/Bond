using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Reactions;
using static Reactions.Authoring.ReactionDefBuilder;

namespace Reactions.Authoring
{
    /// <summary>
    /// 현재 구현 가능한 성향(트레잇)을 코드로 저작 (Editor 전용).
    /// 트레잇마다 ReactionDefinitionSO(빌더) + TraitSO(분류 + 리액션 참조)를 생성하고 각 DB에 등록한다.
    /// 재실행 안전(Id 기준 제자리 갱신, GUID 보존). Addressables "DBSO" 라벨은 최초 1회 수동.
    ///
    /// 미포함: 006·014(크리티컬 활성화 선행), 009 "실패 시 +25", 019 "적 반격 유도".
    /// 제외(범위 밖/보류): 007·010·011·012·015·017·020.
    /// CastSkill skillIndex(0)는 임시 — 트레잇이 의도한 공격 스킬 인덱스로 조정 권장.
    /// </summary>
    public static class TraitReactionCatalogGenerator
    {
        private const string ReactionFolder = "Assets/Data/Reactions";
        private const string TraitFolder    = "Assets/Data/Traits";

        [MenuItem("Bond/Reactions/구현가능 성향 카탈로그 생성")]
        public static void Generate()
        {
            var defs   = new List<ReactionDefinitionSO>();
            var traits = new List<TraitSO>();

            void Add(string id, string ko, E_TraitType type, ReactionDefBuilder builder)
            {
                var def = builder.Build(ReactionFolder);
                defs.Add(def);
                traits.Add(BuildTrait(id, ko, type, def));
            }

            // 001 겁쟁이: 자기 턴 HP30%↓ → 후열 이탈 + 전체 봉인
            Add("TRT_001", "겁쟁이", E_TraitType.Negative,
                Def("TRT_001_RDEF", RoleType.None).Name("겁쟁이 — 후방 이탈", "본인 HP 30% 이하 시 설계 스킬 무시, 후열로 강제 이동. 해당 턴 리액션 슬롯 전부 봉인")
                    .Phase(E_ReactionPhase.OnSelfTurn).Observe(E_ObserveFilter.Self)
                    .When(HpBelow(0.3f))
                    .Do(MoveBack(), Seal(SealKind.All, turns: 1)));

            // 002 호전적: 적 처치 → 가장 가까운 적 추가공격 + 다음 턴 봉인
            Add("TRT_002", "호전적", E_TraitType.Neutral,
                Def("TRT_002_RDEF", RoleType.None).Name("호전적 — 무계획 추격")
                    .Phase(E_ReactionPhase.PostApply).Observe(E_ObserveFilter.Self)
                    .When(SubjectIs(E_TargetFilter.Caster), Killed())
                    .Do(CastSkill(E_TargetFilter.FrontmostEnemy, skillIndex: 0), Seal(SealKind.All, turns: 2)));

            // 003 희생적: HP30%↓ 아군 피격예고 → 대신 맞기 + 본인 스트레스+20
            Add("TRT_003", "희생적", E_TraitType.Positive,
                Def("TRT_003_RDEF", RoleType.None).Name("희생적 — 무계획 방어")
                    .Phase(E_ReactionPhase.PreApply).Observe(E_ObserveFilter.OtherAlly)
                    .When(SubjectIs(E_TargetFilter.Target), HpBelow(0.3f), SkillTypeIs(SkillType.OFFENSIVE, SkillType.SPELL))
                    .Do(Intercept(), Stress(20)));

            // 004 의심많은: 아군 공격 빗나감 → 해당 라운드 전체 봉인
            Add("TRT_004", "의심 많은", E_TraitType.Negative,
                Def("TRT_004_RDEF", RoleType.None).Name("의심 많은 — 단독 행동")
                    .Phase(E_ReactionPhase.PostApply).Observe(E_ObserveFilter.OtherAlly)
                    .When(SubjectIs(E_TargetFilter.Caster), Evaded())
                    .Do(Seal(SealKind.All, turns: 1)));

            // 005 냉혹한: 아군 HP30%↓ → 그 아군 설계 리액션 봉인 + 그 아군 스트레스+15
            Add("TRT_005", "냉혹한", E_TraitType.Negative,
                Def("TRT_005_RDEF", RoleType.None).Name("냉혹한 — 방치")
                    .Phase(E_ReactionPhase.PostApply).Observe(E_ObserveFilter.OtherAlly)
                    .When(SubjectIs(E_TargetFilter.Target), HpBelow(0.3f))
                    .Do(Seal(SealKind.DesignedOnly, turns: 1, self: false), Stress(15, E_TargetFilter.Observed)));

            // 006 허세: 아군 치명타 → 그 대상에 마무리 + 연쇄 리셋 (※크리 활성화 선행)
            Add("TRT_006", "허세 부리는", E_TraitType.Neutral,
                Def("TRT_006_RDEF", RoleType.None).Name("허세 — 끼어들기")
                    .Phase(E_ReactionPhase.PostApply).Observe(E_ObserveFilter.OtherAlly)
                    .When(SubjectIs(E_TargetFilter.Caster), Crit())
                    .Do(CastSkill(E_TargetFilter.Target, skillIndex: 0), ResetChainCount()));

            // 008 복수심: 본인 피격 → 공격자 반격 + 역할 리액션 봉인
            Add("TRT_008", "복수심 강한", E_TraitType.Neutral,
                Def("TRT_008_RDEF", RoleType.None).Name("복수심 — 충동 반격")
                    .Phase(E_ReactionPhase.PostApply).Observe(E_ObserveFilter.Self)
                    .When(SubjectIs(E_TargetFilter.Target), Hit())
                    .Do(CastSkill(E_TargetFilter.Caster, skillIndex: 0), Seal(SealKind.DesignedOnly, turns: 1)));

            // 009 인정욕구: 파티 평균 스트레스 60%↑ → 전열 돌파 (※"실패 시 +25" 미포함)
            Add("TRT_009", "인정욕구 강한", E_TraitType.Neutral,
                Def("TRT_009_RDEF", RoleType.None).Name("인정욕구 — 무모한 돌격")
                    .Phase(E_ReactionPhase.OnSelfTurn).Observe(E_ObserveFilter.Self)
                    .When(PartyStressAbove(60f))
                    .Do(MoveFront()));

            // 013 쉽게지침: 연속 3회 리액션 → 탈진(행동 생략) + 전체 봉인
            Add("TRT_013", "쉽게 지치는", E_TraitType.Negative,
                Def("TRT_013_RDEF", RoleType.None).Name("쉽게 지치는 — 강제 휴식")
                    .Phase(E_ReactionPhase.OnSelfTurn).Observe(E_ObserveFilter.Self)
                    .When(ReactionCountAtLeast(3))
                    .Do(Seal(SealKind.All, turns: 1)));

            // 014 과시욕: 본인 치명타 → 남은 리액션 봉인 (※크리 활성화 선행)
            Add("TRT_014", "과시욕 있는", E_TraitType.Neutral,
                Def("TRT_014_RDEF", RoleType.None).Name("과시욕 — 독무대")
                    .Phase(E_ReactionPhase.PostApply).Observe(E_ObserveFilter.Self)
                    .When(SubjectIs(E_TargetFilter.Caster), Crit())
                    .Do(Seal(SealKind.All, turns: 1)));

            // 016 규율적: 아군 돌발 후 내 턴 → 자기통제(리액션 1개 봉인)
            Add("TRT_016", "규율적인", E_TraitType.Positive,
                Def("TRT_016_RDEF", RoleType.None).Name("규율적 — 자기 통제")
                    .Phase(E_ReactionPhase.OnSelfTurn).Observe(E_ObserveFilter.Self)
                    .When(AllyAnomaly())
                    .Do(Seal(SealKind.Slots, turns: 1, count: 1)));

            // 018 완벽주의: 본인 공격 빗나감 → 다음 턴 1개 봉인 + 스트레스+10
            Add("TRT_018", "완벽주의적", E_TraitType.Negative,
                Def("TRT_018_RDEF", RoleType.None).Name("완벽주의 — 자기 처벌")
                    .Phase(E_ReactionPhase.PostApply).Observe(E_ObserveFilter.Self)
                    .When(SubjectIs(E_TargetFilter.Caster), Evaded())
                    .Do(Seal(SealKind.Slots, turns: 2, count: 1), Stress(10)));

            // 019 무모한: 자기 턴 HP70%↑ → 전열 + 가장 먼 적 단독 돌진 (※"적 반격 유도" 미포함)
            Add("TRT_019", "무모한", E_TraitType.Neutral,
                Def("TRT_019_RDEF", RoleType.None).Name("무모한 — 과신한 돌진")
                    .Phase(E_ReactionPhase.OnSelfTurn).Observe(E_ObserveFilter.Self)
                    .When(HpAbove(0.7f))
                    .Do(MoveFront(), CastSkill(E_TargetFilter.BackmostEnemy, skillIndex: 0)));

            // ── 등록 ──────────────────────────────────────────
            var rdb = ReactionAuthoringIO.FindOrCreateDatabase<ReactionDefinitionDataBaseSO>(ReactionFolder, "ReactionDefinitionDataBase");
            ReactionAuthoringIO.RegisterInDatabase(rdb, defs);

            var tdb = ReactionAuthoringIO.FindOrCreateDatabase<TraitDataBaseSO>(TraitFolder, "TraitDataBase");
            ReactionAuthoringIO.RegisterInDatabase(tdb, traits);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=lime>[TraitCatalog]</color> 구현가능 성향 {traits.Count}종 저작 완료 " +
                      $"(리액션 정의 {defs.Count} → {ReactionFolder}, 트레잇 → {TraitFolder})\n" +
                      $"※ 최초 1회: ReactionDefinitionDataBase / TraitDataBase 에 Addressables \"DBSO\" 라벨 부착.\n" +
                      $"※ 006·014는 크리티컬 활성화 후 발동. CastSkill skillIndex(0)는 의도한 스킬 인덱스로 조정 권장.");
            Selection.activeObject = tdb;
            EditorGUIUtility.PingObject(tdb);
        }

        private static TraitSO BuildTrait(string id, string displayName, E_TraitType type, ReactionDefinitionSO def)
        {
            var trait = ScriptableObject.CreateInstance<TraitSO>();
            trait.Type = type;
            trait.ReactionDefinition = def;
            ReactionAuthoringIO.SetBaseSoIds(trait, id, displayName, "");
            return ReactionAuthoringIO.Persist(trait, TraitFolder, id);
        }
    }
}

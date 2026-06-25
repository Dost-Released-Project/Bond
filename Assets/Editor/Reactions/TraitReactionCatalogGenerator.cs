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

            // 001 겁쟁이: HP30%↓ - Default 후열 이동. 해당 턴 리액션 봉인 / Alt 스트레스 -20
            Add("TRT_001", "겁쟁이", E_TraitType.Negative,
                Def("TRT_001_RDEF", RoleType.None).Name("겁쟁이 — 후방 이탈", "HP30% 이하일때, Default: 후열로 이동. 해당 턴 리액션 봉인 / Alt: 스트레스 -20")
                    .Phase(E_ReactionPhase.PostApply).Observe(E_ObserveFilter.Self)
                    .When(SubjectIs(E_TargetFilter.Target), HpBelow(0.3f), SkillTypeIs(SkillType.OFFENSIVE, SkillType.SPELL))
                    .Do(MoveBack(), Seal(SealKind.All, turns: 1))
                    .Alt(Stress(-20)));

            // 002 호전적: 적 처치 시 — Default 전열 이동+가까운 적 추가공격 / Alt 자기 공격버프+제자리 공격(불가 시 무행동)
            Add("TRT_002", "호전적", E_TraitType.Neutral,
                Def("TRT_002_RDEF", RoleType.None).Name("호전적 — 무계획 추격", "적 처치 시, Default: 전열 이동+가까운 적 추가공격 / Alt: 자기 공격버프+제자리 공격")
                    .Phase(E_ReactionPhase.PostApply).Observe(E_ObserveFilter.Self)
                    .When(SubjectIs(E_TargetFilter.Caster), Killed())
                    .Do(MoveFront(), CastRandomAttack(E_TargetFilter.FrontmostEnemy))
                    .Alt(Buff(StatType.DamageMultiplier, 0.3f, turns: 2, to: E_TargetFilter.Self), CastRandomAttack(E_TargetFilter.FrontmostEnemy)));

            // 004 의심많은: 아군 공격 빗나감 시 — Default 그 아군 불협조 / Alt 무효과(대사 연출만)
            Add("TRT_004", "의심 많은", E_TraitType.Negative,
                Def("TRT_004_RDEF", RoleType.None).Name("의심 많은 — 단독 행동", "아군 공격 빗나감 시, Default: 그 아군 불협조(리액션·보조·보호 차단) / Alt: 무효과")
                    .Phase(E_ReactionPhase.PostApply).Observe(E_ObserveFilter.OtherAlly)
                    .When(SubjectIs(E_TargetFilter.Caster), Evaded())
                    .Do(Distrust(turns: 1))
                    .Alt(NoAction()));

            // 014 과시욕: 본인 치명타 시 — Default 남은 리액션 봉인+스트레스-10 / Alt 봉인 없이 스트레스-5
            Add("TRT_014", "과시욕 있는", E_TraitType.Neutral,
                Def("TRT_014_RDEF", RoleType.None).Name("과시욕 — 독무대", "본인 치명타 시, Default: 이번 턴동안 리액션 봉인+스트레스-10 / Alt: 봉인 없이 스트레스-5")
                    .Phase(E_ReactionPhase.PostApply).Observe(E_ObserveFilter.Self)
                    .When(SubjectIs(E_TargetFilter.Caster), Crit())
                    .Do(Seal(SealKind.All, turns: 1), Stress(-10))
                    .Alt(Stress(-5)));

            // ── 등록 ──────────────────────────────────────────
            var rdb = ReactionAuthoringIO.FindOrCreateDatabase<ReactionDefinitionDataBaseSO>(ReactionFolder, "ReactionDefinitionDataBase");
            ReactionAuthoringIO.RegisterInDatabase(rdb, defs);

            var tdb = ReactionAuthoringIO.FindOrCreateDatabase<TraitDataBaseSO>(TraitFolder, "TraitDataBase");
            ReactionAuthoringIO.RegisterInDatabase(tdb, traits);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=lime>[TraitCatalog]</color> 구현가능 성향 {traits.Count}종 저작 완료 " +
                      $"(리액션 정의 {defs.Count} → {ReactionFolder}, 트레잇 → {TraitFolder})\n" +
                      $"※ 최초 1회: ReactionDefinitionDataBase / TraitDataBase 에 Addressables \"DBSO\" 라벨 부착.\n");
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

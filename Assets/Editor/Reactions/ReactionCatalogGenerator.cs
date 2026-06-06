using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Reactions;
using static Reactions.Authoring.ReactionDefBuilder;

namespace Reactions.Authoring
{
    /// <summary>
    /// 역할 리액션 카탈로그를 코드로 저작하는 생성기 (Editor 전용).
    /// 메뉴 한 번 클릭으로 ReactionDefinitionSO 들을 만들고 DB 등록 + "DBSO" 라벨까지 처리한다.
    /// 재실행해도 같은 Id 는 제자리 갱신(GUID 보존)이라 안전하다.
    ///
    /// 새 리액션 추가 = 아래 리스트에 Def(...) 한 블록 추가. 새 조건/행동 "타입"이 필요할 때만 코드(어휘) 확장.
    /// </summary>
    public static class ReactionCatalogGenerator
    {
        private const string Folder = "Assets/Data/Reactions";

        [MenuItem("Bond/Reactions/역할 리액션 카탈로그 생성")]
        public static void Generate()
        {
            var defs = new List<ReactionDefinitionSO>();

            // ── 딜러 ──────────────────────────────────────────
            // 1) 자신이 회피했을 때 → 공격한 적(Caster)에게 지정 공격스킬
            defs.Add(
                Def("RACT_DEAL_COUNTER_EVADE", RoleType.Dealer).Name("회피 반격", "공격을 회피하면 공격자에게 지정 공격스킬로 반격")
                    .Phase(E_ReactionPhase.PostApply).Observe(E_ObserveFilter.Self)
                    .When(SubjectIs(E_TargetFilter.Target), Evaded())
                    .Do(CastSkill(E_TargetFilter.Caster))
                    .Editable(ActionSkill("반격 스킬", SkillType.OFFENSIVE, SkillType.SPELL))
                    .Build(Folder));

            // 2) 자신이 치명타를 가했을 때 → 맞은 대상(Target)에게 지정 공격스킬
            //    ※ 현재 CriticalStep 이 isCritical=false 고정이라, 크리 굴림 복구 전까지는 발화하지 않음(데이터는 정상).
            defs.Add(
                Def("RACT_DEAL_COUNTER_CRIT", RoleType.Dealer).Name("치명타 추격", "치명타를 가하면 그 대상에게 지정 공격스킬로 추격")
                    .Phase(E_ReactionPhase.PostApply).Observe(E_ObserveFilter.Self)
                    .When(SubjectIs(E_TargetFilter.Caster), Crit())
                    .Do(CastSkill(E_TargetFilter.Target))
                    .Editable(ActionSkill("추격 스킬", SkillType.OFFENSIVE, SkillType.SPELL))
                    .Build(Folder));

            // ── 탱커 ──────────────────────────────────────────
            // 3) 지정 아군이 공격 대상이 될 때 → 대신 맞기
            defs.Add(
                Def("RACT_TANK_COVER", RoleType.Tanker).Name("엄호", "지정한 아군이 공격 대상이 되면 대신 맞는다")
                    .Phase(E_ReactionPhase.PreApply).Observe(E_ObserveFilter.Specific)
                    .When(SubjectIs(E_TargetFilter.Target), SkillTypeIs(SkillType.OFFENSIVE, SkillType.SPELL))
                    .Do(Intercept())
                    .Editable(ObserveTarget("보호 대상"))
                    .Build(Folder));

            // 4) 아군(자기 제외)이 HP 20% 이하에서 공격 대상이 될 때 → 대신 맞기
            defs.Add(
                Def("RACT_TANK_COVER_LOWHP", RoleType.Tanker).Name("위기 엄호", "체력 20% 이하 아군이 공격 대상이 되면 대신 맞는다")
                    .Phase(E_ReactionPhase.PreApply).Observe(E_ObserveFilter.OtherAlly)
                    .When(SubjectIs(E_TargetFilter.Target), SkillTypeIs(SkillType.OFFENSIVE, SkillType.SPELL), HpBelow(0.2f))
                    .Do(Intercept())
                    .Build(Folder));

            // ── 서포터 ────────────────────────────────────────
            // 5) 지정 아군이 공격 스킬을 사용할 때 → 그 아군에게 공격력(DamageMultiplier) 버프
            //    PreApply 이므로(파이프라인상 PreApply→Entry) 트리거된 그 공격부터 적용된다.
            defs.Add(
                Def("RACT_SUP_ATKUP", RoleType.Supporter).Name("지원 공격 강화", "지정 아군이 공격할 때 공격력 버프 부여")
                    .Phase(E_ReactionPhase.PreApply).Observe(E_ObserveFilter.Specific)
                    .When(SubjectIs(E_TargetFilter.Caster), SkillTypeIs(SkillType.OFFENSIVE, SkillType.SPELL))
                    .Do(Buff(StatType.DamageMultiplier, 0.3f, turns: 2, to: E_TargetFilter.Observed, buffId: "atk_up"))
                    .Editable(ObserveTarget("지원 대상"))
                    .Build(Folder));

            // 6) 지정 아군이 공격 대상이 될 때 → 그 아군에게 방어력(DamageReduction) 버프
            //    PreApply→Defense 순서라 그 공격을 실제로 경감한다.
            defs.Add(
                Def("RACT_SUP_DEFUP", RoleType.Supporter).Name("지원 방어 강화", "지정 아군이 공격 대상이 되면 방어력 버프 부여")
                    .Phase(E_ReactionPhase.PreApply).Observe(E_ObserveFilter.Specific)
                    .When(SubjectIs(E_TargetFilter.Target), SkillTypeIs(SkillType.OFFENSIVE, SkillType.SPELL))
                    .Do(Buff(StatType.DamageReduction, 0.2f, turns: 2, to: E_TargetFilter.Observed, buffId: "def_up"))
                    .Editable(ObserveTarget("보호 대상"))
                    .Build(Folder));

            // ── 등록 + 저장 ──────────────────────────────────
            var db = ReactionAuthoringIO.FindOrCreateDatabase<ReactionDefinitionDataBaseSO>(Folder, "ReactionDefinitionDataBase");
            ReactionAuthoringIO.RegisterInDatabase(db, defs);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=lime>[ReactionCatalog]</color> 역할 리액션 {defs.Count}개 생성·DB 등록 완료 → {Folder}\n" +
                      $"※ 최초 1회: DB 에셋('{db.name}')을 Addressables 그룹에 넣고 \"DBSO\" 라벨을 부착하세요. " +
                      $"그래야 부트스트랩 PreloadByLabelAsync(\"DBSO\")로 로드됩니다.");
            Selection.activeObject = db;
            EditorGUIUtility.PingObject(db);
        }
    }
}

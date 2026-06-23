using System.Linq;
using Reactions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 리액션 Default/Alt 분기를 강제하고, 판정 확률을 몬테카를로로 검증하는 디버그 윈도우(에디터 전용).
/// 강제: JudgeReaction 의 확률 굴림을 건너뛰고 고정 분기를 반환 → 각 리액션의 BaseEffect/AltEffect 를 결정적으로 확인.
/// 검증: 선택한 캐릭터·리액션으로 JudgeReaction 을 N회 굴려 실측 분기 비율을 기대 확률과 비교. (Bond/리액션 분기 디버그)
/// </summary>
public class ReactionBranchDebugWindow : EditorWindow
{
    private static readonly string[] _labels = { "Off (정상 판정)", "Default 강제", "Alt 강제" };

    private int _charIdx;
    private int _reactionIdx;
    private int _iterations = 10000;
    private string _mcResult;

    [MenuItem("Bond/Reactions/리액션 분기 디버그", priority = 10)]
    public static void Open() => GetWindow<ReactionBranchDebugWindow>("리액션 분기");

    // 플레이 진입/종료 등 외부 상태 변화를 윈도우에 반영.
    private void OnInspectorUpdate() => Repaint();

    private void OnGUI()
    {
        DrawForceSection();
        EditorGUILayout.Space();
        DrawMonteCarloSection();
    }

    // ── 분기 강제 ───────────────────────────────────────────────
    private void DrawForceSection()
    {
        EditorGUILayout.LabelField("리액션 분기 강제", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "JudgeReaction 의 확률 굴림을 건너뛰고 모든 리액션을 지정 분기로 고정합니다.\n" +
            "ForceAlt 는 성향=BondAwakening / 역할=Anomaly 로 자동 매핑되어 후속 분기(MarkAnomaly 등)까지 재현됩니다.",
            MessageType.None);

        int idx = (int)BaseCharacter.DebugBranchForce;
        int next = GUILayout.Toolbar(idx, _labels);
        if (next != idx)
            BaseCharacter.DebugBranchForce = (BaseCharacter.ReactionBranchForce)next;

        EditorGUILayout.Space();

        switch (BaseCharacter.DebugBranchForce)
        {
            case BaseCharacter.ReactionBranchForce.Off:
                EditorGUILayout.HelpBox("정상 판정 — 관계·스트레스·INT 확률로 분기합니다.", MessageType.Info);
                break;
            case BaseCharacter.ReactionBranchForce.ForceDefault:
                EditorGUILayout.HelpBox("모든 리액션이 BaseEffect(평상시)로 고정됩니다.", MessageType.Warning);
                break;
            case BaseCharacter.ReactionBranchForce.ForceAlt:
                EditorGUILayout.HelpBox("모든 리액션이 AltEffect(대체)로 고정됩니다. (미저작 시 BaseEffect 폴백)", MessageType.Warning);
                break;
        }

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox(
                "플레이 모드에서만 실제로 적용됩니다. 플레이 진입 시 Off 로 초기화될 수 있습니다(도메인 리로드).",
                MessageType.Info);
    }

    // ── 몬테카를로 검증 ─────────────────────────────────────────
    private void DrawMonteCarloSection()
    {
        EditorGUILayout.LabelField("몬테카를로 검증", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "선택한 캐릭터·리액션으로 JudgeReaction 을 N회 굴려 실측 분기 비율을 기대 확률과 비교합니다.\n" +
            "현재 캐릭터 상태(관계=파티 평균, 스트레스, INT) 기준입니다.",
            MessageType.None);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("플레이 모드에서만 가능합니다.", MessageType.Info);
            return;
        }

        var chars = BaseCharacter.Dict.Values.Where(c => c != null).ToList();
        if (chars.Count == 0)
        {
            EditorGUILayout.HelpBox("등록된 캐릭터가 없습니다.", MessageType.Warning);
            return;
        }

        _charIdx = Mathf.Clamp(_charIdx, 0, chars.Count - 1);
        _charIdx = EditorGUILayout.Popup("캐릭터", _charIdx,
            chars.Select(c => $"{c.Name} ({c.Id})").ToArray());
        var chara = chars[_charIdx];

        var reactions = chara.Reactions.Where(r => r != null && r.Trigger != null).ToList();
        if (reactions.Count == 0)
        {
            EditorGUILayout.HelpBox("이 캐릭터에 유효한 리액션(트리거 있음)이 없습니다.", MessageType.Warning);
            return;
        }

        _reactionIdx = Mathf.Clamp(_reactionIdx, 0, reactions.Count - 1);
        var reactionLabels = reactions
            .Select((r, i) => $"{i}: {(chara.IsTraitReaction(r) ? "성향" : "역할")} | {r.Trigger.Description}")
            .ToArray();
        _reactionIdx = EditorGUILayout.Popup("리액션", _reactionIdx, reactionLabels);
        var reaction = reactions[_reactionIdx];

        _iterations = Mathf.Max(1, EditorGUILayout.IntField("시행 횟수", _iterations));

        if (GUILayout.Button("굴리기"))
            _mcResult = RunMonteCarlo(chara, reaction, _iterations);

        if (!string.IsNullOrEmpty(_mcResult))
            EditorGUILayout.HelpBox(_mcResult, MessageType.None);
    }

    // JudgeReaction 을 N회 호출(부작용 없음)해 Default/Alt 빈도를 세고 기대 확률과 비교한다.
    private static string RunMonteCarlo(BaseCharacter chara, Reaction reaction, int n)
    {
        var subjects = System.Array.Empty<BaseCharacter>(); // 현재 상태(관계=파티 평균)로 굴림
        var (chance, isTrait, relation) = chara.DebugJudgeProbe(reaction, subjects);

        // 강제 토글이 켜져 있으면 실측이 오염되므로 측정 동안만 Off 로 되돌린다.
        var saved = BaseCharacter.DebugBranchForce;
        BaseCharacter.DebugBranchForce = BaseCharacter.ReactionBranchForce.Off;

        int alt = 0;
        for (int i = 0; i < n; i++)
            if (chara.JudgeReaction(reaction, subjects) != ReactionResult.Default)
                alt++;

        BaseCharacter.DebugBranchForce = saved;

        float altPct = 100f * alt / n;
        float expPct = chance * 100f;
        string altKind = isTrait ? "BondAwakening(강화)" : "Anomaly(돌발)";
        return
            $"{(isTrait ? "성향" : "역할")} 리액션 | relation = {relation}\n" +
            $"기대 Alt 확률 : {expPct:F1}%  ({altKind})\n" +
            $"실측 Alt      : {alt} / {n} = {altPct:F1}%\n" +
            $"실측 Default  : {n - alt} / {n} = {100f - altPct:F1}%\n" +
            $"오차          : {Mathf.Abs(altPct - expPct):F2} %p";
    }
}

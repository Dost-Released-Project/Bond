using UnityEditor;
using UnityEngine;

/// <summary>
/// 리액션 Default/Alt 분기를 강제하는 디버그 윈도우(에디터 전용).
/// JudgeReaction 의 확률 굴림을 건너뛰고 고정 분기를 반환하게 해, 각 리액션의
/// BaseEffect/AltEffect 를 결정적으로 한 번씩 확인할 수 있다. (Tools/Bond/리액션 분기 디버그)
/// </summary>
public class ReactionBranchDebugWindow : EditorWindow
{
    private static readonly string[] _labels = { "Off (정상 판정)", "Default 강제", "Alt 강제" };

    [MenuItem("Bond/리액션 분기 디버그")]
    public static void Open() => GetWindow<ReactionBranchDebugWindow>("리액션 분기");

    // 플레이 진입/종료 등 외부 상태 변화를 윈도우에 반영.
    private void OnInspectorUpdate() => Repaint();

    private void OnGUI()
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

        EditorGUILayout.Space();
        if (!Application.isPlaying)
            EditorGUILayout.HelpBox(
                "플레이 모드에서만 실제로 적용됩니다. 플레이 진입 시 Off 로 초기화될 수 있습니다(도메인 리로드).",
                MessageType.Info);
    }
}

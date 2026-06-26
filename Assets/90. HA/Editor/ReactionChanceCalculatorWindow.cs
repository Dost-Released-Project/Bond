using UnityEditor;
using UnityEngine;

/// <summary>
/// GetAnomalyChance / GetBondAwakeningChance 의 확률 수식을 상수·런타임 변수로 직접 입력해
/// 최종 분기 확률을 미리 보는 튜닝용 윈도우(에디터 전용). 순수 계산이라 플레이 모드가 필요 없다.
/// 수식·기본값은 두 함수를 손으로 복제한 것 — 코드 수식/상수가 바뀌면 같이 고칠 것. (Bond/리액션 확률 계산기)
/// </summary>
public class ReactionChanceCalculatorWindow : EditorWindow
{
    // 분기별 상수는 따로 보존해 토글을 오가도 입력이 유지된다.
    // 기본값 = GetAnomalyChance / GetBondAwakeningChance 안의 const 를 손으로 복제한 값.
    private static readonly string[] _branchLabels = { "역할 (Anomaly)", "성향 (BondAwakening)" };
    private int _branch; // 0 = 역할(Anomaly), 1 = 성향(BondAwakening)

    // 역할(Anomaly) 상수 — GetAnomalyChance 의 const 와 동기화
    private float _aBase = 0f, _aStress = 0.0035f, _aInt = 0.01f, _aRelation = 0.002f, _aMin = 0.05f;
    // 성향(BondAwakening) 상수 — GetBondAwakeningChance 의 const 와 동기화
    private float _bBase = 0f, _bStress = 0.005f, _bInt = 0.001f, _bRelation = 0.005f, _bMin = 0f;

    // 런타임 변수 입력(두 분기 공유): 스트레스=Insanity(int 0~100), 지능=Stat.INT(float), 관계=relation(int)
    private int _inStress;
    private float _inInt;
    private int _inRelation;

    [MenuItem("Bond/Reactions/리액션 확률 계산기", priority = 10)]
    public static void Open()
    {
        var window = GetWindow<ReactionChanceCalculatorWindow>("확률 계산기");
        window.minSize = new Vector2(360f, 360f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("확률 계산기 (수식 튜닝)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "상수 5개와 런타임 변수(스트레스·지능·관계)를 직접 입력해 최종 분기 확률을 미리 확인합니다.\n" +
            "GetAnomalyChance / GetBondAwakeningChance 의 수식을 복제했습니다 — 코드 수식 변경 시 동기화 필요. 플레이 모드 불필요.",
            MessageType.None);

        _branch = GUILayout.Toolbar(_branch, _branchLabels);
        EditorGUILayout.Space();

        // ── 상수 5개 (분기별 보존) ──
        EditorGUILayout.LabelField("상수", EditorStyles.miniBoldLabel);
        if (_branch == 0)
        {
            _aBase     = EditorGUILayout.FloatField("기본 확률 (baseRate)", _aBase);
            _aStress   = EditorGUILayout.FloatField("스트레스 계수 (stressCoef)", _aStress);
            _aInt      = EditorGUILayout.FloatField("지능 계수 (intCoef)", _aInt);
            _aRelation = EditorGUILayout.FloatField("관계 계수 (relationCoef)", _aRelation);
            _aMin      = EditorGUILayout.FloatField("최저 확률 (minRate)", _aMin);
        }
        else
        {
            _bBase     = EditorGUILayout.FloatField("기본 확률 (baseRate)", _bBase);
            _bStress   = EditorGUILayout.FloatField("스트레스 계수 (stressCoef)", _bStress);
            _bInt      = EditorGUILayout.FloatField("지능 계수 (intCoef)", _bInt);
            _bRelation = EditorGUILayout.FloatField("관계 계수 (relationCoef)", _bRelation);
            _bMin      = EditorGUILayout.FloatField("최저 확률 (minRate)", _bMin);
        }

        EditorGUILayout.Space();

        // ── 런타임 변수 (두 분기 공유) ──
        EditorGUILayout.LabelField("런타임 변수", EditorStyles.miniBoldLabel);
        _inStress   = EditorGUILayout.IntField("스트레스 (Insanity 0~100)", _inStress);
        _inInt      = EditorGUILayout.FloatField("지능 (Stat.INT)", _inInt);
        _inRelation = EditorGUILayout.IntField("관계 (relation)", _inRelation);

        if (GUILayout.Button("코드 기본값으로 리셋"))
            ResetDefaults();

        EditorGUILayout.Space();

        // ── 계산 (GetAnomalyChance / GetBondAwakeningChance 와 동일한 수식) ──
        float baseRate, stressCoef, intCoef, relationCoef, minRate;
        if (_branch == 0)
        { baseRate = _aBase; stressCoef = _aStress; intCoef = _aInt; relationCoef = _aRelation; minRate = _aMin; }
        else
        { baseRate = _bBase; stressCoef = _bStress; intCoef = _bInt; relationCoef = _bRelation; minRate = _bMin; }

        float stressTerm   = _inStress * stressCoef;
        float intTerm      = _inInt * intCoef;
        float relationTerm = _inRelation * relationCoef;

        float raw;
        string formula;
        if (_branch == 0)
        {
            // 역할: 관계↓·스트레스↑·지능↓ 일수록 ↑ → 지능·관계는 뺀다.
            raw = baseRate + stressTerm - intTerm - relationTerm;
            formula =
                "raw = base + 스트레스×stressCoef − 지능×intCoef − 관계×relationCoef\n" +
                $"    = {baseRate:0.#####} + {stressTerm:0.#####} − {intTerm:0.#####} − {relationTerm:0.#####}\n" +
                $"    = {raw:0.#####}";
        }
        else
        {
            // 성향: 스트레스↓·지능↑·관계↑ 일수록 ↑ → 스트레스만 뺀다.
            raw = baseRate - stressTerm + intTerm + relationTerm;
            formula =
                "raw = base − 스트레스×stressCoef + 지능×intCoef + 관계×relationCoef\n" +
                $"    = {baseRate:0.#####} − {stressTerm:0.#####} + {intTerm:0.#####} + {relationTerm:0.#####}\n" +
                $"    = {raw:0.#####}";
        }

        float final = Mathf.Clamp(raw, minRate, 1f);
        bool clamped = !Mathf.Approximately(final, raw);

        EditorGUILayout.HelpBox(formula, MessageType.None);

        string clampNote = !clamped ? ""
            : final > raw ? $"  (minRate {minRate:0.#####} 하한 적용)"
            : "  (1.0 상한 적용)";
        EditorGUILayout.LabelField("최종 확률", $"{final * 100f:F2} %{clampNote}", EditorStyles.boldLabel);

        Rect bar = EditorGUILayout.GetControlRect(false, 18f);
        EditorGUI.ProgressBar(bar, final, $"{final * 100f:F2} %");
    }

    // 상수 입력을 코드의 const 기본값으로 되돌린다.
    private void ResetDefaults()
    {
        _aBase = 0f; _aStress = 0.0035f; _aInt = 0.01f; _aRelation = 0.002f; _aMin = 0.05f;
        _bBase = 0f; _bStress = 0.002f; _bInt = 0.003f; _bRelation = 0.005f; _bMin = 0f;
        GUI.FocusControl(null); // 포커스된 필드가 옛 입력값을 다시 덮어쓰지 않게 포커스 해제
    }
}

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bond.UI.Editor
{
    // ─────────────────────────────────────────────────────────────────────────
    // 테마 교체 방식:
    //   PanelSettings.themeStyleSheet 는 ThemeStyleSheet 타입(.tss)을 요구하며
    //   일반 .uss(StyleSheet)를 직접 대입할 수 없다.
    //   따라서 EoB_Variables.uss 를 참조하는 모든 UXML 파일을 탐색하여
    //   기존 EoB_Theme_*.uss src 속성을 선택한 테마 경로로 치환하는 방식을 사용.
    // ─────────────────────────────────────────────────────────────────────────
    public class EoB_ThemeSwitcher : EditorWindow
    {
        private const string THEME_DARK_AMBER = "Assets/UI/Themes/EoB_Theme_DarkAmber.uss";
        private const string THEME_DARK_BLUE  = "Assets/UI/Themes/EoB_Theme_DarkBlue.uss";

        private const string PREF_THEME = "EoB_SelectedTheme";
        private const string PREF_SCALE = "EoB_FontScale";

        private static readonly string[] ThemeNames = { "DarkAmber", "DarkBlue" };
        private static readonly string[] ThemePaths = { THEME_DARK_AMBER, THEME_DARK_BLUE };

        private static readonly Dictionary<string, float> FontBaseSizes = new()
        {
            { "--font-size-header", 11f },
            { "--font-size-name",   13f },
            { "--font-size-class",   9f },
            { "--font-size-body",   12f },
            { "--font-size-button", 11f },
            { "--font-size-system",  9f },
        };

        private int    _selectedTheme;
        private float  _fontScale = 1.0f;
        private string _status    = "";
        private bool   _statusOk;

        [MenuItem("Tools/EoB/Theme Switcher")]
        public static void ShowWindow() =>
            GetWindow<EoB_ThemeSwitcher>("EoB Theme Switcher").minSize = new Vector2(360, 340);

        private void OnEnable()
        {
            _selectedTheme = EditorPrefs.GetInt(PREF_THEME, 0);
            _fontScale     = EditorPrefs.GetFloat(PREF_SCALE, 1.0f);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);

            // ── 테마 선택 ──
            EditorGUILayout.LabelField("테마 선택", EditorStyles.boldLabel);
            _selectedTheme = EditorGUILayout.Popup(_selectedTheme, ThemeNames);

            EditorGUILayout.Space(10);

            // ── 폰트 배율 ──
            EditorGUILayout.LabelField("폰트 배율", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _fontScale = EditorGUILayout.Slider(_fontScale, 0.8f, 1.4f);
            EditorGUILayout.LabelField($"{_fontScale:F2}x", GUILayout.Width(44));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // ── 미리보기 ──
            EditorGUILayout.LabelField("크기 미리보기", EditorStyles.boldLabel);
            foreach (var kvp in FontBaseSizes)
            {
                int    px  = Mathf.RoundToInt(kvp.Value * _fontScale);
                string key = kvp.Key.Replace("--font-size-", "");

                var style = new GUIStyle(EditorStyles.label)
                {
                    fontSize = px,
                    wordWrap = false
                };
                GUILayout.Label($"{key}  ({kvp.Value}px → {px}px)", style);
            }

            EditorGUILayout.Space(10);

            // ── 버튼 ──
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply", GUILayout.Height(28))) Apply();
            if (GUILayout.Button("Reset Scale", GUILayout.Height(28)))
            {
                _fontScale = 1.0f;
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            // ── 상태 메시지 ──
            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox(_status, _statusOk ? MessageType.Info : MessageType.Warning);
            }
        }

        private void Apply()
        {
            string themePath = ThemePaths[_selectedTheme];

            if (!File.Exists(themePath))
            {
                SetStatus($"테마 파일 없음: {themePath}", ok: false);
                return;
            }

            // 1. 프로젝트 내 모든 PanelSettings에 선택된 테마 USS 적용
            int psCount = ApplyToPanelSettings(themePath);

            // 2. 선택된 테마 USS 파일에 폰트 배율 적용
            ApplyFontScaleToFile(themePath);

            // 3. 저장
            EditorPrefs.SetInt(PREF_THEME, _selectedTheme);
            EditorPrefs.SetFloat(PREF_SCALE, _fontScale);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            SetStatus($"✓ 완료 — {ThemeNames[_selectedTheme]} / {_fontScale:F2}x  (PanelSettings {psCount}개 갱신)", ok: true);
        }

        // 프로젝트 내 모든 PanelSettings의 themeStyleSheet 필드를 교체한다.
        // Unity 내부 직렬화 필드명: m_ThemeStyleSheet
        private static int ApplyToPanelSettings(string themePath)
        {
            var themeAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>(themePath);
            if (themeAsset == null) return 0;

            var guids = AssetDatabase.FindAssets("t:PanelSettings");
            int count = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ps   = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
                if (ps == null) continue;

                var so   = new SerializedObject(ps);
                var prop = so.FindProperty("m_ThemeStyleSheet");
                if (prop == null) prop = so.FindProperty("themeStyleSheet"); // fallback
                if (prop == null) continue;

                prop.objectReferenceValue = themeAsset;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(ps);
                count++;
            }

            return count;
        }

        // 선택된 테마 USS 파일의 --font-size-* 값을 배율 적용 후 덮어쓴다.
        private void ApplyFontScaleToFile(string path)
        {
            string content = File.ReadAllText(path);

            foreach (var kvp in FontBaseSizes)
            {
                float  computed = kvp.Value * _fontScale;
                string pattern  = $@"{Regex.Escape(kvp.Key)}\s*:\s*[\d.]+px";
                content = Regex.Replace(content, pattern, $"{kvp.Key}: {computed:F1}px");
            }

            content = Regex.Replace(
                content,
                @"--font-scale\s*:\s*[\d.]+",
                $"--font-scale:       {_fontScale:F2}");

            File.WriteAllText(path, content);
        }

        private void SetStatus(string msg, bool ok)
        {
            _status   = msg;
            _statusOk = ok;
            Repaint();
        }
    }
}
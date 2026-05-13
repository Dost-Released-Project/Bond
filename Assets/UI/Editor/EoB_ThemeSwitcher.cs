using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Bond.UI.Editor
{
    // ─────────────────────────────────────────────────────────────────────────
    // 테마 교체 방식:
    //   PanelSettings.themeStyleSheet 는 ThemeStyleSheet(.tss)만 수용하므로
    //   일반 .uss 파일을 직접 대입할 수 없다.
    //   대신 프로젝트 내 모든 .uxml 파일에서
    //     <Style src="...EoB_Theme_*.uss" />
    //   줄을 찾아 선택한 테마 경로로 교체한다.
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

        // .uxml 안의 EoB 테마 Style 태그를 찾는 패턴
        private static readonly Regex UxmlThemePattern = new(
            @"<Style\s+src=""[^""]*EoB_Theme_[^""]+\.uss""\s*/>",
            RegexOptions.Compiled);

        private int    _selectedTheme;
        private float  _fontScale = 1.0f;
        private string _status    = "";
        private bool   _statusOk;

        [MenuItem("Tools/Bond/Theme Switcher")]
        public static void ShowWindow() =>
            GetWindow<EoB_ThemeSwitcher>("EoB Theme Switcher").minSize = new Vector2(360, 360);

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

            // 1. 모든 .uxml 에서 EoB 테마 Style src를 선택한 테마로 교체
            int uxmlCount = ReplaceThemeInUxmlFiles(themePath);

            // 2. 선택된 테마 USS 파일에 폰트 배율 적용
            ApplyFontScaleToFile(themePath);

            // 3. 저장
            EditorPrefs.SetInt(PREF_THEME, _selectedTheme);
            EditorPrefs.SetFloat(PREF_SCALE, _fontScale);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            SetStatus(
                $"✓ 완료 — {ThemeNames[_selectedTheme]} / {_fontScale:F2}x  (UXML {uxmlCount}개 갱신)",
                ok: true);
        }

        // Assets 하위 모든 .uxml 파일에서 EoB 테마 Style 태그를 교체
        private static int ReplaceThemeInUxmlFiles(string newThemePath)
        {
            // project:// 형식으로 변환 (Unity UXML src 규칙)
            string newSrcAttr = $@"<Style src=""project://database/{newThemePath}"" />";

            var guids = AssetDatabase.FindAssets("t:VisualTreeAsset", new[] { "Assets" });
            Debug.Log(guids.Length);
            int count = 0;

            foreach (var guid in guids)
            {
                string filePath = AssetDatabase.GUIDToAssetPath(guid);
                if (!filePath.EndsWith(".uxml", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                string content = File.ReadAllText(filePath);
                if (!UxmlThemePattern.IsMatch(content)) continue;

                string updated = UxmlThemePattern.Replace(content, newSrcAttr);
                if (updated == content) continue;

                File.WriteAllText(filePath, updated);
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
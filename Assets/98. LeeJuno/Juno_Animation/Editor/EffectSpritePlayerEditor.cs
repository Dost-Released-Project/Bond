using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(EffectSpritePlayer))]
public class EffectSpritePlayerEditor : Editor
{
    private bool _isPreviewing;
    private int _currentFrame;
    private double _lastFrameTime;

    private SpriteRenderer _spriteRenderer;
    private Sprite[] _sprites;
    private float _fps;
    private Button _previewButton;

    private void OnDisable()
    {
        StopPreview();
    }

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = new VisualElement();

        InspectorElement.FillDefaultInspector(root, serializedObject, this);

        VisualElement spacer = new VisualElement();
        spacer.style.height = 8;
        root.Add(spacer);

        _previewButton = new Button(OnPreviewButtonClicked);
        _previewButton.text = "Preview";
        root.Add(_previewButton);

        return root;
    }

    private void OnPreviewButtonClicked()
    {
        if (_isPreviewing)
        {
            StopPreview();
        }
        else
        {
            StartPreview();
        }
    }

    private void StartPreview()
    {
        serializedObject.Update();

        SerializedProperty rendererProp = serializedObject.FindProperty("_spriteRenderer");
        SerializedProperty spritesProp = serializedObject.FindProperty("_sprites");
        SerializedProperty fpsProp = serializedObject.FindProperty("_fps");

        _spriteRenderer = rendererProp.objectReferenceValue as SpriteRenderer;

        if (_spriteRenderer == null)
        {
            Debug.LogWarning("EffectSpritePlayer: SpriteRenderer가 연결되지 않았습니다.");
            return;
        }

        int count = spritesProp.arraySize;

        if (count == 0)
        {
            Debug.LogWarning("EffectSpritePlayer: Sprites 배열이 비어 있습니다.");
            return;
        }

        _sprites = new Sprite[count];

        for (int i = 0; i < count; i++)
        {
            _sprites[i] = spritesProp.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
        }

        _fps = fpsProp.floatValue;
        _currentFrame = 0;
        _lastFrameTime = EditorApplication.timeSinceStartup;
        _isPreviewing = true;
        _previewButton.text = "Stop Preview";

        EditorApplication.update += OnEditorUpdate;
    }

    private void StopPreview()
    {
        if (_isPreviewing == false) return;

        _isPreviewing = false;
        EditorApplication.update -= OnEditorUpdate;

        if (_previewButton != null)
        {
            _previewButton.text = "Preview";
        }

        if (_spriteRenderer != null && _sprites != null && _sprites.Length > 0)
        {
            _spriteRenderer.sprite = _sprites[0];
            SceneView.RepaintAll();
        }
    }

    private void OnEditorUpdate()
    {
        if (_spriteRenderer == null || _sprites == null)
        {
            StopPreview();
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        double interval = 1.0 / _fps;

        if (now - _lastFrameTime < interval) return;

        _lastFrameTime = now;
        _spriteRenderer.sprite = _sprites[_currentFrame];
        SceneView.RepaintAll();

        _currentFrame++;

        if (_currentFrame >= _sprites.Length)
        {
            _currentFrame = 0;
        }
    }
}

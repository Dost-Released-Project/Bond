using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Bond.Tutorial
{
    public class TutorialTownView : MonoBehaviour
    {
        private TutorialSystemController _controller;

        [SerializeField] private UIDocument townUiDocument; // 튜토리얼 배리어가 있는 UI Document
        [Header("카메라 (좌표 환산용)")]
        [SerializeField] private Camera mainCamera;
        
        private VisualElement _root;
        private VisualElement _barrier;
        private VisualElement _focusBox;
        private Label _guideLabel;
        private Button _skipButton;

        [Inject]
        public void Construct(TutorialSystemController controller)
        {
            _controller = controller;
        }

        private void Start()
        {
            if (townUiDocument == null) return;
            if (mainCamera == null) mainCamera = Camera.main;
            
            _root = townUiDocument.rootVisualElement;
            _barrier = _root.Q<VisualElement>("TutorialBarrier");
            _focusBox = _barrier?.Q<VisualElement>("FocusBox");
            _guideLabel = _root.Q<Label>("GuideTextLabel");
            _skipButton = _root.Q<Button>("SkipButton");

            if (_skipButton != null) _skipButton.clicked += OnSkipButtonTriggered;

            // 포커스 박스 자체는 마우스 입력을 통과시켜 아래의 진짜 버튼이 눌리도록 세팅
            if (_focusBox != null)
            {
                _focusBox.pickingMode = PickingMode.Ignore;
            }

            _controller.OnStepChanged += OnStepUpdated;
            _controller.OnTutorialFinished += OnTutorialCleared;

            _controller.LoadProgress();
        }

        private void OnStepUpdated(TutorialStepSO stepData)
        {
            if (stepData.Sequence != TutorialSequence.Sequence_A_Town)
            {
                if (_barrier != null) _barrier.style.display = DisplayStyle.None;
                return;
            }

            if (_barrier == null || _focusBox == null) return;

            _barrier.style.display = DisplayStyle.Flex;
            _barrier.pickingMode = PickingMode.Position;

            if (_guideLabel != null) _guideLabel.text = stepData.Description;

            // 1. 전역 UIDocument에서 타겟 UI 검색
            VisualElement targetUi = null;
            var uiDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            foreach (var doc in uiDocuments)
            {
                if (doc != null && doc.rootVisualElement != null)
                {
                    targetUi = doc.rootVisualElement.Q<VisualElement>(stepData.TargetUiKey);
                    if (targetUi != null) break;
                }
            }
            
            if (targetUi != null)
            {
                // 타이밍 버그 해결: 레이아웃 계산이 완료되는 순간을 구독
                targetUi.RegisterCallback<GeometryChangedEvent>(evt => 
                {
                    Rect globalBounds = targetUi.worldBound;
                    if (!float.IsNaN(globalBounds.width) && !float.IsNaN(globalBounds.height))
                    {
                        ApplyFocusRect(globalBounds);
                    }
                });

                // 단계별 해제 처리
                targetUi.pickingMode = PickingMode.Ignore;
                
                Rect directBounds = targetUi.worldBound;
                if (!float.IsNaN(directBounds.width) && !float.IsNaN(directBounds.height))
                {
                    ApplyFocusRect(directBounds);
                }
            }
            else
            {
                // GameObject(건설 슬롯) 대응
                GameObject targetObj = GameObject.Find(stepData.TargetUiKey);
                if (targetObj != null)
                {
                    Rect worldToScreenBounds = CalculateWorldBoundsToScreen(targetObj);
                    ApplyFocusRect(worldToScreenBounds);
                }
            }
        }

        // ─── 공통 포커스 설정 메소드 ───
        /// <summary>
        /// 계산된 스크린 Rect 좌표를 기반으로 하이라이트 포커스 박스의 위치와 크기를 동기화합니다.
        /// </summary>
        private void ApplyFocusRect(Rect bounds)
        {
            if (_focusBox == null) return;

            _focusBox.style.position = Position.Absolute;
            _focusBox.style.left = bounds.x;
            _focusBox.style.top = bounds.y;
            _focusBox.style.width = bounds.width;
            _focusBox.style.height = bounds.height;
        }

        private Rect CalculateWorldBoundsToScreen(GameObject targetGo)
        {
            Bounds objectBounds = new Bounds(targetGo.transform.position, Vector3.one * 2f);
            if (targetGo.TryGetComponent<Collider>(out var collider)) objectBounds = collider.bounds;
            else if (targetGo.TryGetComponent<SpriteRenderer>(out var renderer)) objectBounds = renderer.bounds;

            Vector3 screenMin = mainCamera.WorldToScreenPoint(objectBounds.min);
            Vector3 screenMax = mainCamera.WorldToScreenPoint(objectBounds.max);

            float x = screenMin.x;
            float y = Screen.height - screenMax.y; 
            float width = screenMax.x - screenMin.x;
            float height = screenMax.y - screenMin.y;

            return new Rect(x, y, width, height);
        }
        private void OnSkipButtonTriggered()
        {
            var buildingDB = DBSORegistry.GetDb<BuildingDataBaseSO>();
            BuildingData supplySO = buildingDB?.FindSO<BuildingData>(d => d.buildingType == BuildingType.Supply);
            BuildingData storageSO = buildingDB?.FindSO<BuildingData>(d => d.buildingType == BuildingType.Storage);
            _controller.Skip(supplySO, storageSO);
        }

        private void OnTutorialCleared()
        {
            if (_barrier != null) _barrier.style.display = DisplayStyle.None;
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnStepChanged -= OnStepUpdated;
                _controller.OnTutorialFinished -= OnTutorialCleared;
            }
        }
    }
}
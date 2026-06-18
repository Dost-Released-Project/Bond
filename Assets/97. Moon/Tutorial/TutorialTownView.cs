using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Bond.Tutorial
{
    public class TutorialTownView : MonoBehaviour
    {
        private TutorialSystemController _controller;

        [SerializeField] private UIDocument townUiDocument;
        
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
            
            _root = townUiDocument.rootVisualElement;
            _barrier = _root.Q<VisualElement>("TutorialBarrier");
            _focusBox = _barrier?.Q<VisualElement>("FocusBox");
            _guideLabel = _root.Q<Label>("GuideTextLabel");
            _skipButton = _root.Q<Button>("SkipButton");

            if (_skipButton != null) _skipButton.clicked += OnSkipButtonTriggered;

            // 이벤트 바인딩
            _controller.OnStepChanged += OnStepUpdated;
            _controller.OnTutorialFinished += OnTutorialCleared;

            // 현재 씬이 마을(Sequence_A)일 때만 작동하도록 동기화 트리거 개시
            _controller.LoadProgress();
        }

        private void OnStepUpdated(TutorialStepSO stepData)
        {
            // 만약 현재 단계가 탐사 씬 소관이라면 마을 뷰는 화면을 열지 않고 대기합니다.
            if (stepData.Sequence != TutorialSequence.Sequence_A_Town)
            {
                if (_barrier != null) _barrier.style.display = DisplayStyle.None;
                return;
            }

            if (_barrier == null) return;

            // 1. 전체 화면 락 (패널 배리어 가동)
            _barrier.style.display = DisplayStyle.Flex;
            _barrier.pickingMode = PickingMode.Position;

            if (_guideLabel != null) _guideLabel.text = stepData.Description;

            // 2. 동적 좌표 연산: 지정된 타겟 UI 요소를 찾아서 하이라이트 박스를 씌움
            VisualElement targetElement = _root.Q<VisualElement>(stepData.TargetUiKey);
            if (targetElement != null && _focusBox != null)
            {
                // 타겟의 절대 좌표 Bounds를 구함
                Rect bounds = targetElement.worldBound;

                _focusBox.style.position = Position.Absolute;
                _focusBox.style.left = bounds.x;
                _focusBox.style.top = bounds.y;
                _focusBox.style.width = bounds.width;
                _focusBox.style.height = bounds.height;

                // 타겟 엘리먼트 자체의 마우스 클릭 입력 차단 완전 해제
                targetElement.pickingMode = PickingMode.Ignore; 
            }
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
            Debug.Log("<color=lime>[Tutorial Visual]</color> 마을 튜토리얼 뷰 정상 종료.");
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
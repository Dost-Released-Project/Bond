using System;
using System.Collections.Generic;
using UnityEngine;
using Bond.Persistence;

namespace Bond.Tutorial
{
    public class TutorialSystemController
    {
        private readonly ResourceManager _resourceManager;
        private readonly TotalInventory _totalInventory;
        private readonly SettlementManager _settlementManager;

        // 세이브 데이터 키 고정
        private const string SAVE_KEY = "tutorial_progress";
        
        private TutorialRawSaveData _saveData = new();
        private readonly Dictionary<string, TutorialStepSO> _stepMap = new();
        private readonly List<string> _stepOrder = new();
        private int _currentStepIndex = -1;

        public event Action<TutorialStepSO> OnStepChanged;
        public event Action OnTutorialFinished;

        public bool IsCleared => _saveData.isTutorialCleared;
        public string CurrentStepId => _saveData.currentStepId;

        public TutorialSystemController(
            ResourceManager resourceManager, 
            TotalInventory totalInventory, 
            SettlementManager settlementManager)
        {
            _resourceManager = resourceManager;
            _totalInventory = totalInventory;
            _settlementManager = settlementManager;
        }

        // 튜토리얼 스텝 리스트 빌드
        public void SetupSteps(List<TutorialStepSO> steps)
        {
            _stepMap.Clear();
            _stepOrder.Clear();
            
            foreach (var step in steps)
            {
                _stepMap[step.Id] = step;
                _stepOrder.Add(step.Id);
            }
        }

        // 세이브 상태 로드
        public void LoadProgress()
        {
            if (SaveLoadSystem.HasSave(SAVE_KEY))
            {
                // 외부 세이브 파일로부터 원시 데이터 로드 및 수동 동기화
                // 프로젝트의 기본 SaveLoadSystem 작동 원리에 대응
                var dummySaveable = new GenericSaveableWrapper<TutorialRawSaveData>(SAVE_KEY, _saveData, (loaded) => _saveData = loaded);
                SaveLoadSystem.Load(dummySaveable);

                if (_saveData.isTutorialCleared)
                {
                    _currentStepIndex = _stepOrder.Count;
                    OnTutorialFinished?.Invoke();
                }
                else
                {
                    _currentStepIndex = _stepOrder.IndexOf(_saveData.currentStepId);
                    if (_currentStepIndex != -1)
                    {
                        // 현재 단계 트리거 호출
                        OnStepChanged?.Invoke(_stepMap[_saveData.currentStepId]);
                    }
                }
            }
            else
            {
                // 완전 신규 세션 시작
                _saveData.isTutorialCleared = false;
                _saveData.currentStepId = _stepOrder.Count > 0 ? _stepOrder[0] : "NONE";
                _currentStepIndex = _stepOrder.Count > 0 ? 0 : -1;
                
                SaveCurrentState();
                
                if (_currentStepIndex != -1)
                    OnStepChanged?.Invoke(_stepMap[_saveData.currentStepId]);
            }
        }

        // 단계 전진
        public void Advance()
        {
            if (_saveData.isTutorialCleared) return;

            _currentStepIndex++;
            if (_currentStepIndex >= _stepOrder.Count)
            {
                FinishTutorial();
            }
            else
            {
                _saveData.currentStepId = _stepOrder[_currentStepIndex];
                SaveCurrentState();
                OnStepChanged?.Invoke(_stepMap[_saveData.currentStepId]);
            }
        }

        // ⏭️ 무결성 정산 스킵 매커니즘 (기획 완벽 준수)
        public void Skip(BuildingData supplyData, BuildingData storageData)
        {
            if (_saveData.isTutorialCleared) return;

            Debug.Log("<color=cyan>[Tutorial Core]</color> 스킵 요청 감지 -> 무결성 정산 시작.");

            // 1. 마을 건물 강제 인쇄 완공 정산 (명세 기준 슬롯 고정 배치)
            _settlementManager.LoadBuilding(1, supplyData, 1);
            _settlementManager.LoadBuilding(4, storageData, 1);

            // 2. 고정된 목걸이 ID 3종 인벤토리 자동 수납
            _totalInventory.AddItemId("08000000", 1);
            _totalInventory.AddItemId("08010000", 1);
            _totalInventory.AddItemId("08020000", 1);

            // 3. 인벤토리 수납 영구 각인
            _totalInventory.SaveTotalInventory();

            // 4. 완료 처리
            FinishTutorial();
        }

        private void FinishTutorial()
        {
            _saveData.isTutorialCleared = true;
            _saveData.currentStepId = "FINISHED";
            SaveCurrentState();
            OnTutorialFinished?.Invoke();
        }

        private void SaveCurrentState()
        {
            var dummySaveable = new GenericSaveableWrapper<TutorialRawSaveData>(SAVE_KEY, _saveData, null);
            SaveLoadSystem.Save(dummySaveable);
        }
    }

    // 세이브로드시스템 규격을 우회하여 원자적 데이터를 보존하기 위한 불변 프록시 래퍼
    public class GenericSaveableWrapper<T> : ISaveable
    {
        public string Key { get; }
        public object Data { get; }
        private readonly Action<T> _onRestore;

        public GenericSaveableWrapper(string key, T data, Action<T> onRestore)
        {
            Key = key;
            Data = data;
            _onRestore = onRestore;
        }

        public void Restore(object data)
        {
            if (data is T castedData) _onRestore?.Invoke(castedData);
        }
    }
}
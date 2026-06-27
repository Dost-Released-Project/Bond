using System;
using System.Collections.Generic;
using Bond.Expedition;
using UnityEngine;
using Bond.Persistence;
using PipeLine;
using Unity.VisualScripting;
using VContainer;

namespace Bond.Tutorial
{
    public class ExpeditionTutorialSystemController
    {
        [Inject] private ExpeditionPayload _payload;

        // 세이브 데이터 키 고정
        private const string SAVE_KEY = "expedition_tutorial_progress";
        
        private TutorialRawSaveData _saveData = new();
        private readonly Dictionary<string, TutorialStepSO> _stepMap = new();
        private readonly List<string> _stepOrder = new();
        private int _currentStepIndex = -1;

        public event Action<TutorialStepSO> OnStepChanged;
        public event Action OnTutorialFinished;

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

            // 💥 [보상 정산 타이밍] 현재 완료 판정을 받은 스텝의 SO를 꺼내 정산 개시
            if (_stepMap.TryGetValue(_saveData.currentStepId, out var currentStepSO))
            {
                // 1. 자원 보상 처리
                if (currentStepSO.RewardFrontier > 0 || currentStepSO.RewardWood > 0 || currentStepSO.RewardOre > 0)
                {
                    _payload.AddReward(currentStepSO.RewardFrontier, currentStepSO.RewardWood, currentStepSO.RewardOre);
                }

                // 2. 💥 [콤마 분리형 다중 아이템 자동 안착 엔진]
                if (!string.IsNullOrEmpty(currentStepSO.RewardItemIds) && !string.IsNullOrEmpty(currentStepSO.RewardItemCounts))
                {
                    // 콤마 단위 슬라이싱 파싱
                    string[] ids = currentStepSO.RewardItemIds.Split(',');
                    string[] counts = currentStepSO.RewardItemCounts.Split(',');

                    int minLength = Mathf.Min(ids.Length, counts.Length);
                    bool hasAddedAny = false;

                    for (int i = 0; i < minLength; i++)
                    {
                        string itemId = ids[i].Trim();
                        if (int.TryParse(counts[i].Trim(), out int count) && !string.IsNullOrEmpty(itemId))
                        {
                            _payload.Supplies.AddItemId(itemId, count);
                            hasAddedAny = true;
                            Debug.Log($"<color=orange>[Tutorial Auto-Reward]</color> 보상 수납: Item {itemId} x{count}개 완공.");
                        }
                    }

                    if (hasAddedAny)
                    {
                        _payload.Supplies.SaveExpeditionInventory();
                    }
                }
            }

            // 보상 정산이 완벽히 끝난 후 안전하게 인덱스를 다음 단계로 전환
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
        public void Skip()
        {
            if (_saveData.isTutorialCleared) return;

            Debug.Log("<color=cyan>[Tutorial Core]</color> 스킵 요청 감지 -> 무결성 정산 시작.");

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
}
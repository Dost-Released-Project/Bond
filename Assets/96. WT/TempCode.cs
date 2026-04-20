using _03._PipeLine;
using Cysharp.Threading.Tasks;
using juno_Test;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using System.Threading;
using System;
using _02._Scripts.BattleSystem;

namespace _96._WT
{
    /// <summary>
    /// [D] Runtime Data - 전투 흐름의 상태 보존 및 변경 통지
    /// </summary>
    public sealed class RuntimeDataBattleFlow
    {
        public int[] PlayerUnitIds { get; internal set; } = Array.Empty<int>();

        private bool _isBattleActive;
        public bool IsBattleActive
        {
            get => _isBattleActive;
            internal set
            {
                if (_isBattleActive == value) return;
                _isBattleActive = value;
                OnBattleActiveChanged?.Invoke(value);
            }
        }

        public event Action<bool> OnBattleActiveChanged;
    }

    /// <summary>
    /// [V] Visual Interface - 연출 명령 규약
    /// </summary>
    public interface IBattleFlowVisualizer
    {
        void PlayStartBattleEffect();
        void PlaySkillAppliedEffect();
    }

    /// <summary>
    /// [L] Logic System - 전투 흐름 제어 및 데이터 갱신
    /// </summary>
    public sealed class BattleFlowSystem : MonoBehaviour
    {
        private IBattleEntryPoint _battleEntryPoint;
        private IBattleManager _battleManager;
        private IBattleFlowVisualizer _visualizer;
        private RuntimeDataBattleFlow _runtimeData;
        private BaseCharacter[] _playerUnitInstances;
        private CancellationTokenSource _cts;

        [Inject]
        public void Construct(
            IBattleEntryPoint battleEntryPoint, 
            IBattleManager battleManager,
            IBattleFlowVisualizer visualizer,
            RuntimeDataBattleFlow runtimeData,
            BaseCharacter[] initialUnits)
        {
            _battleEntryPoint = battleEntryPoint;
            _battleManager = battleManager;
            _visualizer = visualizer;
            _runtimeData = runtimeData;
            _playerUnitInstances = initialUnits ?? Array.Empty<BaseCharacter>();
            
            // [D] Update: 순수 데이터(ID)만 추출하여 상태 레이어에 저장
            int[] ids = new int[_playerUnitInstances.Length];
            for (int i = 0; i < _playerUnitInstances.Length; i++)
            {
                ids[i] = _playerUnitInstances[i].GetInstanceID();
            }
            _runtimeData.PlayerUnitIds = ids;
            
            _cts = new CancellationTokenSource();
        }

        private void Update()
        {
            HandleBattleInput();
        }

        private void HandleBattleInput()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                ExecuteBattleProcess().Forget();
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                ExecuteSkillProcess();
            }
        }

        private async UniTaskVoid ExecuteBattleProcess()
        {
            if (_runtimeData.PlayerUnitIds.Length == 0)
            {
                Debug.LogWarning("[Logic] No unit IDs assigned to start battle.");
                return;
            }

            if (_runtimeData.IsBattleActive) return;

            // [V] Command: 시각적 연출 명령
            _visualizer.PlayStartBattleEffect();
            
            // [D] Update: 관측 가능한 상태 변경
            _runtimeData.IsBattleActive = true;
            
            await _battleEntryPoint.StartAsync(_cts.Token, _playerUnitInstances);
        }

        private void ExecuteSkillProcess()
        {
            Debug.Log("[Logic] Requesting skill logic application");
            _battleManager.SkillApplyLogic(new BattleContext());
            _visualizer.PlaySkillAppliedEffect();
        }

        private void OnDestroy()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }
        }
    }

    /// <summary>
    /// [V] World Visualizer - 실제 연출 구현체
    /// </summary>
    public sealed class BattleFlowVisualizer : MonoBehaviour, IBattleFlowVisualizer
    {
        public void PlayStartBattleEffect()
        {
            Debug.Log("[Visual] Battle Started Visual Sequence Played.");
        }

        public void PlaySkillAppliedEffect()
        {
            Debug.Log("[Visual] Skill Applied Visual Sequence Played.");
        }
    }
}

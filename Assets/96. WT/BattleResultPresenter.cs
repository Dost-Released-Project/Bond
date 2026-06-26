using System;
using System.Collections.Generic;
using BattleSystem.Interface;
using Bond.Expedition;
using Bond.WT.Journal;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer.Unity;

namespace BattleSystem
{
    public class BattleResultPresenter : IStartable, IDisposable
    {
        private readonly IBattleFlowManager _battleFlowManager;
        private readonly ExpeditionPayload _payload;
        private readonly BattleResultView _view;
        private readonly ISpriteLoader _spriteLoader;

        private readonly List<AsyncOperationHandle<Sprite>> _loadedHandles = new();

        public BattleResultPresenter(
            IBattleFlowManager battleFlowManager,
            ExpeditionPayload payload,
            BattleResultView view,
            ISpriteLoader spriteLoader)
        {
            _battleFlowManager = battleFlowManager;
            _payload = payload;
            _view = view;
            _spriteLoader = spriteLoader;
        }

        public void Start()
        {
            if (_battleFlowManager != null)
            {
                _battleFlowManager.OnBattleEnd += HandleBattleEnd;
            }
        }

        public void Dispose()
        {
            if (_battleFlowManager != null)
            {
                _battleFlowManager.OnBattleEnd -= HandleBattleEnd;
            }
            ReleaseHandles();
        }

        private void HandleBattleEnd(BattleEndStatus status)
        {
            ShowResultAsync(status).Forget();
        }

        private async UniTaskVoid ShowResultAsync(BattleEndStatus status)
        {
            ReleaseHandles();

            var portraits = new Dictionary<string, Sprite>();
            var party = _payload.Party;

            if (party != null)
            {
                var tasks = new List<UniTask<(string, AsyncOperationHandle<Sprite>)>>();

                foreach (var character in party)
                {
                    if (character == null) continue;

                    string address = character.EffectiveIdleImageAddress;
                    if (!string.IsNullOrEmpty(address))
                    {
                        tasks.Add(LoadPortraitAsync(character.Id, address));
                    }
                }

                if (tasks.Count > 0)
                {
                    var results = await UniTask.WhenAll(tasks);
                    foreach (var res in results)
                    {
                        var handle = res.Item2;
                        if (handle.Status == AsyncOperationStatus.Succeeded)
                        {
                            _loadedHandles.Add(handle);
                            portraits[res.Item1] = handle.Result;
                        }
                        else
                        {
                            UnityEngine.AddressableAssets.Addressables.Release(handle);
                        }
                    }
                }
            }

            // 승리 시에만 보상을 띄우고, 패배/퇴각 시에는 0으로 고정
            int frontier = status == BattleEndStatus.Victory ? _payload.LastAddedFrontier : 0;
            int wood = status == BattleEndStatus.Victory ? _payload.LastAddedWood : 0;
            int ore = status == BattleEndStatus.Victory ? _payload.LastAddedOre : 0;

            _view.ShowResult(status, party, frontier, wood, ore, portraits, () =>
            {
                // 맵으로 복귀 버튼 클릭 콜백
                StageResult result = new StageResult
                {
                    IsSuccess = status == BattleEndStatus.Victory,
                    IsGameOver = status == BattleEndStatus.Defeat,
                    IsBattleTriggered = false,
                    RewardIds = new List<string>()
                };

                // 복귀 신호 발송
                StageCompletionChannel.Invoke(result);

                // UI 숨김 및 리소스 해제
                _view.Hide();
                ReleaseHandles();
            });
        }

        private async UniTask<(string, AsyncOperationHandle<Sprite>)> LoadPortraitAsync(string id, string address)
        {
            var handle = await _spriteLoader.LoadAsync(address);
            return (id, handle);
        }

        private void ReleaseHandles()
        {
            foreach (var handle in _loadedHandles)
            {
                if (handle.IsValid())
                {
                    UnityEngine.AddressableAssets.Addressables.Release(handle);
                }
            }
            _loadedHandles.Clear();
        }
    }
}

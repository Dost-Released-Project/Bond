using System;
using System.Collections.Generic;
using System.Linq;
using Bond.Embark;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;
using VContainer.Unity;

namespace Bond.WT.Journal
{
    /// <summary>
    /// [Feature]Binder 모델의 상태 변화를 UI 뷰에 연결
    /// </summary>
    public class JournalBinder : IStartable, IDisposable
    {
        private readonly JournalModel _model;
        private readonly IJournalVisualizer _view;
        private readonly JournalSystem _system;
        private readonly ISpriteLoader _spriteLoader;
        private readonly Bond.Expedition.ExpeditionPayload _payload;

        private AsyncOperationHandle<Sprite>? _currentIconHandle;
        private readonly List<AsyncOperationHandle<Sprite>> _partyHandles = new();

        // 자체 IObserver 구현용 임시 래퍼 (ObservableValue가 IObserver<T>만 받으므로)
        private class ObserverWrapper<T> : IObserver<T>
        {
            public Action<T> EventHandler { get; set; }
        }

        private readonly ObserverWrapper<string> _paragraphObserver = new ObserverWrapper<string>();
        private readonly ObserverWrapper<IReadOnlyList<JournalOption>> _optionsObserver = new ObserverWrapper<IReadOnlyList<JournalOption>>();
        private readonly ObserverWrapper<JournalReport> _reportObserver = new ObserverWrapper<JournalReport>();
        private readonly ObserverWrapper<bool> _completeObserver = new ObserverWrapper<bool>();
        private readonly ObserverWrapper<bool> _prevPageObserver = new ObserverWrapper<bool>();
        private readonly ObserverWrapper<bool> _lastPageObserver = new ObserverWrapper<bool>();
        private readonly ObserverWrapper<bool> _nextPageEnabledObserver = new ObserverWrapper<bool>();

        [Inject]
        public JournalBinder(JournalModel model, IJournalVisualizer view, JournalSystem system, ISpriteLoader spriteLoader, Bond.Expedition.ExpeditionPayload payload)
        {
            _model = model;
            _view = view;
            _system = system;
            _spriteLoader = spriteLoader;
            _payload = payload;

            // 핸들러 설정
            _paragraphObserver.EventHandler = text => 
            {
                if (!string.IsNullOrEmpty(text))
                {
                    _view.SetVisible(true);
                    _view.ShowText(text);
                }
            };

            _optionsObserver.EventHandler = options => 
            {
                _view.SetOptions(options);
            };

            _reportObserver.EventHandler = report => 
            {
                if (report != null)
                {
                    bool isBattleEnd = report.Metadata.TryGetValue("IsBattleEnd", out string isBattleEndStr) && isBattleEndStr == "true";

                    if (isBattleEnd)
                    {
                        // 결과창 바인딩 페이지에서는 대표 사건 아이콘을 숨김
                        ReleaseIconHandle();
                        _view.SetIcon(null);
                        LoadAndSetBattleResultAsync(report).Forget();
                    }
                    else
                    {
                        var manualChar = _payload.Party?
                            .FirstOrDefault(c => c.isPlayable);
                        
                        string address = (manualChar != null && !string.IsNullOrEmpty(manualChar.ImageAddress)) 
                            ? manualChar.ImageAddress 
                            : report.IconId;
                        
                        LoadAndSetIconAsync(address).Forget();
                        _view.ClearBattleResult();
                    }
                }
            };

            _completeObserver.EventHandler = isComplete => 
            {
                if (isComplete)
                {
                    _view.ClearUI();
                    _view.ClearBattleResult();
                    _view.SetVisible(false);
                    ReleaseIconHandle();
                    ReleasePartyHandles();
                }
            };

            _prevPageObserver.EventHandler = hasPrev => _view.SetPrevButtonEnabled(hasPrev);
            _lastPageObserver.EventHandler = isLast => _view.SetNextButtonText(isLast ? "닫기" : "다음 장");
            _nextPageEnabledObserver.EventHandler = isEnabled => _view.SetNextButtonEnabled(isEnabled);
        }

        private async UniTaskVoid LoadAndSetIconAsync(string iconId)
        {
            ReleaseIconHandle();

            if (string.IsNullOrEmpty(iconId))
            {
                _view.SetIcon(null);
                return;
            }

            var handle = await _spriteLoader.LoadAsync(iconId);
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _currentIconHandle = handle;
                _view.SetIcon(handle.Result);
            }
            else
            {
                UnityEngine.AddressableAssets.Addressables.Release(handle);
                _view.SetIcon(null);
            }
        }

        private void ReleaseIconHandle()
        {
            if (_currentIconHandle.HasValue)
            {
                UnityEngine.AddressableAssets.Addressables.Release(_currentIconHandle.Value);
                _currentIconHandle = null;
            }
        }

        public void Start()
        {
            // 모델 구독
            _model.CurrentParagraph.Subscribe(_paragraphObserver);
            _model.CurrentOptions.Subscribe(_optionsObserver);
            _model.IsJournalComplete.Subscribe(_completeObserver);
            _model.CurrentReport.Subscribe(_reportObserver);
            _model.HasPrevPage.Subscribe(_prevPageObserver);
            _model.IsLastPage.Subscribe(_lastPageObserver);
            _model.IsNextButtonEnabled.Subscribe(_nextPageEnabledObserver);

            // [초기화] 구독 시점에 이미 데이터가 있을 경우를 위해 강제 동기화 (Value가 null/초기값이 아닐 때만)
            if (!string.IsNullOrEmpty(_model.CurrentParagraph.Value)) _paragraphObserver.EventHandler?.Invoke(_model.CurrentParagraph.Value);
            if (_model.CurrentOptions.Value != null) _optionsObserver.EventHandler?.Invoke(_model.CurrentOptions.Value);
            if (_model.CurrentReport.Value != null) _reportObserver.EventHandler?.Invoke(_model.CurrentReport.Value);
            _completeObserver.EventHandler?.Invoke(_model.IsJournalComplete.Value);
            _prevPageObserver.EventHandler?.Invoke(_model.HasPrevPage.Value);
            _lastPageObserver.EventHandler?.Invoke(_model.IsLastPage.Value);
            _nextPageEnabledObserver.EventHandler?.Invoke(_model.IsNextButtonEnabled.Value);

            // 뷰 이벤트 연결 (기존 콜백 보존 체이닝 처리로 덮어쓰기 방지)
            _view.OnNextClicked = () => _system.NextPage();
            _view.OnPrevClicked = () => _system.PrevPage();
            
            var originalCallback = _view.OnOptionSelected;
            _view.OnOptionSelected = option =>
            {
                _system.SelectOption(option);
                originalCallback?.Invoke(option);
            };
        }

        public void Dispose()
        {
            _model.CurrentParagraph.Unsubscribe(_paragraphObserver);
            _model.CurrentOptions.Unsubscribe(_optionsObserver);
            _model.IsJournalComplete.Unsubscribe(_completeObserver);
            _model.CurrentReport.Unsubscribe(_reportObserver);
            _model.HasPrevPage.Unsubscribe(_prevPageObserver);
            _model.IsLastPage.Unsubscribe(_lastPageObserver);
            _model.IsNextButtonEnabled.Unsubscribe(_nextPageEnabledObserver);
            ReleaseIconHandle();
            ReleasePartyHandles();
        }

        private async UniTaskVoid LoadAndSetBattleResultAsync(JournalReport report)
        {
            ReleasePartyHandles();

            // 메타데이터 정보 파싱
            BattleSystem.Interface.BattleEndStatus status = BattleSystem.Interface.BattleEndStatus.Victory;
            if (report.Metadata.TryGetValue("BattleEndStatus", out string statusStr))
            {
                Enum.TryParse(statusStr, out status);
            }

            int frontier = 0;
            int wood = 0;
            int ore = 0;
            if (report.Metadata.TryGetValue("RewardFrontier", out string fStr)) int.TryParse(fStr, out frontier);
            if (report.Metadata.TryGetValue("RewardWood", out string wStr)) int.TryParse(wStr, out wood);
            if (report.Metadata.TryGetValue("RewardOre", out string oStr)) int.TryParse(oStr, out ore);

            var party = _payload.Party;
            var enemyParty = _payload.EnemyParty;
            var portraits = new Dictionary<string, Sprite>();

            var tasks = new List<UniTask<(string, AsyncOperationHandle<Sprite>)>>();

            if (party != null)
            {
                foreach (var character in party)
                {
                    if (character == null) continue;

                    string address = character.EffectiveIdleImageAddress;
                    if (!string.IsNullOrEmpty(address))
                    {
                        tasks.Add(LoadPortraitAsync(character.Id, address));
                    }
                }
            }

            if (enemyParty != null)
            {
                foreach (var character in enemyParty)
                {
                    if (character == null) continue;

                    string address = character.EffectiveIdleImageAddress;
                    if (!string.IsNullOrEmpty(address))
                    {
                        tasks.Add(LoadPortraitAsync(character.Id, address));
                    }
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
                        _partyHandles.Add(handle);
                        portraits[res.Item1] = handle.Result;
                    }
                    else
                    {
                        UnityEngine.AddressableAssets.Addressables.Release(handle);
                    }
                }
            }

            _view.SetBattleResult(status, party, enemyParty, portraits, frontier, wood, ore);
        }

        private async UniTask<(string, AsyncOperationHandle<Sprite>)> LoadPortraitAsync(string id, string address)
        {
            var handle = await _spriteLoader.LoadAsync(address);
            return (id, handle);
        }

        private void ReleasePartyHandles()
        {
            foreach (var handle in _partyHandles)
            {
                if (handle.IsValid())
                {
                    UnityEngine.AddressableAssets.Addressables.Release(handle);
                }
            }
            _partyHandles.Clear();
        }
    }
}
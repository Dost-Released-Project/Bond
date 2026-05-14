using System;
using System.Collections.Generic;
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
    public class JournalBinder : IStartable, IDisposable, IObserver<string>, IObserver<IReadOnlyList<JournalOption>>, IObserver<bool>, IObserver<JournalReport>
    {
        private readonly JournalModel _model;
        private readonly IJournalVisualizer _view;
        private readonly JournalSystem _system;
        private readonly ISpriteLoader _spriteLoader;

        private AsyncOperationHandle<Sprite>? _currentIconHandle;

        // IObserver 구현을 위한 핸들러들
        Action<string> IObserver<string>.EventHandler { get; set; }
        Action<IReadOnlyList<JournalOption>> IObserver<IReadOnlyList<JournalOption>>.EventHandler { get; set; }
        Action<bool> IObserver<bool>.EventHandler { get; set; }
        Action<JournalReport> IObserver<JournalReport>.EventHandler { get; set; }

        [Inject]
        public JournalBinder(JournalModel model, IJournalVisualizer view, JournalSystem system, ISpriteLoader spriteLoader)
        {
            _model = model;
            _view = view;
            _system = system;
            _spriteLoader = spriteLoader;

            // 핸들러 설정
            ((IObserver<string>)this).EventHandler = text => 
            {
                if (!string.IsNullOrEmpty(text))
                {
                    _view.SetVisible(true);
                    _view.ShowText(text);
                }
            };

            ((IObserver<IReadOnlyList<JournalOption>>)this).EventHandler = options => _view.SetOptions(options);

            ((IObserver<JournalReport>)this).EventHandler = report => 
            {
                if (report != null)
                {
                    LoadAndSetIconAsync(report.IconId).Forget();
                }
            };

            ((IObserver<bool>)this).EventHandler = isComplete => 
            {
                if (isComplete)
                {
                    _view.ClearUI();
                    _view.SetVisible(false);
                    ReleaseIconHandle();
                }
            };
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
            _model.CurrentParagraph.Subscribe(this);
            _model.CurrentOptions.Subscribe(this);
            _model.IsJournalComplete.Subscribe(this);
            _model.CurrentReport.Subscribe(this);

            // 뷰 이벤트 연결
            _view.OnNextClicked = () => 
            {
                _system.SelectOption(null); 
            };
            
            _view.OnOptionSelected = option => _system.SelectOption(option);
        }

        public void Dispose()
        {
            _model.CurrentParagraph.Unsubscribe(this);
            _model.CurrentOptions.Unsubscribe(this);
            _model.IsJournalComplete.Unsubscribe(this);
            _model.CurrentReport.Unsubscribe(this);
            ReleaseIconHandle();
        }
    }
}

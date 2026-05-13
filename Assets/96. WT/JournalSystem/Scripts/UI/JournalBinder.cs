using System;
using System.Collections.Generic;
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

        // IObserver 구현을 위한 핸들러들
        Action<string> IObserver<string>.EventHandler { get; set; }
        Action<IReadOnlyList<JournalOption>> IObserver<IReadOnlyList<JournalOption>>.EventHandler { get; set; }
        Action<bool> IObserver<bool>.EventHandler { get; set; }
        Action<JournalReport> IObserver<JournalReport>.EventHandler { get; set; }

        [Inject]
        public JournalBinder(JournalModel model, IJournalVisualizer view, JournalSystem system)
        {
            _model = model;
            _view = view;
            _system = system;

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
                if (report != null) _view.SetIcon(report.Icon);
            };

            ((IObserver<bool>)this).EventHandler = isComplete => 
            {
                if (isComplete)
                {
                    _view.ClearUI();
                    _view.SetVisible(false);
                }
            };
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
        }
    }
}

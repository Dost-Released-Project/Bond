using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bond.WT.Journal
{
    [Serializable]
    public struct JournalOption
    {
        public string text;
        public string actionKey; // 선택 시 수행할 로직 식별자
    }

    /// <summary>
    /// [PureData] 일지에 표시될 개별 사건 또는 텍스트 템플릿 정의
    /// </summary>
    [CreateAssetMenu(fileName = "JournalData_", menuName = "WT/Journal/Data")]
    public class JournalDataSO : BaseSO
    {
        [Header("일지 템플릿")]
        [Tooltip("텍스트 내에 {0}, {1} 등을 포함하여 런타임에 데이터를 조립할 수 있습니다.")]
        [SerializeField] private List<string> _paragraphs = new List<string>();

        [Header("선택지")]
        [SerializeField] private List<JournalOption> _options = new List<JournalOption>();

        [Header("시각 연출")]
        [Tooltip("Addressables에서 로드할 Sprite의 Key (비워두면 아이콘 숨김)")]
        [SerializeField] private string _entryIconId;

        public IReadOnlyList<string> Paragraphs => _paragraphs;
        public IReadOnlyList<JournalOption> Options => _options;
        public string EntryIconId => _entryIconId;

        public void SetData(string id, List<string> paragraphs, List<JournalOption> options, string iconId)
        {
            base.Initialize(id, id, ""); // Name과 Desc는 ID로 대체 또는 빈값
            this._paragraphs = paragraphs;
            this._options = options;
            this._entryIconId = iconId;
        }
    }
}

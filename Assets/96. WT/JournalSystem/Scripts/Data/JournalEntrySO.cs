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
    /// [PureData] 일지에 표시될 개별 사건 정의
    /// </summary>
    [CreateAssetMenu(fileName = "JournalEntry_", menuName = "WT/Journal/Entry")]
    public class JournalEntrySO : BaseSO
    {
        [Header("일지 내용")]
        [Tooltip("일지에서 순차적으로 출력될 텍스트 리스트")]
        [SerializeField] private List<string> _paragraphs = new List<string>();

        [Header("선택지")]
        [SerializeField] private List<JournalOption> _options = new List<JournalOption>();

        [Header("시각 연출")]
        [SerializeField] private Sprite _entryIcon;

        public IReadOnlyList<string> Paragraphs => _paragraphs;
        public IReadOnlyList<JournalOption> Options => _options;
        public Sprite EntryIcon => _entryIcon;
    }
}

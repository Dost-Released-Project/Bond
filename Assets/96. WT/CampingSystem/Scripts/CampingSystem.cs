using System;
using System.Collections.Generic;
using Bond.Expedition;
using Bond.WT.Journal;
using UnityEngine;

namespace Bond.WT.Camping
{
    public class CampingSystem : IDisposable
    {
        private readonly ExpeditionPayload _payload;
        private readonly IJournalVisualizer _journalVisualizer;
        private bool _isCamping = false;

        public CampingSystem(ExpeditionPayload payload, IJournalVisualizer journalVisualizer)
        {
            _payload = payload;
            _journalVisualizer = journalVisualizer;
            
            if (_payload != null && _payload.Supplies != null)
            {
                _payload.Supplies.OnChanged += OnInventoryChanged;
            }
        }

        public void Dispose()
        {
            if (_payload != null && _payload.Supplies != null)
            {
                _payload.Supplies.OnChanged -= OnInventoryChanged;
            }
        }

        private void OnInventoryChanged()
        {
            // 캠핑 씬 활성화 중일 때 인벤토리가 갱신(로드 완료 등)되면 리포트를 다시 그립니다.
            if (_isCamping)
            {
                GenerateCampingReport();
            }
        }

        public void StartCamping()
        {
            _isCamping = true;
            GenerateCampingReport();
        }

        public void GenerateCampingReport()
        {
            var report = new JournalReport
            {
                Title = "캠핑장 정비",
                IconId = "CampIcon"
            };
            report.Paragraphs.Add("탐사 중 휴식을 취할 시간입니다.\n파티원의 상태를 확인하고 아이템을 사용해 회복시킬 수 있습니다.");
            
            int hpItemCount = GetItemCountByType(ConsumableType.Bandage);
            int insanityItemCount = GetItemCountByType(ConsumableType.Sedative);

            if (_payload != null && _payload.Party != null)
            {
                for (int i = 0; i < _payload.Party.Count; i++)
                {
                    var chara = _payload.Party[i];

                    if (chara.Stat.current_Hp < chara.Stat.max_Hp)
                    {
                        report.Options.Add(new JournalOption
                        {
                            text = $"[{chara.Name}] HP 회복 (남은 붕대류: {hpItemCount})",
                            actionKey = $"CAMP_REST_HP_{i}",
                            isEnabled = hpItemCount > 0
                        });
                    }

                    if (chara.Insanity > 0)
                    {
                        report.Options.Add(new JournalOption
                        {
                            text = $"[{chara.Name}] 정신력 회복 (남은 진정제류: {insanityItemCount})",
                            actionKey = $"CAMP_REST_INSANITY_{i}",
                            isEnabled = insanityItemCount > 0
                        });
                    }
                }
            }

            report.Options.Add(new JournalOption
            {
                text = "정비 끝내기",
                actionKey = "CAMP_END_MAINTENANCE",
                isEnabled = true
            });

            ShowReport(report);
        }

        private void ShowReport(JournalReport report)
        {
            _journalVisualizer.ClearUI();
            _journalVisualizer.SetVisible(true);
            
            string fullText = report.Title + "\n\n" + string.Join("\n", report.Paragraphs);
            _journalVisualizer.ShowText(fullText, false);
            _journalVisualizer.SetOptions(report.Options);
        }

        private int GetItemCountByType(ConsumableType type)
        {
            if (_payload == null || _payload.Supplies == null) return 0;

            int count = 0;
            var slots = _payload.Supplies.GetAll();
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && slot.item is ConsumableItem cItem && cItem.consumableType == type)
                {
                    count += slot.quantity;
                }
            }
            return count;
        }

        public void EndCamping()
        {
            _isCamping = false;
            _journalVisualizer.SetVisible(false);
            UnityEngine.Debug.Log("<color=cyan>[CampingSystem] 캠핑 종료. 다음 씬으로 이동 처리 필요.</color>");
        }
    }
}

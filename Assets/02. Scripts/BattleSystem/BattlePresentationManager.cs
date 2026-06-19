using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using DG.Tweening;
using Object = UnityEngine.Object;
using Shapes;
using BattleSystem.Interface;

namespace BattleSystem
{
    public class BattlePresentationManager
    {
        private ImmediateModeCanvas m_shapesCanvas;

        private class SlotRestoreData
        {
            public Transform parent;
            public int siblingIndex;
            public Vector2 anchoredPosition;
            public Vector3 localPosition;
            public Vector3 localScale;
        }

        private class FocusLevel
        {
            public DimPanelVisualizer dimVisualizer;
            public Dictionary<CharacterSlot, SlotRestoreData> restoreData = new Dictionary<CharacterSlot, SlotRestoreData>();
        }

        private Stack<FocusLevel> m_focusStack = new Stack<FocusLevel>();

        public void Initialize(CharacterSlot slot)
        {
            if (m_shapesCanvas != null) return;
            if (slot == null) return;

            // FindObjectOfType 대신 연출을 실행하는 슬롯의 부모 캔버스를 직접 찾음
            m_shapesCanvas = slot.GetComponentInParent<ImmediateModeCanvas>();
            if (m_shapesCanvas == null)
            {
                Debug.LogError("[BattlePresentationManager] ImmediateModeCanvas not found in parents of CharacterSlot!");
                return;
            }
        }

        public async UniTask StartFocusEffect(CharacterSlot caster, List<CharacterSlot> targets)
        {
            if (m_shapesCanvas == null) Initialize(caster);
            if (m_shapesCanvas == null) return;

            var focusLevel = new FocusLevel();

            // 1. 개별 Dim 켜기 (현재 레벨용)
            var dimGo = new GameObject($"DimPanelVisualizer_Level_{m_focusStack.Count}");
            var rect = dimGo.AddComponent<RectTransform>();
            dimGo.transform.SetParent(m_shapesCanvas.transform, false);
            
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            focusLevel.dimVisualizer = dimGo.AddComponent<DimPanelVisualizer>();
            focusLevel.dimVisualizer.alpha = 0f;
            
            // DOTween으로 alpha 트위닝
            DOTween.To(() => focusLevel.dimVisualizer.alpha, x => focusLevel.dimVisualizer.alpha = x, 0.7f, 0.3f);

            // 레이아웃 이동 전에 모든 타겟의 원본 데이터를 먼저 캐싱 (siblingIndex 꼬임 방지)
            if (caster != null) CacheSlotData(caster, focusLevel);
            if (targets != null && targets.Count > 0)
            {
                foreach (var t in targets)
                {
                    if (t != null && !focusLevel.restoreData.ContainsKey(t)) 
                        CacheSlotData(t, focusLevel);
                }
            }

            var seq = DOTween.Sequence();

            // 2. Caster 처리 (껐다 켜서 Dim보다 뒤의 순서로, 즉 최상단에 렌더링되게 만듦)
            if (caster != null)
            {
                BringToFront(caster);
                bool isSameSide = (targets != null && targets.Count > 0 && targets[0] != null && targets[0].side == caster.side);
                float casterX = (caster.side == E_BattleSide.Player) ? -250f : 250f;
                // 광역(2명 이상)이면서 아군 대상인 경우에만 캐스터를 더 외곽으로 이동
                if (isSameSide && targets.Count > 1)
                {
                    casterX = (caster.side == E_BattleSide.Player) ? -350f : 350f;
                }
                MoveSlotToCenter(caster, new Vector3(casterX, 0, 0), seq, focusLevel);
            }
            
            // 3. Targets 처리
            if (targets != null && targets.Count > 0)
            {
                bool isSameSide = (caster != null && targets[0] != null && targets[0].side == caster.side);
                
                if (targets.Count > 1) // 광역(2명 이상) 연출: 가로 정렬 적용
                {
                    float targetOffsetX = - (targets.Count - 1) * 75f; 
                    for (int i = 0; i < targets.Count; i++)
                    {
                        if (targets[i] != null)
                        {
                            BringToFront(targets[i]);
                            float targetX = (targets[i].side == E_BattleSide.Player) ? -250f : 250f;
                            
                            // 시전자와 대상이 같은 진영일 경우 (ex: 힐, 버프) 타겟을 화면 중앙(0f)에 배치
                            if (isSameSide)
                            {
                                targetX = 0f;
                            }

                            MoveSlotToCenter(targets[i], new Vector3(targetX + targetOffsetX + (i * 150f), 0, 0), seq, focusLevel);
                        }
                    }
                }
                else // 단일(1명) 연출: 기존 오리지널 위치 유지
                {
                    var target = targets[0];
                    if (target != null)
                    {
                        BringToFront(target);
                        float targetX = (target.side == E_BattleSide.Player) ? -250f : 250f;
                        if (caster != null && target.side == caster.side)
                        {
                            targetX = (target.side == E_BattleSide.Player) ? -100f : 100f;
                        }
                        MoveSlotToCenter(target, new Vector3(targetX, 0, 0), seq, focusLevel);
                    }
                }
            }

            m_focusStack.Push(focusLevel);

            var tcs = new UniTaskCompletionSource();
            seq.OnComplete(() => tcs.TrySetResult());
            await tcs.Task;
        }

        private void BringToFront(CharacterSlot slot)
        {
            var visualizer = slot.GetComponent<CharacterSlotVisualizer>();
            if (visualizer != null)
            {
                // 컴포넌트를 껐다 켜서 Shapes ImmediateModeCanvas 렌더링 리스트의 맨 끝으로 보냄
                visualizer.enabled = false;
                visualizer.enabled = true;
            }
        }

        private void CacheSlotData(CharacterSlot slot, FocusLevel level)
        {
            var rectTransform = slot.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            level.restoreData[slot] = new SlotRestoreData
            {
                parent = rectTransform.parent,
                siblingIndex = rectTransform.GetSiblingIndex(),
                anchoredPosition = rectTransform.anchoredPosition,
                localPosition = rectTransform.localPosition,
                localScale = rectTransform.localScale
            };
        }

        private void MoveSlotToCenter(CharacterSlot slot, Vector3 targetLocalPos, Sequence seq, FocusLevel level)
        {
            var rectTransform = slot.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            // 레이아웃 그룹에서 분리하기 위해 캔버스 최상단으로 이동 (화면 위치 유지)
            rectTransform.SetParent(m_shapesCanvas.transform, true);

            // Canvas의 중심(0,0,0)을 기준으로 이동
            seq.Join(rectTransform.DOLocalMove(targetLocalPos, 0.3f).SetEase(Ease.OutQuad));
            seq.Join(rectTransform.DOScale(Vector3.one * 1.5f, 0.3f).SetEase(Ease.OutQuad));
        }

        public async UniTask EndFocusEffect(CharacterSlot caster, List<CharacterSlot> targets)
        {
            if (m_focusStack.Count == 0) return;
            
            var currentLevel = m_focusStack.Pop();

            if (currentLevel.dimVisualizer != null)
            {
                DOTween.To(() => currentLevel.dimVisualizer.alpha, x => currentLevel.dimVisualizer.alpha = x, 0f, 0.3f);
            }

            var seq = DOTween.Sequence();

            if (caster != null) RestoreSlotAnim(caster, seq, currentLevel);
            
            if (targets != null)
            {
                foreach (var target in targets)
                {
                    if (target != null) RestoreSlotAnim(target, seq, currentLevel);
                }
            }

            var tcs = new UniTaskCompletionSource();
            seq.OnComplete(() => 
            {
                // 애니메이션 종료 후 레이아웃 데이터 복원
                if (caster != null) RestoreSlotLayout(caster, currentLevel);
                if (targets != null)
                {
                    foreach (var target in targets)
                    {
                        if (target != null) RestoreSlotLayout(target, currentLevel);
                    }
                }
                
                // 해당 레벨의 DimPanel 정리
                if (currentLevel.dimVisualizer != null)
                {
                    Object.Destroy(currentLevel.dimVisualizer.gameObject);
                }
                
                tcs.TrySetResult();
            });

            await tcs.Task;
        }

        private void RestoreSlotAnim(CharacterSlot slot, Sequence seq, FocusLevel level)
        {
            if (!level.restoreData.TryGetValue(slot, out var data)) return;
            var rectTransform = slot.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            seq.Join(rectTransform.DOScale(data.localScale, 0.3f).SetEase(Ease.OutQuad));
            
            if (data.parent != null)
            {
                Vector3 targetWorldPos = data.parent.TransformPoint(data.localPosition);
                Vector3 targetLocalPos = rectTransform.parent.InverseTransformPoint(targetWorldPos);
                seq.Join(rectTransform.DOLocalMove(targetLocalPos, 0.3f).SetEase(Ease.OutQuad));
            }
        }

        private void RestoreSlotLayout(CharacterSlot slot, FocusLevel level)
        {
            if (!level.restoreData.TryGetValue(slot, out var data)) return;
            var rectTransform = slot.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            // 계층 복구
            rectTransform.SetParent(data.parent, false);
            rectTransform.SetSiblingIndex(data.siblingIndex);

            // 레이아웃 및 트랜스폼 데이터 복구
            rectTransform.anchoredPosition = data.anchoredPosition;
            rectTransform.localPosition = data.localPosition;
            rectTransform.localScale = data.localScale;

            // 리액션 등 연출 중첩 시 부모 복귀 후 LayoutGroup이 즉시 갱신되지 않아 빈칸이 생기는 현상 방지
            if (data.parent != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(data.parent.GetComponent<RectTransform>());
            }
        }
    }
}
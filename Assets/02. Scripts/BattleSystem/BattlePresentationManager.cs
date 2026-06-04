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
        private DimPanelVisualizer m_dimVisualizer;
        private ImmediateModeCanvas m_shapesCanvas;

        private class SlotRestoreData
        {
            public Transform parent;
            public int siblingIndex;
            public Vector2 anchoredPosition;
            public Vector3 localPosition;
            public Vector3 localScale;
        }

        private Dictionary<CharacterSlot, SlotRestoreData> m_originalSlotData = new Dictionary<CharacterSlot, SlotRestoreData>();

        public void Initialize(CharacterSlot slot)
        {
            if (m_dimVisualizer != null) return;
            if (slot == null) return;

            // FindObjectOfType 대신 연출을 실행하는 슬롯의 부모 캔버스를 직접 찾음
            m_shapesCanvas = slot.GetComponentInParent<ImmediateModeCanvas>();
            if (m_shapesCanvas == null)
            {
                Debug.LogError("[BattlePresentationManager] ImmediateModeCanvas not found in parents of CharacterSlot!");
                return;
            }

            var dimGo = new GameObject("DimPanelVisualizer");
            var rect = dimGo.AddComponent<RectTransform>();
            dimGo.transform.SetParent(m_shapesCanvas.transform, false);
            
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            m_dimVisualizer = dimGo.AddComponent<DimPanelVisualizer>();
            m_dimVisualizer.alpha = 0f;
            dimGo.SetActive(false); // Initially off
        }

        public async UniTask StartFocusEffect(CharacterSlot caster, List<CharacterSlot> targets)
        {
            if (m_dimVisualizer == null) Initialize(caster);
            if (m_dimVisualizer == null) return;

            // 1. Dim 켜기 (리스트의 마지막으로 들어감)
            m_dimVisualizer.gameObject.SetActive(true);
            
            // DOTween으로 alpha 트위닝
            DOTween.To(() => m_dimVisualizer.alpha, x => m_dimVisualizer.alpha = x, 0.7f, 0.3f);

            m_originalSlotData.Clear();
            var seq = DOTween.Sequence();

            // 2. Caster 처리 (껐다 켜서 Dim보다 뒤의 순서로, 즉 최상단에 렌더링되게 만듦)
            if (caster != null)
            {
                BringToFront(caster);
                float casterX = (caster.side == E_BattleSide.Player) ? -250f : 250f;
                MoveSlotToCenter(caster, new Vector3(casterX, 0, 0), seq);
            }
            
            // 3. Targets 처리
            if (targets != null && targets.Count > 0)
            {
                float targetOffsetY = - (targets.Count - 1) * 75f; 
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] != null)
                    {
                        BringToFront(targets[i]);
                        float targetX = (targets[i].side == E_BattleSide.Player) ? -250f : 250f;
                        
                        // 시전자와 대상이 같은 진영일 경우 (ex: 힐, 버프) 타겟을 화면 중앙 쪽으로 약간 당겨서 시전자와 겹치는 것을 방지
                        if (caster != null && targets[i].side == caster.side)
                        {
                            targetX = (targets[i].side == E_BattleSide.Player) ? -100f : 100f;
                        }

                        MoveSlotToCenter(targets[i], new Vector3(targetX, targetOffsetY + (i * 150f), 0), seq);
                    }
                }
            }

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

        private void MoveSlotToCenter(CharacterSlot slot, Vector3 targetLocalPos, Sequence seq)
        {
            var rectTransform = slot.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            // 원본 데이터 캐싱
            m_originalSlotData[slot] = new SlotRestoreData
            {
                parent = rectTransform.parent,
                siblingIndex = rectTransform.GetSiblingIndex(),
                anchoredPosition = rectTransform.anchoredPosition,
                localPosition = rectTransform.localPosition,
                localScale = rectTransform.localScale
            };

            // 레이아웃 그룹에서 분리하기 위해 캔버스 최상단으로 이동 (화면 위치 유지)
            rectTransform.SetParent(m_shapesCanvas.transform, true);

            // Canvas의 중심(0,0,0)을 기준으로 이동
            seq.Join(rectTransform.DOLocalMove(targetLocalPos, 0.3f).SetEase(Ease.OutQuad));
            seq.Join(rectTransform.DOScale(Vector3.one * 1.5f, 0.3f).SetEase(Ease.OutQuad));
        }

        public async UniTask EndFocusEffect(CharacterSlot caster, List<CharacterSlot> targets)
        {
            if (m_dimVisualizer == null) return;

            DOTween.To(() => m_dimVisualizer.alpha, x => m_dimVisualizer.alpha = x, 0f, 0.3f);

            var seq = DOTween.Sequence();

            if (caster != null) RestoreSlotAnim(caster, seq);
            
            if (targets != null)
            {
                foreach (var target in targets)
                {
                    if (target != null) RestoreSlotAnim(target, seq);
                }
            }

            var tcs = new UniTaskCompletionSource();
            seq.OnComplete(() => 
            {
                // 애니메이션 종료 후 레이아웃 데이터 복원
                if (caster != null) RestoreSlotLayout(caster);
                if (targets != null)
                {
                    foreach (var target in targets)
                    {
                        if (target != null) RestoreSlotLayout(target);
                    }
                }
                
                m_dimVisualizer.gameObject.SetActive(false);
                tcs.TrySetResult();
            });

            await tcs.Task;
        }

        private void RestoreSlotAnim(CharacterSlot slot, Sequence seq)
        {
            if (!m_originalSlotData.TryGetValue(slot, out var data)) return;
            var rectTransform = slot.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            seq.Join(rectTransform.DOScale(data.localScale, 0.3f).SetEase(Ease.OutQuad));
            // Dim FadeOut과 함께 크기만 줄어들게 처리. 위치는 Layout 복원 시 즉시 돌아감.
        }

        private void RestoreSlotLayout(CharacterSlot slot)
        {
            if (!m_originalSlotData.TryGetValue(slot, out var data)) return;
            var rectTransform = slot.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            // 계층 복구
            rectTransform.SetParent(data.parent, false);
            rectTransform.SetSiblingIndex(data.siblingIndex);

            // 레이아웃 및 트랜스폼 데이터 복구
            rectTransform.anchoredPosition = data.anchoredPosition;
            rectTransform.localPosition = data.localPosition;
            rectTransform.localScale = data.localScale;
        }
    }
}
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace BattleSystem.UI
{
    public class TextVisualizer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_Text;
        [SerializeField] private Color m_DamageColor = Color.red;
        [SerializeField] private Color m_CriticalColor = Color.yellow;
        [SerializeField] private Color m_HealColor = Color.green;
        [SerializeField] private Color m_MissColor = Color.white;
        [SerializeField] private float m_Duration = 0.8f;
        [SerializeField] private float m_MoveDistance = 50f;

        private Vector3 m_StartLocalPosition;
        private Sequence m_AnimationSequence;

        private void Awake()
        {
            if (m_Text == null)
            {
                m_Text = GetComponent<TextMeshProUGUI>();
            }
            
            if (m_Text != null)
            {
                m_StartLocalPosition = m_Text.transform.localPosition;
                m_Text.gameObject.SetActive(false);
            }
        }

        public void Show(int amount, bool isHeal, bool isCritical = false)
        {
            if (m_Text == null) return;

            // 기존 진행 중인 연출 중단
            if (m_AnimationSequence != null && m_AnimationSequence.IsActive())
            {
                m_AnimationSequence.Kill();
            }

            // 초기 상태로 리셋 및 약간의 랜덤 X 오프셋으로 겹침 방지
            m_Text.transform.localPosition = m_StartLocalPosition + new Vector3(Random.Range(-15f, 15f), 0f, 0f);
            m_Text.color = new Color(m_Text.color.r, m_Text.color.g, m_Text.color.b, 1f);
            
            // 수치 및 색상 설정
            switch ((isHeal, isCritical))
            {
                case (true, true):
                    m_Text.text = $"치명타\n{amount}!";
                    m_Text.color = m_HealColor;
                    break;
                case (true, false):
                    m_Text.text = amount.ToString();
                    m_Text.color = m_HealColor;
                    break;
                case (false, true):
                    m_Text.text = $"치명타\n{amount}!";
                    m_Text.color = m_CriticalColor;
                    break;
                case (false, false):
                    m_Text.text = amount.ToString();
                    m_Text.color = m_DamageColor;
                    break;
            }
            
            m_Text.gameObject.SetActive(true);

            // DOTween 연출 재생
            m_AnimationSequence = DOTween.Sequence();
            m_AnimationSequence.Join(m_Text.transform.DOLocalMoveY(m_StartLocalPosition.y + m_MoveDistance, m_Duration).SetEase(Ease.OutQuad));
            m_AnimationSequence.Join(m_Text.DOFade(0f, m_Duration).SetEase(Ease.InQuad));
            m_AnimationSequence.OnComplete(() =>
            {
                m_Text.gameObject.SetActive(false);
                m_Text.transform.localPosition = m_StartLocalPosition; // 위치 원복
            });
        }

        public void ShowMiss()
        {
            if (m_Text == null) return;

            // 기존 진행 중인 연출 중단
            if (m_AnimationSequence != null && m_AnimationSequence.IsActive())
            {
                m_AnimationSequence.Kill();
            }

            // 초기 상태로 리셋 및 약간의 랜덤 X 오프셋으로 겹침 방지
            m_Text.transform.localPosition = m_StartLocalPosition + new Vector3(Random.Range(-15f, 15f), 0f, 0f);
            m_Text.color = new Color(m_Text.color.r, m_Text.color.g, m_Text.color.b, 1f);
            
            // 텍스트 및 색상 설정
            m_Text.text = "빗나감";
            m_Text.color = m_MissColor;
            
            m_Text.gameObject.SetActive(true);

            // DOTween 연출 재생
            m_AnimationSequence = DOTween.Sequence();
            m_AnimationSequence.Join(m_Text.transform.DOLocalMoveY(m_StartLocalPosition.y + m_MoveDistance, m_Duration).SetEase(Ease.OutQuad));
            m_AnimationSequence.Join(m_Text.DOFade(0f, m_Duration).SetEase(Ease.InQuad));
            m_AnimationSequence.OnComplete(() =>
            {
                m_Text.gameObject.SetActive(false);
                m_Text.transform.localPosition = m_StartLocalPosition; // 위치 원복
            });
        }

        private void OnDestroy()
        {
            if (m_AnimationSequence != null && m_AnimationSequence.IsActive())
            {
                m_AnimationSequence.Kill();
            }
        }
    }
}

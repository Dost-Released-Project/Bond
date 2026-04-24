// using UnityEngine;
//
// namespace _02.Scripts.UI.Title
// {
//     /// <summary>
//     /// [V] Title 씬의 전체 버튼 관리자입니다.
//     /// BaseHybridScrollUIView를 상속받아 공통 UX 로직을 재사용합니다.
//     /// </summary>
//     public class TitleUIView : BaseHybridScrollUIView
//     {
//         protected override void Start()
//         {
//             // 부모의 기본 이벤트 연결 실행 (버튼 클릭 시 이동 로직 포함)
//             base.Start();
//
//             // 타이틀 전용: 불러오기 버튼 활성화 체크
//             for (int i = 0; i < buttons.Count; i++)
//             {
//                 if (buttons[i] != null && buttons[i].ButtonID == "Load")
//                 {
//                     bool canLoad = DataHub.Instance.HasSaveFile;
//                     buttons[i].SetInteractable(canLoad);
//                 }
//             }
//         }
//
//         public override void ConfirmSelection()
//         {
//             int finalIndex = Mathf.RoundToInt(m_TargetScrollPos);
//             if (finalIndex >= 0 && finalIndex < buttons.Count)
//             {
//                 var selectedButton = buttons[finalIndex];
//                 if (selectedButton != null)
//                 {
//                     // Load 버튼이고 세이브가 없으면 실행 안 함
//                     if (selectedButton.ButtonID == "Load" && !DataHub.Instance.HasSaveFile) return;
//                     
//                     // 데이터 주도 로직(Apply) 실행
//                     base.ConfirmSelection();
//                 }
//             }
//         }
//     }
// }

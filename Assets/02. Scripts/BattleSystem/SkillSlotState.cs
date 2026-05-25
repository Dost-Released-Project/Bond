public enum SkillSlotState
{
    Inactive,   // 내 턴이 아님 — 흑백 처리, 클릭 불가
    Ready,      // 내 턴, 미선택 — 컬러, 클릭 가능
    Selected,   // 선택됨 — 테두리 강조
}

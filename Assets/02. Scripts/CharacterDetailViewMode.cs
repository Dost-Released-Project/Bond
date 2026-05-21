public enum CharacterDetailViewMode
{
    FullEdit,   // 마을씬, 탐사 중 캠프/휴식 — 역할·리액션·장비 모두 편집 가능
    EquipOnly,  // 탐사 중 (전투 외) — 장비 교체만 가능
    ReadOnly,   // 전투 중 — 모든 편집 불가, 열람만 가능
}

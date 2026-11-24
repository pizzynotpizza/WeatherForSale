# Story 007: Freeplay + บอส Tempest ลับ

> **Epic**: Weather for Sale
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 6`

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.22f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Features 6, Goal & fail state):*

- [ ] เลือก Stage (Freeplay) จากหน้าเลือกโหมดได้เฉพาะหลังเคลียร์ Story ครั้งแรก (อ่าน flag จาก `ProgressService`); เข้าแล้วข้ามคัตซีนทั้งหมด
- [ ] หน้าเลือกด่าน: 4 แมพ (1-4) รอบช่อง "???" ตรงกลาง แต่ละแมพแสดงดาวที่ได้สูงสุด (0-3); กดแมพแล้วขึ้นแผงรายละเอียด (ชื่อแมพ ดาว เกณฑ์เวลาแต่ละดาว Key Item 0/1 ปุ่ม Start)
- [ ] เล่นแมพนั้น (obby 2 ด่าน + ดูด) ด้วยกติกาเดียวกับ Story (จับเวลา, Checkpoint, Game Over)
- [ ] ผ่านแมพแล้วขึ้นหน้า Complete แล้วกลับหน้าเลือกด่าน; ดาวสูงสุดใหม่ถูกบันทึก
- [ ] ช่อง "???" (บอส Tempest ลับ) ล็อกไว้ จนกว่าผู้เล่นเคยได้ True Ending (อ่าน flag จาก `ProgressService`)
- [ ] ปลดล็อกแล้ว ยังปลดล็อกอยู่หลังปิดและเปิด browser ใหม่
- [ ] การตายใน Freeplay นับรวมใน Achievement "ตาย 10 รอบ"

---

## Implementation Notes

- ใช้ฉากบอสจาก story 008; ชนะแล้วกลับหน้าเลือกด่าน (ไม่เล่นฉากจบ)

---

## Out of Scope

- ด่านหรือบอสใหม่ที่ไม่มีใน Story Mode

---

## Files

- Assets/Scripts/Freeplay/LevelSelect.cs
- Assets/Scripts/Freeplay/FreeplayController.cs
- Assets/Scenes/LevelSelect.unity

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: Test: waived at qa.level minimal

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/weather-for-sale/story-004-four-maps.md`, `production/epics/weather-for-sale/story-005-achievements-progress-save.md`, `production/epics/weather-for-sale/story-006-story-mode.md`, `production/epics/weather-for-sale/story-008-tempest-boss-fight.md`
- Unlocks: None

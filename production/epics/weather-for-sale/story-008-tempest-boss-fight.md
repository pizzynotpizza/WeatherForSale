# Story 008: บอส Tempest (ลานบอส หลบการโจมตี ดูดด้วยขวดม่วง หลอดเลือด)

> **Epic**: Weather for Sale
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 5`

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.22f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Features 5, In your words — การสู้บอส):*

- [ ] ลานบอส 3D พื้นราบ ไม่มีส่วน obby; ผู้เล่นเดินและกระโดดหลบด้วยระบบควบคุมเดิม (PC + ปุ่มสัมผัส)
- [ ] บอสมีรูปแบบการโจมตีหลายแบบ *(guessed: 3 แบบ เช่น ฟ้าผ่าเป็นจุด, ลมกวาดลาน, ลูกเห็บตกเป็นวง)* แต่ละแบบมีสัญญาณเตือนก่อนโดน
- [ ] หลอดเลือดผู้เล่นบนจอ; โดนโจมตีแล้ว HP ลด; HP หมด = Game Over แล้วเริ่มสู้บอสใหม่
- [ ] ตัวจับเวลาบนจอ; หมดเวลาก่อนชนะ = Game Over แล้วเริ่มสู้บอสใหม่
- [ ] หลังโจมตีครบรอบ บอส "เหนื่อย" ช่วงสั้นๆ พร้อมสัญญาณบนจอ
- [ ] ระหว่างบอสเหนื่อย ใช้ขวดม่วงดูดบอสด้วยการกด/แตะรัวๆ (ระบบเกจจาก story 003) ดูดสำเร็จแล้ว HP บอสลด; หลอดเลือดบอสแสดงบนจอ
- [ ] วนหลบ → ดูด จน HP บอสหมด
- [ ] ชนะ: บอสถูกดูดเข้าขวดม่วง ขวดกลายเป็นสายรุ้ง สภาพอากาศข้างนอกเลิกปั่นป่วน แล้วส่งต่อไปฉากจบ
- [ ] HP ผู้เล่น, ดาเมจ, HP บอส, เวลา, รูปแบบโจมตี และช่วงเหนื่อย เก็บใน config

---

## Implementation Notes

- บอสเป็น state machine: Attack(pattern) → Tired → Suction → Attack… / Defeated
- รูปแบบโจมตีแต่ละแบบเป็นคลาส/ScriptableObject แยก เพิ่มแบบใหม่ได้โดยไม่แก้ตัวบอส
- ฉากบอสเดียวกันใช้ทั้ง Story Mode (story 006) และ Freeplay (story 007); บอกผลชนะผ่าน event ให้ผู้เรียกตัดสินว่าจะไปฉากจบหรือกลับหน้าเลือกด่าน

---

## Out of Scope

- Story 006: คัตซีน True Ending และการตั้ง flag ปลดล็อก
- Story 007: ทางเข้าบอสลับใน Freeplay

---

## Files

- Assets/Scripts/Boss/TempestBoss.cs
- Assets/Scripts/Boss/BossAttackPattern.cs
- Assets/Scripts/Boss/Patterns/*.cs
- Assets/Scripts/Boss/BossConfig.cs
- Assets/Scripts/Player/PlayerHealth.cs
- Assets/Scripts/UI/BossHud.cs
- Assets/Scenes/Boss_Tempest.unity

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: Test: waived at qa.level minimal (screenshot ของลานบอสและฉากชนะแนะนำให้มี)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/weather-for-sale/story-003-suction-minigame-loop.md`, `production/epics/weather-for-sale/story-004-four-maps.md`, `production/epics/weather-for-sale/story-005-achievements-progress-save.md`
- Unlocks: `production/epics/weather-for-sale/story-006-story-mode.md`, `production/epics/weather-for-sale/story-007-freeplay-secret-boss.md`

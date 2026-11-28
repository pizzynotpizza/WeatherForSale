# Story 008: บอส Tempest (ลานบอส หลบการโจมตี ดูดด้วยขวดม่วง หลอดเลือด)

> **Epic**: Weather for Sale
> **Status**: In Progress
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-28

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

**Design changes asked by the user while building (2026-11-28), these override the criteria above:**
- No player health. A hit only throws Sam (`knockback` 28 m/s); falling off the arena (no wall) or the 3-minute clock running out is Game Over. Replaces the "HP ผู้เล่น / หลอดเลือดผู้เล่น" parts of criteria 3 and 9.
- Boss HP 5, three phases from the HP left: phase 1 (HP 5) plays one attack (lightning); phase 2 (HP 4-3) plays 2 random attacks per round; phase 3 (HP 2-1) plays 3, faster. One suction takes 1 HP; there is no "direct hit" banner and no one-shot. After a suction the boss clutches its head (shake/squash for now; a real rig is pending).
- From phase 3 on, every successful suction drops a Storm Crystal; walking into it makes the next suction easier (x1.8 gain, +5 s, no decay).
- Four attacks (criterion 2 said 3): lightning circles (red circle + column, no overlap, aimed at Sam), wind sweep (jump over), hail rings (full closed rings), charge (5 s lane warning; the boss waits in the sky over the edge, drops to the floor, then dashes).
- Boss suction uses the map suction's keys (W/A/S/D) but is easier (x2.2 gain, 0.3 decay, 12 s); Sam holds a purple bottle in the map suction's pulling pose. On the final blow the boss is drawn into that bottle, the sky turns blue and the rainbow rises in front of Sam.
- Boss bar at the top centre under the clock; the clock plate is the same small plate in every obby level and the boss arena.

---

## Out of Scope

- Story 006: คัตซีน True Ending และการตั้ง flag ปลดล็อก
- Story 007: ทางเข้าบอสลับใน Freeplay

---

## Files

- Assets/Scripts/Boss/IBossArena.cs
- Assets/Scripts/Boss/BossAttackPattern.cs
- Assets/Scripts/Boss/BossConfig.cs
- Assets/Scripts/Boss/BossShapes.cs
- Assets/Scripts/Boss/TempestBoss.cs
- Assets/Scripts/Boss/BossSuction.cs
- Assets/Scripts/Boss/BossSky.cs
- Assets/Scripts/Boss/BossFightController.cs
- Assets/Scripts/Boss/SuctionBoostItem.cs
- Assets/Scripts/Boss/Patterns/LightningStrikePattern.cs
- Assets/Scripts/Boss/Patterns/WindSweepPattern.cs
- Assets/Scripts/Boss/Patterns/HailRingPattern.cs
- Assets/Scripts/Boss/Patterns/ChargeDashPattern.cs
- Assets/Scripts/UI/BossHud.cs
- Assets/Scripts/Suction/SuctionStage.cs
- Assets/Shaders/BossSkySwirl.shader
- Assets/Editor/BossSceneBuilder.cs
- Assets/Editor/TimerStyleApplier.cs
- Assets/Editor/UiSheetImporter.cs
- Assets/Editor/SuctionStageBuilder.cs
- Assets/Editor/ObbyTestSceneBuilder.cs
- Assets/Data/Boss/BossConfig.asset
- Assets/Data/Boss/BossPlayerConfig.asset
- Assets/Art/Props/Boss/StormCrystal.prefab
- Assets/Scenes/Boss_Tempest.unity
- Assets/Scenes/Obby_Valley.unity
- Assets/Scenes/Obby_Desert.unity
- Assets/Scenes/Obby_Glacier.unity
- Assets/Scenes/Obby_Rainforest.unity
- Assets/Data/Boss/Pattern_*.asset (4 attack patterns), ArenaDisc/ArenaLip/Inlay_* meshes
- Assets/Art/UI/Sheet/*, Assets/Art/UI/Boss/*, Button_Start/Quit.png, WeatherForSale_Logo.png (sliced from design/ref UI sheets)

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

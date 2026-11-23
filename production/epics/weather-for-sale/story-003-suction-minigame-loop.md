# Story 003: มินิเกมดูดอากาศ + เชื่อมลูป ร้าน → obby 2 ด่าน → ดูด → ร้าน

> **Epic**: Weather for Sale
> **Status**: Complete
> **Layer**: Core
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-24

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 4`

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.22f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Features 4, Core loop, Build order 3):*

- [ ] ผ่าน obby ด่านที่ 2 ของแมพแล้วถึงจุดเก็บอากาศ เข้ามินิเกมดูดแบบ Quick Time Event
- [ ] PC: ระบบสุ่มปุ่มคีย์บอร์ด 1 ปุ่มแสดงบนจอ ผู้เล่นกดปุ่มนั้นรัวๆ เพื่อเพิ่มเกจ Collecting; ปุ่มอื่นไม่เพิ่มเกจ; สุ่มปุ่มใหม่เป็นช่วงๆ ระหว่างเติมเกจ *(guessed: ทุก 25% ของเกจ — ค่าใน config)*
- [ ] มือถือ: ปุ่มแตะขึ้นที่ตำแหน่งสุ่มบนจอ ผู้เล่นแตะรัวๆ ตามปุ่มนั้นเพื่อเพิ่มเกจ; ปุ่มย้ายตำแหน่งสุ่มใหม่เป็นช่วงๆ *(guessed: ทุก 25% ของเกจ)*; ตำแหน่งต้องอยู่ในขอบจอที่แตะถึง
- [ ] ชุดปุ่มที่สุ่มได้ (PC) และค่าเพิ่มเกจต่อครั้ง เก็บใน config; เกจเริ่มที่ 0
- [ ] เกจเต็ม 100 = เก็บอากาศสำเร็จ ขึ้นหน้า "Weather Collected" พร้อมขวดอากาศชนิดของแมพนั้น
- [ ] เกจแสดงบนจอตลอด และค่าไม่เกิน 100
- [ ] หลัง Weather Collected ขึ้นหน้า Complete: ดาว 0-3 ตามเวลารวมที่ใช้ (เกณฑ์ใน config), เวลา, Key Item (Shard) 0/1 และปุ่ม Next กลับร้าน
- [ ] ลูปเต็มรอบทำงาน: ร้าน → เลือกแมพ → Loading → obby ด่าน 1 → obby ด่าน 2 → ดูด → Weather Collected → Complete → กลับร้านส่งขวด
- [ ] Game Over ในด่านใดก็ตาม เริ่มด่านนั้นใหม่ได้ ไม่หลุดออกจากลูป

---

## Implementation Notes

- ค่าต่อการกด (และการลดลงของเกจ ถ้ามี) เก็บใน config
- แยก logic เกจ (คำนวณ, clamp 0-100) เป็นคลาส C# ธรรมดา ทดสอบได้โดยไม่ต้องมีฉาก

---

## Out of Scope

- Story 004: แมพครบ 4 แมพ (story นี้ใช้แมพเดียวพอให้ลูปครบ)

---

## Files

- Assets/Scripts/Suction/SuctionGauge.cs (plain C# gauge 0-100, decay, steps)
- Assets/Scripts/Suction/SuctionConfig.cs, Assets/Data/Suction/SuctionConfig.asset (keys W A S D, 3.5 per press, 7/s decay, change every 10% or 1.5 s, camera, bottle, pulling pose, orb, streaks, stars)
- Assets/Scripts/Suction/SuctionStrings.cs, Assets/Data/Suction/SuctionStrings.asset ("Collect the Weather!", "Press {0}!")
- Assets/Scripts/Suction/SuctionMinigame.cs (UI and input; the key prompt and tap button move around the screen)
- Assets/Scripts/Suction/SuctionStage.cs (in place at the end of level 2: camera to the front, bottle in both hands, whole-body tug-of-war rock with feet planted, orb, streaks)
- Assets/Scripts/Core/GameLoopController.cs, Assets/Scripts/Core/GameLoopConfig.cs, Assets/Data/GameLoopConfig.asset
- Assets/Scripts/UI/WeatherCollectedScreen.cs (the bottle shakes in place)
- Assets/Scripts/Obby/ObbyLevelManager.cs (countdown keeps running through the suction; time out = Game Over)
- Assets/Scripts/UI/StageCompletePanel.cs
- Assets/Scripts/UI/FpsOverlay.cs (hidden unless ?fps in the web build URL)
- Assets/Scripts/Shop/ShopSession.cs (run state: level, seconds, deaths, shard)
- Assets/Editor/SuctionStageBuilder.cs (suction HUD, screens, orb, streaks; Assets/Materials/Suction/*)
- Assets/Editor/ObbyTestSceneBuilder.cs (level 2 per map, GameLoop wiring, Next button, suction on _2)
- Assets/Scenes/Obby_Rainforest_2.unity, Obby_Valley_2.unity, Obby_Desert_2.unity, Obby_Glacier_2.unity (generated level 2 with suction)
- Assets/Scenes/Obby_<Map>_1.unity (regenerated with GameLoop)
- Assets/Data/ObbyLevelConfig_<Map>_2.asset (generated, 0.8x time)
- ProjectSettings/EditorBuildSettings.asset (level 2 scenes appended)
- (no test file: waived at qa.level minimal)

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: Test: waived at qa.level minimal (playtest note ของลูปเต็มรอบแนะนำให้มี)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/weather-for-sale/story-001-obby-first-level.md`, `production/epics/weather-for-sale/story-002-shop-map-select.md`
- Unlocks: `production/epics/weather-for-sale/story-005-achievements-progress-save.md`, `production/epics/weather-for-sale/story-006-story-mode.md`

## Completion Notes
**Completed**: 2026-11-24
**Criteria**: 9/9 passing (no deferred items)
**Deviations**: Advisory only. Evidence images 01, 02 and 06 were captured before the later UI changes (fps text, old gauge position); not re-captured. Phone and WebGL testing not done (story 001 still In Progress); played in the Editor with a simulated Touchscreen. Changed files outside `## Files` are story 001/002 work not yet committed, plus the unused `Assets/Scenes/Suction.unity`.
**Test Evidence**: waived at qa.level minimal; screenshots in `production/qa/evidence/story-003-suction-minigame-loop/` (01-08), stamp unchanged for all 14 files
**Reviewed by**: self, no independent review (solo mode; QL-TEST-COVERAGE and LP-CODE-REVIEW skipped)

## Calls beyond the design
- Keys W A S D; the key and its position change every 10% of the gauge or every 1.5 s (story guessed 25%) (`Assets/Data/Suction/SuctionConfig.asset`) · kept 2026-11-24
- +3.5 per press, -7 per second decay, wrong key has no penalty (`SuctionConfig.asset`) · kept 2026-11-24
- Stars: 3 at 120 s or less, 2 at 180 s, 1 at 270 s, same for every map (`SuctionConfig.asset`) · kept 2026-11-24
- Level 2 is a mirrored layout at 0.8x time; Next button after a level; 0.8 s input delay on Continue/Next (`ObbyTestSceneBuilder.cs`, `SuctionConfig.asset`) · kept 2026-11-24
- Suction runs on level 2's countdown; running out of time is Game Over and Restart replays level 2 (`GameLoopController.cs`, `ObbyLevelManager.cs`) · kept 2026-11-24
- "Collect the Weather!" and the gauge sit under the timer at the top; the bottle shakes on Weather Collected (`SuctionStageBuilder.cs`, `WeatherCollectedScreen.cs`) · kept 2026-11-24
- Sam's pull pose: whole body rocks about the ankles with feet planted (`SuctionStage.cs`, `SuctionConfig.asset`) · kept 2026-11-24

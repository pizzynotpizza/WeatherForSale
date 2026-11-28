# Story 005: Achievement + จำความคืบหน้า (localStorage)

> **Epic**: Weather for Sale
> **Status**: Complete
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-28

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 7`

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.22f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Features 7, Out of scope):*

- [ ] เก็บความคืบหน้า: Shard ที่เก็บได้, ดาว, ตอนจบที่เคยได้, Achievement, จำนวนครั้งที่ตาย, ด่านที่ปลดล็อก, flag บอสลับ
- [ ] บน WebGL เขียนลง `localStorage` ผ่านปลั๊กอิน `.jslib` ด้วย key ที่ขึ้นต้นเฉพาะเกม (เช่น `weatherforsale.`)
- [ ] ปิดแท็บ/ปิด browser แล้วเข้าใหม่ ค่ายังอยู่; อัปโหลด build ใหม่ขึ้น itch แล้วค่ายังอยู่
- [ ] ใน Editor/แพลตฟอร์มอื่น ใช้ที่เก็บสำรอง (เช่น PlayerPrefs) ผ่าน interface เดียวกัน
- [ ] Achievement 4 รายการปลดล็อกถูกเงื่อนไข: ตาย 10 รอบ, ได้ดาว 12 ดวง, ได้ Normal Ending, ได้ True Ending
- [ ] ปลดล็อก Achievement แล้วแสดงแจ้งเตือนบนจอ และไม่ปลดซ้ำ
- [ ] ข้อมูลเสียหรือว่างเปล่า (เช่นโหมดไม่ระบุตัวตน) เกมยังเริ่มได้ด้วยค่าเริ่มต้น ไม่ error

---

## Implementation Notes

- `IProgressStore` (Load/Save) มี 2 implementation: `LocalStorageProgressStore` (WebGL) และ `PlayerPrefsProgressStore`; inject เข้า `ProgressService`
- serialize ความคืบหน้าเป็น JSON (JsonUtility) แล้วเก็บเป็น string เดียว พร้อม version field
- ไม่ใช้ `PlayerPrefs` บน WebGL เพราะผูกกับ URL หน้าเกม ซึ่ง itch เปลี่ยนทุกครั้งที่อัปโหลด

---

## Out of Scope

- Cloud save / ผูกกับบัญชี itch (อยู่ใน Out of scope ของ brief)
- Story 006/007: การตั้งค่าเมื่อจบเกมและการอ่าน flag บอสลับ

---

## Files

- Assets/Plugins/WebGL/LocalStorage.jslib
- Assets/Scripts/Progress/WeatherForSale.Progress.asmdef
- Assets/Scripts/Progress/IAchievementSource.cs
- Assets/Scripts/Progress/ProgressData.cs
- Assets/Scripts/Progress/IProgressStore.cs
- Assets/Scripts/Progress/LocalStorageProgressStore.cs
- Assets/Scripts/Progress/PlayerPrefsProgressStore.cs
- Assets/Scripts/Progress/ProgressService.cs
- Assets/Scripts/Progress/AchievementService.cs
- Assets/Scripts/Progress/AchievementToast.cs
- Assets/Data/ProgressService.asset
- Assets/Scripts/Core/GameLoopController.cs
- Assets/Scripts/Suction/SuctionMinigame.cs
- Assets/Scripts/Shop/ShopFlowController.cs
- Assets/Editor/ObbyTestSceneBuilder.cs
- Assets/Editor/ShopSceneBuilder.cs
- Assets/Scenes/Shop.unity
- Assets/Scenes/Obby_Valley.unity
- Assets/Scenes/Obby_Desert.unity
- Assets/Scenes/Obby_Glacier.unity
- Assets/Scenes/Obby_Rainforest.unity
- Assets/Tests/EditMode/WeatherForSale.Tests.EditMode.asmdef
- Assets/Tests/EditMode/Progress/AchievementServiceTests.cs

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: Test: waived at qa.level minimal

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/weather-for-sale/story-003-suction-minigame-loop.md`
- Unlocks: `production/epics/weather-for-sale/story-006-story-mode.md`, `production/epics/weather-for-sale/story-007-freeplay-secret-boss.md`

## Completion Notes
**Completed**: 2026-11-28
**Criteria**: 6/7 passing (AC3 DEFERRED: close tab/browser and re-upload to itch, check in a WebGL build)
**Deviations**: ADVISORY: toast size, colour and position are literals in `AchievementToast.cs`, not data
**Test Evidence**: Assets/Tests/EditMode/Progress/AchievementServiceTests.cs (14/14 passed; waived at qa.level minimal); production/qa/evidence/story-005-achievements-progress-save/achievement-toast.png
**Reviewed by**: self, no independent review

## Calls beyond the design
- Unlock notice at top centre below the level timer, 3 s, dark brown panel, built-in font (`AchievementToast.cs`) · kept 2026-11-28
- Notice text "Achievement Unlocked!" and achievement names set on `Assets/Data/ProgressService.asset` · kept 2026-11-28
- Deaths counted lifetime, every death in every map including restarted attempts (`GameLoopController.OnDeathCountChanged`) · kept 2026-11-28
- Best stars and cleared map saved when the Complete screen shows, before Next (`SuctionMinigame.OnContinue`) · kept 2026-11-28
- Endings and secret boss flag: storage and API only (`RecordEnding`, `SetSecretBossUnlocked`), nothing calls them yet · kept 2026-11-28
- Save key `weatherforsale.progress` (`ProgressService.StorageKey`) · kept 2026-11-28

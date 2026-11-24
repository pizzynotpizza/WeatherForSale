# Story 006: Story Mode: Title, เลือกโหมด, คัตซีน, Shard, ดาว, บอส, ฉากจบ 2 แบบ

> **Epic**: Weather for Sale
> **Status**: Ready
> **Layer**: Presentation
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

*From `design/game-brief.md` (Features 5, Goal & fail state, First minute):*

- [ ] หน้า Title ใช้โลโก้และปุ่ม Start จาก `design/ref/` มีปุ่ม Start, Option และไอคอนสมุด Achievement; Option เปิดแผงเสียง All/Music/SFX (0-100), Back, Quit
- [ ] กด Start แล้วหน้าเลือกโหมด: Story Mode / Stage (Freeplay) — Stage ล็อกไว้พร้อมข้อความ "ปลดล็อกเมื่อเคลียร์ Story" จนกว่าจะจบ Story ครั้งแรก
- [ ] Story Mode เริ่มด้วยคัตซีน: Samตื่นนอน แต่งตัว เดินไปร้าน Uncle Johnไม่อยู่ มีโน้ตฝากดูแลร้าน 1 วัน แล้วเข้าสู่ร้าน
- [ ] ลูกค้า 4 คนมาตามลำดับ แต่ละคนพาไปแมพของตัวเอง (ลูปจาก story 003)
- [ ] Shard ม่วงซ่อนแมพละ 1 ชิ้น และดาว 3 ดวงต่อแมพตามเวลา (รวม 12) เก็บแล้วนับและบันทึกผ่าน `ProgressService` (story 005)
- [ ] ครบ 4 ลูกค้า Shard ไม่ครบ 4 → Normal Ending: Uncle Johnกลับมา เห็นร้านไปได้สวย ให้ดูแลร้านต่อ ท้องฟ้าครึ้มเล็กน้อย → จอดำ "There's something you missed / Collect all 4 Key Items" → กลับหน้าหลัก
- [ ] Shard ครบ 4 → คัตซีน: Uncle Johnกลับมา ฟ้าผ่า ลมแรง Tempestปรากฏ, Shard รวมร่างเป็นขวดม่วง → สู้บอส (story 008) → "Boss Sealed" → True Ending (ท้องฟ้าสดใส สายรุ้ง) → หน้า Complete (ดาว เวลา ปุ่ม Menu) → กลับหน้าหลัก
- [ ] จบ Story ครั้งแรก (ตอนจบใดก็ได้) ปลดล็อกโหมด Stage และบันทึกถาวร
- [ ] ได้ True Ending แล้วตั้ง flag ปลดล็อกบอสลับใน Freeplay และบันทึกถาวร
- [ ] ข้ามคัตซีนได้ด้วยปุ่ม Skip

---

## Implementation Notes

- คัตซีนทำแบบเรียบง่าย (ภาพนิ่งหรือกล้อง + กล่องข้อความ) พอสำหรับ WebGL
- การสู้บอสทั้งหมดอยู่ใน story 008; story นี้แค่เรียกฉากบอสและรับผลชนะเพื่อไปคัตซีน True Ending

---

## Out of Scope

- Story 007: Freeplay และบอสลับ
- Story 008: ระบบสู้บอส

---

## Files

- Assets/Scripts/UI/TitleScreen.cs
- Assets/Scripts/UI/ModeSelect.cs
- Assets/Scripts/Story/CutscenePlayer.cs
- Assets/Scripts/Story/StoryModeController.cs
- Assets/Scripts/Story/ShardPickup.cs
- Assets/Scenes/Title.unity

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: Test: waived at qa.level minimal (screenshot ของ Title และฉากจบแนะนำให้มี)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/weather-for-sale/story-003-suction-minigame-loop.md`, `production/epics/weather-for-sale/story-004-four-maps.md`, `production/epics/weather-for-sale/story-005-achievements-progress-save.md`, `production/epics/weather-for-sale/story-008-tempest-boss-fight.md`
- Unlocks: `production/epics/weather-for-sale/story-007-freeplay-secret-boss.md`

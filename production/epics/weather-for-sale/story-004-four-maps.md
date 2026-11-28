# Story 004: 4 แมพ × 2 ด่าน obby

> **Epic**: Weather for Sale
> **Status**: Complete
> **Layer**: Feature
> **Type**: Visual/Feel
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-28

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 3`

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.22f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Features 3, Challenge, Look) และรายละเอียดด่านใน `design/brief/Weather_for_Sale.pdf`:*

- [ ] มี 4 แมพ: ป่าดิบชื้น, หุบเขา, ทะเลทราย, ภูเขาน้ำแข็ง แต่ละแมพมี obby 2 ด่าน (รวม 8 ด่าน)
- [ ] อุปสรรคตาม PDF ครบทุกด่าน:
  - ป่าดิบชื้น 1 · ทางเห็ดเด้ง: เห็ดยักษ์ลอยเหนือลำธาร, เห็ดเด้งส่งตัวสูง, เห็ดที่ยุบหลังเหยียบ 1 วินาที, ดอก Rainbell แกว่งขวางทาง
  - ป่าดิบชื้น 2 · ท่อนไม้กับเถาวัลย์: ท่อนไม้ลอยน้ำหมุนกลิ้ง, โหนเถาวัลย์แกว่ง, ไต่กิ่งไม้, กิ่งผุหักเมื่อยืนนาน
  - หุบเขา 1 · หินลอยกลางลม: หินลอยขึ้นลงและหมุนช้า, ลมกระโชกเป็นจังหวะดันด้านข้าง
  - หุบเขา 2 · ใบพัดและช่องลม: ใบพัดยักษ์กวาดบนหิ้งแคบ, ช่องลมพ่นตัวขึ้น, สะพานแขวนแกว่ง
  - ทะเลทราย 1 · กระบองเพชรกับเสาไฟ: กระบองเพชร (โดนแล้วกลับ Checkpoint), เสาหินแคบ, ลูกกลิ้งหญ้าแห้งกลิ้งสวน
  - ทะเลทราย 2 · บ่อทรายดูด: แผ่นหินลอยที่ค่อยๆ จมเมื่อยืน
  - ภูเขาน้ำแข็ง 1 · แผ่นน้ำแข็งลื่น: พื้นลื่นไถล, แผ่นมีรอยร้าวแตกหลังเหยียบ
  - ภูเขาน้ำแข็ง 2 · เนินหิมะถล่ม: ขั้นหินบนเนินลื่น, ก้อนหิมะกลิ้งลงเป็นระลอก, โขดหินให้หลบ
- [ ] ตกหรือโดนกับดักแล้วเกิดใหม่ที่ Checkpoint ล่าสุด; ทุกด่านมี Checkpoint กลางด่านและเวลาจำกัดของตัวเองจาก config
- [ ] ด่านหลังเวลาบีบขึ้นหรือสิ่งกีดขวางมากขึ้นกว่าด่านแรก
- [ ] แต่ละแมพมีจุดซ่อน Shard ม่วง 1 ชิ้นบนทางแยกลับที่กระโดดยากกว่าทางหลัก (ตำแหน่งวางไว้ การเก็บนับผลใน story 006)
- [ ] ใช้ asset จาก `Assets/` (เช่น KayKit Forest) ปรับสี/UV ให้เข้ากับภูมิอากาศแต่ละแมพ; โมเดลที่ไม่มีให้รายงานเพื่อทำ prompt ChatGPT → Meshy ไม่ใช้กล่องแทน
- [ ] สไตล์ 3D low-poly สีสดตาม `design/ref/`
- [ ] ทุกด่านเล่นได้ลื่นบน WebGL มือถือ (ไม่ต่ำกว่าเกณฑ์ใน story 001)

---

## Implementation Notes

- ใช้ prefab สิ่งกีดขวางที่ปรับค่าได้ร่วมกันระหว่างด่าน
- คุมจำนวน polygon และ texture ให้พอดีกับ WebGL มือถือ
- ภาพอ้างอิงสีและองค์ประกอบของแต่ละแมพ (ผู้ใช้ส่งมา 2026-11-23 ควรเซฟไว้ที่ `design/ref/maps/`):
  - Rainforest: หน้าผาหินเทาคลุมมอส ต้นไม้ใหญ่และเถาวัลย์ น้ำตกหลายชั้น แม่น้ำสีเทอร์ควอยส์ด้านล่าง หมอกและฝนตก ฟ้าเทาฟ้า
  - Valley: หุบเขาหินทรายสีส้ม เสาหินยอดแบนมีต้นไม้เล็ก เหวลึกเต็มไปด้วยหมอก/เมฆ เส้นลมสีขาวพัดผ่าน ฟ้าใส
  - Desert: เนินทรายสีทอง หินสีแดงอมส้ม ซุ้มหินโค้ง กระบองเพชรมีดอกชมพู หญ้าแห้ง พระอาทิตย์จ้า ฟ้าใส
  - Glacier: หน้าผาหินเทาคลุมหิมะ ยอดเขาแหลม สนคลุมหิมะ หมอกในหุบ ฟ้าใส แดดอ่อน
- ด่านตั้งต้น (story 002) ใช้สีจากภาพเหล่านี้แล้วใน `ObbyTestSceneBuilder.MapThemes`; story นี้เปลี่ยน layout และใส่องค์ประกอบจริง

---

## Out of Scope

- Story 006: การเก็บ Shard/ดาวที่นับผลจริง

---

## Files

- Assets/Editor/MapLevelBuilder.cs, MapLevelBuilder.Rainforest.cs, MapLevelBuilder.Valley.cs, MapLevelBuilder.Desert.cs, MapLevelBuilder.Glacier.cs (layouts and scenery for all 8 levels)
- Assets/Editor/ObbyTestSceneBuilder.cs (calls the layouts; soft shadows), Assets/Editor/SuctionStageBuilder.cs (open bottle, prompt off Sam)
- Assets/Scripts/Obby/Obstacles/*.cs: RideableSurface, ObstacleUtility, BouncePad, CollapsingPlatform, PlatformMover, PendulumSwing, RollingSurface, ShardPickup, Spinner, WindGust, StreakDrift, UpdraftVent, RollingHazard, SinkingPlatform, IceSurface
- Assets/Scripts/Obby/PlayerController.cs (platform carry, Launch, AddExternalVelocity, AddLift, ice traction), Assets/Scripts/Obby/ObbyLevelManager.cs (one hit = one fall)
- Assets/Scripts/Suction/SuctionStage.cs, SuctionMinigame.cs, SuctionConfig.cs, Assets/Data/Suction/SuctionConfig.asset (weather waits on the dais, camera, pull pose, feet locked, no time limit)
- Assets/Scripts/Core/GameLoopController.cs (suction time added to the run)
- Assets/Data/ObbyLevelConfig_<Map>_1/_2.asset (120 s / 100 s)
- Assets/Art/Props/Rainforest/ (Meshy mushroom, Rainbell, crystal + CrystalOnly), Assets/Art/Props/Log/, Assets/Art/Props/Desert/, Assets/Art/Props/Palette/, Assets/Art/Props/Bottle/Bottle_Open*
- Assets/Settings/PC_RPAsset.asset, Mobile_RPAsset.asset (shadow bias)
- Assets/Scenes/Obby_<Map>_1.unity, Obby_<Map>_2.unity (generated)
- (no test file: waived at qa.level minimal)

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**: retained screenshot ของแต่ละแมพใน `production/qa/evidence/` + sign-off ใน `production/qa/evidence/story-004-four-maps-evidence.md`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/weather-for-sale/story-001-obby-first-level.md`
- Unlocks: `production/epics/weather-for-sale/story-006-story-mode.md`, `production/epics/weather-for-sale/story-007-freeplay-secret-boss.md`

## Completion Notes
**Completed**: 2026-11-28
**Criteria**: 7/8 passing (WebGL mobile smoothness: DEFERRED — requires playtest session)
**Deviations**: Criteria text still says 8 levels / 120 s / 100 s; build is 4 merged maps at 180 s. `## Files` list is stale. `Obby_Test.unity` deleted but still in build settings. Evidence stamp older than MapLevelBuilder.cs, MapLevelBuilder.Valley.cs, ObbyTestSceneBuilder.cs.
**Test Evidence**: production/qa/evidence/story-004-four-maps-evidence.md + story-004-four-maps/ (checkpoint-popup.png forced, not walked)
**Reviewed by**: self, no independent review

## Calls beyond the design
- "Checkpoint!" popup, yellow, 1.6 s under the timer (`Assets/Scripts/UI/ObbyHud.cs`) · kept 2026-11-28
- No checkpoint flags (`Assets/Editor/MapLevelBuilder.cs`) · kept 2026-11-28
- Every map 180 s (`Assets/Data/ObbyLevelConfig_<Map>.asset`) · kept 2026-11-28
- Stars 3★ ≤150 s, 2★ ≤200 s, 1★ ≤999 s (`Assets/Data/Suction/SuctionConfig.asset`) · kept 2026-11-28
- Valley bridge as 2 swinging sections per zone (`Assets/Editor/MapLevelBuilder.Valley.cs`) · kept 2026-11-28
- Valley Shard near CP2 (`Assets/Editor/MapLevelBuilder.Valley.cs`) · kept 2026-11-28

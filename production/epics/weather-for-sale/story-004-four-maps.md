# Story 004: 4 แมพ × 2 ด่าน obby

> **Epic**: Weather for Sale
> **Status**: In Progress
> **Layer**: Feature
> **Type**: Visual/Feel
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-24

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

- Assets/Scenes/Maps/Rainforest_1.unity, Rainforest_2.unity
- Assets/Scenes/Maps/Valley_1.unity, Valley_2.unity
- Assets/Scenes/Maps/Desert_1.unity, Desert_2.unity
- Assets/Scenes/Maps/Glacier_1.unity, Glacier_2.unity
- Assets/Scripts/Obby/Obstacles/*.cs

---

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

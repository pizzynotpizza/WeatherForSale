# Epic: Weather for Sale

> **Source**: `design/game-brief.md` (workflow: minimal — no GDD, no ADRs)
> **Engine**: Unity 6000.3.22f1 · target: WebGL on itch.io (PC + mobile browsers)

## Goal
Sam ที่ต้องเฝ้าร้านขายสภาพอากาศแทน Uncle John ต้องกระโดดข้ามด่าน obby แข่งกับเวลาไปดูดอากาศใส่ขวดให้ลูกค้า และตามหา Shard ม่วงเพื่อไปให้ถึง True Ending

## Scope
The brief's Features list (7 items), built in the brief's Build order.

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | obby ด่านแรก: เดิน กระโดด Checkpoint จับเวลา Game Over HUD ปุ่มสัมผัส + ทดสอบ WebGL บนมือถือ | Visual/Feel | Ready | N/A |
| 002 | ร้าน first person: ลูกค้า, เลือกแมพบนเคาน์เตอร์, คำใบ้, ส่งขวด | UI | Complete | N/A |
| 003 | มินิเกมดูดอากาศ + เชื่อมลูป ร้าน → obby 2 ด่าน → ดูด → ร้าน | Integration | Complete | N/A |
| 004 | 4 แมพ × 2 ด่าน obby | Visual/Feel | Ready | N/A |
| 005 | Achievement + จำความคืบหน้า (localStorage) | Logic | Ready | N/A |
| 006 | Story Mode: Title, เลือกโหมด, คัตซีน, Shard, ดาว, บอส, ฉากจบ 2 แบบ | Integration | Ready | N/A |
| 007 | Freeplay + บอส Tempest ลับ | Integration | Ready | N/A |
| 008 | บอส Tempest: ลานบอส หลบการโจมตี ดูดด้วยขวดม่วง หลอดเลือด | Integration | Ready | N/A |

Build order: 001 → 002 → 003 → 004 → 005 → 008 → 006 → 007

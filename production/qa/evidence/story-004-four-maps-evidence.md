# Story 004 — 4 maps: Visual/Feel evidence and sign-off

**Story**: `production/epics/weather-for-sale/story-004-four-maps.md`
**Date**: 2026-11-28 (updated after the Meshy props, Rainforest sections B/C, wide platforms, bridge, hawk and fan changes)
**Branch / commit**: `story-004-four-maps` / `b7790ab2` plus uncommitted changes
**Evidence folder**: `production/qa/evidence/story-004-four-maps/`
**Evidence stamp**: `production/qa/evidence/story-004-four-maps/stamp.txt`
(`MapLevelBuilder.cs` and `MapLevelBuilder.Valley.cs` changed after the stamp: the join checkpoint and the split bridge decks)

Each map is one scene (the old level 1 and level 2 merged, one Shard per map, one 180 s timer, 5 checkpoints).

## Retained screenshots, per map

| Map | Overview / checkpoints | Changed in the later rounds |
|---|---|---|
| Rainforest | `merged-rainforest.png`, `run-checkpoints-Rainforest.png` | `rainforest-leaves-flat.png` (drooping leaf platforms), `meshy-props-in-maps.png` (vine, branch, lily pad), `rainforest-connector-stairs.png` |
| Valley | `merged-valley.png`, `run-checkpoints-Valley.png` | `valley-glide-meshy-*.png`, `valley-hawk-from-behind.png`, `glide-hint.png`, `valley-fans-meshy.png`, `valley-bridge-meshy.png`, `changes-round3.png` (2-blade fans, easy Shard) |
| Desert | `merged-desert.png`, `run-checkpoints-Desert.png` | `desert-tumbleweed-meshy.png`, `desert-tumbleweed-dune.png`, `changes-round3.png` (wide sinking slabs) |
| Glacier | `merged-glacier.png`, `run-checkpoints-Glacier.png` | `changes-round3.png` (wide ice pads), `glacier2-ramp-v2.png` |
| All | `complete-shard.png`, `shop-after-cleanup.png` | |

## What was observed

- Round 2 automated run on all 4 maps: every checkpoint activates, every fall respawns at that checkpoint, the finish starts the vacuum stage, the join stairs walk both ways.
- Round 3 and later: gaps and rises of the changed platforms were measured (all within jump reach); screenshots above.
- **Not observed**: the split bridge decks and the hawks' new oval flight were built and compiled but not looked at in play; no full automated run was repeated after the last changes.
- A script teleporting to checkpoints is not a person playing the jumps.

## Sign-off

One person may sign every row.

| Role | What is approved | Status | Signed by | Date |
|---|---|---|---|---|
| Look (project owner) | Rainforest: scene look, props, leaf / stone sections | [x] Approved | project owner (approved in chat) | 2026-11-28 |
| Look (project owner) | Valley: scene look, glide, Meshy hawks / glider / island / bridge / fans | [x] Approved | project owner (approved in chat) | 2026-11-28 |
| Look (project owner) | Desert: scene look, Meshy cactus / tumbleweed, sinking slabs | [x] Approved | project owner (approved in chat) | 2026-11-28 |
| Look (project owner) | Glacier: scene look, Meshy snowball, wide ice pads | [x] Approved | project owner (approved in chat) | 2026-11-28 |

**Not covered by this sign-off** (deferred to a playtest): the feel half of the story — jump timing, difficulty of each section, whether 180 s is enough, and smoothness on a WebGL phone.

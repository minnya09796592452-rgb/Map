# P1 — Mission 3 Terrain / Map Reverse Engineering

**Status:** Active  
**Independent project timeline:** [TIMELINE.md](TIMELINE.md)  
**Evidence root:** [evidence/](evidence/)  
**Previous primary record:** [Mission 3 reverse-engineering record](evidence/MISSION3_REVERSE_ENGINEERING_RECORD.txt)  
**Master plan:** [CASTAWAY_DEPTHS_MASTER_PLAN.md](evidence/extracted/CASTAWAY_DEPTHS_MASTER_PLAN.md)

## Objective

Recover and verify Stranded Deep Mission 3 map/terrain data from Unity serialized resources and source evidence, establish exact height decoding and world placement, then determine when terrain is sufficiently validated for Godot migration.

## Scope

- EDITOR / OBJECT / HEIGHT map streams and binary decoding.
- Serialized GameObject, Transform, Terrain, TerrainCollider, TerrainData and external references.
- Mission identity and runtime map ordering.
- Terrain world transforms and height-array correlation.
- Read-only source/IL analysis supporting the above.
- Godot terrain migration only after source data and coordinate model are validated.

Gameplay systems are outside this project's declared scope unless explicitly added by the project owner.

## Evidence organization

All 732 files classified as Mission 3 evidence in the pre-migration tree have been relocated under this folder while preserving their prior relative paths beneath `evidence/`:
- `evidence/extracted/` — V16 through V26.2 and default map evidence.
- `evidence/V232/` — Mission 3 source/token/path/transform trace batches.
- `evidence/Scrpic/` — Mission 3 zone-ID IL evidence and method index.
- `evidence/MISSION3_REVERSE_ENGINEERING_RECORD.txt`, `evidence/MISSION_3_TERRAIN_CLASS_MAP.txt`, `evidence/MISSION_3_SOURCE_EXTRACTION_V15.txt`.
- `evidence/Default Maps.zip` — original uploaded archive.

## Current evidence-backed checkpoint

The repository's Mission 3 record describes the HEIGHT stream as 257×257 UInt16 samples (132,098 bytes) and the map as EDITOR, OBJECT and HEIGHT streams. The source record says runtime height loading applies `ToCompressStable()` before decompression. V26.2 records 21,100 serialized objects, including 51 Terrain/TerrainCollider/TerrainData objects globally; the 49-object `level3` grouping is not proof of Mission 3 identity.

**Still unresolved in the existing evidence:** actual runtime Mission 3 World.MapList index; complete TerrainData height arrays; complete static world transforms; exact TerrainData-to-map numerical match; final coordinate model; Godot integration/validation.

Do not promote these pending items to verified based on a report title or parser error count.

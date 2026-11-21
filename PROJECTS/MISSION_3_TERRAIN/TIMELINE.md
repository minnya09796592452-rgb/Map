# P1 Timeline — Mission 3 Terrain / Map

**Timeline type:** project evidence and engineering progression  
**Last repository snapshot inspected:** main @ `9580472dd835cb4aff79fbc1ec59a1a3c4b5b7a1` (2026-11-21)  
**Date caveat:** entries below use dates stated in project records or artifact names. They are not a complete upload chronology.

| Stage | Recorded work | Evidence / status |
|---|---|---|
| V13 | Original game resource inventory | Historical result recorded in master plan; not rerun in this session |
| V14 | Managed/native DLL classification | Historical result recorded; not rerun |
| V15 | Initial ILSpy decompilation | Historical; later V24 source recovery supersedes the old “decompiler not found” note |
| V16 | C# source inventory | 3,182 files indexed per plan; historical |
| V17 | Targeted terrain/world C# source batch | 33 files reported; user-run results retained as historical evidence |
| V18 | Godot project audit | One project / 16 files reported; read-only historical audit |
| V19–V23.2 | Serialized asset, terrain, transform and pointer evidence generations | Present under `evidence/extracted/`; each stage must be interpreted from its own manifest |
| V24 | C# source recovery | 3,182 scanned, 449 selected; manifest/integrity counts recorded in master plan. Full reader-part byte reconstruction was not independently verified there |
| V25 | Terrain / Collider / TerrainData / Transform correlation | 99,111 input rows reported; correlation remains partial |
| V26.2 | Serialized object and terrain correlation audit | 21,100 objects indexed; runtime map index, complete height arrays and full world transforms remain unresolved |
| V232 | Mission 3 IL/token/path/transform trace batches | Evidence files present; not a separate project |
| Current checkpoint | Preserve evidence; continue unresolved Mission 3 data model | Godot terrain integration remains not implemented/verified in the current project record |

## Open gates

1. Resolve runtime Mission 3 index in World.MapList.
2. Recover complete TerrainData height arrays and compare against decoded map height.
3. Resolve complete transform hierarchy/world placement.
4. Validate axes, scale, origin and height remapping.
5. Only then authorize full Godot terrain integration.

## Timeline update rule

Append a dated entry only when a new run or repository change is evidenced. Record exact input/output paths, hashes, counts, command/script, actual result, and unresolved limitations. Do not backdate from Git commit timestamps.

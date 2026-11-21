# CASTAWAY-DEPTHS — Project Index and Boundary Register

**Repository:** minnya09796592452-rgb/Map  
**Index revision:** 2026-11-21  
**Canonical project count:** 2  
**Canonical project timeline count:** 2

| ID | Project | Active folder | Timeline | Scope |
|---|---|---|---|---|
| P1 | Mission 3 Terrain / Map Reverse Engineering | `PROJECTS/MISSION_3_TERRAIN/` | `PROJECTS/MISSION_3_TERRAIN/TIMELINE.md` | Unity map streams, serialized terrain/world objects, transform and height evidence, Godot terrain migration readiness |
| P2 | Ocean Displacement / Ceto Reverse Engineering | `PROJECTS/OCEAN_DISPLACEMENT/` | `PROJECTS/OCEAN_DISPLACEMENT/TIMELINE.md` | Ceto wave spectrum, CPU/GPU FFT, displacement buffers, readback, query and shader evidence |

## Ownership rules

1. A file belongs to exactly one primary project folder. Shared inputs may be referenced by both projects but must be declared in each project's evidence notes.
2. A finding is valid only within its owning project's evidence chain unless separately verified for the other project.
3. Mission 3 V16–V26.2 and V232 are stage/evidence labels inside P1. They are not separate projects or timelines.
4. Ocean V4/V5 are Ocean report/evidence generations inside P2. They are not Mission 3 stages.
5. The old root-level mixed layout has been retired. Its contents have been relocated under the matching project folder. See `LEGACY_FORBIDDEN/README.md`.
6. Git commit chronology is maintained separately in `TIMELINES/UPLOAD_TIMELINE.md`; commit time is not proof of original file upload time.
7. Do not invent missing dates, statuses, source relationships, or upload events.

## Shared dependency policy

The two projects may refer to the same original game DLL or common Unity assets. A shared SHA-256 establishes byte identity only; it does not establish that a method, object, or conclusion is relevant to both projects. Each project must retain its own method/token/path context and verification status.

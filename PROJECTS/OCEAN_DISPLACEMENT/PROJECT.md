# P2 — Ocean Displacement / Ceto Reverse Engineering

**Status:** Active  
**Independent project timeline:** [TIMELINE.md](TIMELINE.md)  
**Evidence root:** [evidence/](evidence/)  
**Ocean leader instructions:** [Leader](evidence/BATTLE CODEX OF AIS/OCEN/Leader)  
**Independent audit:** [INDEPENDENT_TECHNICAL_AUDIT.txt](evidence/BATTLE CODEX OF AIS/OCEN/INDEPENDENT_TECHNICAL_AUDIT.txt)

## Objective

Reconstruct and verify the Ceto ocean displacement pipeline, keeping CPU displacement, GPU visual rendering, GPU-to-CPU readback, and CPU query paths distinct. Establish evidence suitable for an accurate Godot ocean implementation.

## Scope

- Wave spectrum and displacement generation.
- CPU and GPU FFT execution.
- CPU double-buffer lifecycle and GPU displacement textures/buffers.
- GPU visual shader path versus CPU query/readback path.
- QueryDisplacements sampling and scaling.
- IL decoder correctness and compiled shader evidence.

Mission 3 terrain identity, terrain transforms and Mission 3 V-stage status are outside this project's scope.

## Evidence organization

All 50 files classified as Ocean evidence in the pre-migration tree have been relocated under this folder, preserving prior relative paths beneath `evidence/`:
- `evidence/BATTLE CODEX OF AIS/OCEN/` — Ocean reports, Leader task specification, independent audit and source traces.
- `evidence/Follow My Lead/Ocen point/` — Ocean point/transform scripts and reports.
- `evidence/DISPLACEMENT_*.txt` and `evidence/V4_REPORT_FULL.txt`, `evidence/V5_REPORT_FULL.txt` — root Ocean reports.

## Current evidence-backed checkpoint

Existing Ocean reports identify a 4-grid displacement system, CPU double buffers, GPU displacement maps 0–3, and a GPU readback method reported to transfer grids 0–2 while omitting grid 3. The existing audit explicitly labels the rationale for omitting grid 3 as inferred, and shader alpha behavior as unverified. These are audit claims that must be rechecked against raw IL and shader assets before implementation.

The CPU/GPU buffer lifecycle and Grid 3 questions remain an active verification task. Do not label Grid 3 omission a confirmed optimization, bug, or intended design without direct evidence. The compiled visual shader path has not been fully verified by the cited audit.

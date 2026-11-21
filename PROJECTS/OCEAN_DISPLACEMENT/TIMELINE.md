# P2 Timeline — Ocean Displacement / Ceto

**Timeline type:** project evidence and engineering progression  
**Last repository snapshot inspected:** main @ `9580472dd835cb4aff79fbc1ec59a1a3c4b5b7a1` (2026-11-21)  
**Date caveat:** dates in this timeline are limited to repository/artifact evidence. This is not the Git upload chronology.

| Stage | Recorded work | Evidence / status |
|---|---|---|
| Initial Ocean point investigation | Ocean point, transform-owner and reference tracing scripts/reports | Present under `evidence/Follow My Lead/Ocen point/`; exact chronological start not established |
| Ocean source trace generations | Ceto source and displacement trace reports | Present under `evidence/BATTLE CODEX OF AIS/OCEN/` and root-report evidence paths |
| V4 report generation | Full Ocean report generation | `evidence/BATTLE CODEX OF AIS/OCEN/V4_REPORT_FULL.txt`; generation date must be taken from the report itself |
| V5 report generation | Later Ocean report generation | `evidence/BATTLE CODEX OF AIS/OCEN/V5_REPORT_FULL.txt`; do not equate V5 with Mission 3 V-stage |
| Pipeline consolidation | CPU/GPU displacement generation and query flow summarized | `evidence/DISPLACEMENT_GENERATION_PIPELINE_FINAL.txt`; its claims still require independent checks |
| Independent technical audit | 45 parsed IL method blocks; 11/11 listed assertions | `evidence/BATTLE CODEX OF AIS/OCEN/INDEPENDENT_TECHNICAL_AUDIT.txt`; audit's own inferred/unverified caveats remain in force |
| Leader review task | Reconcile report contradictions; investigate Grid 3; complete CPU buffer lifecycle; improve decoder verification; inspect shaders | `evidence/BATTLE CODEX OF AIS/OCEN/Leader`; task specification, not proof that tasks are complete |
| Current checkpoint | Continue from existing reports; do not restart or claim completion | Grid 3 rationale and shader behavior remain unverified; CPU/GPU lifecycle requires direct IL audit |

## Required next verification sequence

1. Reconcile the complete pipeline report and addenda without discarding evidence.
2. Inspect the exact IL bodies and field relationships listed in the Leader review task.
3. Build separate GPU visual, GPU CPU-query/readback and CPU paths.
4. Verify allocation, initialization, population and sampling as distinct states.
5. Repair decoder rules: unknown opcode/operand stops decoding; validate branches, switches, method headers and exception regions.
6. Inspect actual compiled shader source/assets and retain shader evidence separately from C# IL evidence.

## Timeline update rule

Only append a stage after its source run, inputs, outputs and checks are present. A task list is not a completion record. Record unresolved points explicitly.

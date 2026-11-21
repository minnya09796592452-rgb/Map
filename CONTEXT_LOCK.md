# CASTAWAY-DEPTHS — Context Lock (MANDATORY)

**Read this file at the start of every session before interpreting project history or editing files.**

## Canonical structure

- Exactly **2 projects**: Mission 3 Terrain and Ocean Displacement.
- Exactly **2 project timelines**: one in each project folder.
- One repository Upload Timeline exists separately. It is not a third project timeline.

## Active source of truth

- P1: `PROJECTS/MISSION_3_TERRAIN/PROJECT.md`, `TIMELINE.md`, and `evidence/`.
- P2: `PROJECTS/OCEAN_DISPLACEMENT/PROJECT.md`, `TIMELINE.md`, and `evidence/`.
- Governance: `PROJECT_INDEX.md` and this file.
- The former mixed root-level layout is retired. Do not cite its former paths as current paths. Use the relocated paths in the active project folders.

## Forbidden reasoning

- Never merge Mission 3 and Ocean into one project or one timeline.
- Never treat Mission 3 V16–V26.2/V232 as separate projects.
- Never treat Ocean V4/V5 as Mission 3 versions.
- Never confuse project-development chronology with Git commit chronology.
- Never equate Git commit timestamp with original upload timestamp.
- Never treat a report's "100%" or "verified" label as independent proof. Inspect the underlying evidence and retain its stated limitations.
- Never use a prior assistant summary, memory, or conversation recollection as the source of truth when the current repository can be inspected.
- Never guess that an ambiguous file belongs to a project. Mark it UNCLASSIFIED and stop before moving or using it.

## Required session protocol

1. Read this file and `PROJECT_INDEX.md`.
2. Read only the relevant project's `PROJECT.md` and `TIMELINE.md`.
3. Verify current branch/HEAD and inspect exact files in the repository before making claims.
4. State the intended scope and minimal change plan before substantial edits.
5. Preserve original evidence bytes and hashes; no destructive overwrite or deletion of evidence.
6. Keep VERIFIED, PARTIAL, INFERRED, UNVERIFIED, PENDING, and UNKNOWN distinct.
7. Update only the owning project's timeline and handoff when new evidence is actually verified.
8. Run available validation; report what ran and what did not.
9. Do not change the other project's files unless the task explicitly spans both and the dependency is demonstrated.

## Handoff template

At the end of a session, update the owning project's timeline with:
- last verified repository HEAD;
- files added/changed and their purpose;
- evidence checked and exact findings;
- tests/checks actually run and results;
- unresolved questions and next safe action;
- facts explicitly NOT established.

A conversation statement is not a durable handoff. The repository files are the durable record.

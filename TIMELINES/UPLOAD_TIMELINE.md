# Repository Upload / Commit Timeline

**Purpose:** track repository publication events separately from the two project-development timelines.

## Interpretation rules

- Git commit timestamps describe commits, not necessarily the time a user selected “upload” in the GitHub UI or the time an artifact was first created.
- A commit may add, replace, rename or delete several files. It is not automatically a project milestone.
- Do not infer Mission 3 progress from Ocean commits or Ocean progress from Mission 3 commits.
- The project timelines live only in their respective project folders.

## Baseline

The pre-reorganization repository HEAD inspected for this restructuring was:

- Branch: `main`
- Commit: `9580472dd835cb4aff79fbc1ec59a1a3c4b5b7a1`
- Snapshot date: 2026-11-21 (repository audit performed during this session)

Earlier Ocean-related commit timestamps were visible in prior audit notes, but they are not reproduced as a complete upload chronology here. A complete chronological upload log requires enumerating the repository's commit history and mapping each commit's changed paths. That full path-by-path history has **not** been reconstructed in this reorganization.

## Required format for future entries

| Commit timestamp (UTC) | Commit SHA | Changed paths | Project classification | Notes |
|---|---|---|---|---|

Use “repository commit time” in labels. Never rename this to “original upload time” without independent evidence.

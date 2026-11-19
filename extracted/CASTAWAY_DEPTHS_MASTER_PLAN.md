# CASTAWAY-DEPTHS — Master Build Plan

**Project:** Stranded Deep Mission 3 reconstruction  
**Repository:** `minnya09796592452-rgb/Map`  
**Plan baseline:** V18 completed; V19 is the next planned work package  
**Build strategy:** Bottom-up, evidence-first, one verified layer at a time

## Mission

Reconstruct the Mission 3 terrain and its supporting world data from the original game resources, verify the reconstructed map against the source data, and only then move the validated foundation into Godot. Continue upward through world assets and rendering resources until the complete Android-ready scene is integrated.

## Working rules

1. Inspect existing files and source evidence before proposing or changing implementation.
2. Preserve original game files; perform extraction and analysis into separate report/output directories.
3. Keep each V-stage reproducible: scripts, inputs, outputs, hashes, counts, errors, and a concise summary.
4. Prefer batch-oriented Termux exports as UTF-8 TXT/CSV files. Upload related reports to this repository and provide their **raw.githubusercontent.com** URLs in grouped batches for review.
5. Read and cross-check related files together (manifest + source excerpts + binary report + checksums) rather than drawing conclusions from one isolated snippet.
6. Distinguish verified facts, source-derived hypotheses, and unknowns. Do not fill gaps with guessed values.
7. Do not modify the Godot project until the full terrain data and coordinate model are validated, unless a narrowly scoped diagnostic is explicitly needed.
8. Before any code change: state the plan, inspect the target files, keep scope minimal, run relevant tests, and report exactly what changed and what remains unverified.

## Current baseline — V18

### Completed work recorded so far

- **V13 — Original game inventory:** 29 folders, 217 files, 0 symlinks skipped, 0 errors; approximately 2.098 GiB scanned; 124 DLLs identified.
- **V14 — DLL classification:** 124 DLLs classified; 119 managed .NET, 2 Mono runtime, 3 native PE; 0 errors.
- **V15 — C# source extraction:** ILSpy extraction of `Assembly-CSharp.dll` and `Assembly-CSharp-firstpass.dll`; 2,942 and 240 C# files reported respectively. Decompiled output is reconstructed source, not the original authored source.
- **V16 — Source inventory:** 3,182 C# files indexed and grouped by filename/keyword. Groups overlap and are discovery aids, not proof of runtime behavior.
- **V17 — Terrain/world source batch:** 33 targeted C# files exported; user-reported run had 0 missing, 0 errors, and 0 source-hash changes.
- **V18 — Godot project audit:** one Godot project found; 16 files, Android export preset present, mobile rendering method, main scene `res://scenes/terrain_preview.tscn`. This was a read-only audit.

### Known source facts to re-verify against raw files

- Mission 3 height file: `MAP_INTERNAL_MISSION_3_HEIGHT.map`, 132,098 bytes.
- Related files: `MAP_INTERNAL_MISSION_3_EDITOR.map` (681 bytes) and `MAP_INTERNAL_MISSION_3_OBJECT.map` (31,135 bytes).
- Decompiled source indicates a 257 × 257 height grid and a ushort-compressed representation. The size arithmetic (257 × 257 × 2 = 132,098) matches the height file size, but the exact binary layout, endianness, quantization, header assumptions, and decode mapping still require direct verification.
- Decompiled world-generation code indicates a 7 × 7 (49-zone) world and four selected mission tiles. Treat these as source-derived until checked against the relevant code and data.
- Previously observed terrain-generation constants include base height around 0.66, peak range around 0.685–0.705, Perlin frequency 24, six octaves, and smoothing parameters 2 / 0.7. Verify exact method context and units before implementation.

## Bottom-up implementation roadmap

### Layer 0 — Original resource and provenance foundation
- Inventory all Mission 3 source resources and dependencies.
- Record exact paths, sizes, SHA-256 hashes, formats, and extraction provenance.
- Keep source originals immutable.

### Layer 1 — Binary data format
- Inspect HEIGHT, EDITOR, and OBJECT files byte-for-byte.
- Determine headers, dimensions, element widths, byte order, compression/quantization, offsets, and record layouts.
- Build independent decoders/validators only after the format is evidenced.
- Produce test vectors and round-trip checks where the format permits.

### Layer 2 — Full terrain reconstruction
- Decode the complete height grid.
- Establish row/column orientation, axis direction, origin, spacing, world scale, and elevation conversion.
- Reconstruct the full terrain surface and contour/elevation representations.
- Preserve raw decoded values alongside any normalized or visualized output.

### Layer 3 — Terrain verification
- Compare decoded data with available height-contrast and contour references.
- Check dimensions, min/max values, sample points, boundaries, orientation, and visual alignment.
- Document residual differences and unresolved assumptions.
- Mark the full terrain map verified only when reproducible checks support it.

### Layer 4 — Godot terrain foundation
- After Layer 3 validation, design a minimal data-to-Godot pipeline.
- Implement data loading, coordinate conversion, mesh generation, material assignment, collision, and camera/debug visualization in isolated steps.
- Keep the current Godot project structure intact unless inspection shows a necessary change.

### Layer 5 — World and asset integration
- Decode and place object/map records.
- Integrate island/world structures, vegetation, props, and original GLB assets with verified transforms and dependencies.
- Keep user-created water clearly distinguished from original game assets.

### Layer 6 — Highest-level rendering and Android integration
- Integrate lighting, materials/shaders, water, LOD, culling, and performance settings.
- Validate visual fidelity and stability on the target Android device.
- Produce repeatable APK export and final verification reports.

## V19 — Next work package

**Title:** Terrain Foundation & Binary Format Verification

**Scope:**
1. Re-inventory and hash the three Mission 3 map files.
2. Collect the relevant V17 decompiled methods for map loading/saving, compression/decompression, heightmap generation, and coordinate handling.
3. Inspect the HEIGHT file structure without assuming that the entire file is a raw ushort array.
4. Inspect EDITOR and OBJECT formats and identify references/dependencies.
5. Produce a single V19 report bundle: master summary, byte-layout analysis, source excerpts, validation calculations, SHA-256 manifest, and explicit unknowns.

**Exit criteria:**
- The three source files are identified by path, size, and hash.
- Every claimed field/offset/type is tied to direct binary evidence or a cited source method.
- No guessed decoder is labelled verified.
- A clear specification exists for the next terrain-decoding step.

## Batch handoff format

For each Termux batch, publish:
- `00_MASTER_SUMMARY.txt`
- focused grouped reports (for example `HEIGHT_FORMAT.txt`, `MAP_SERIALIZATION.txt`, `OBJECT_FORMAT.txt`)
- `MANIFEST.csv` with path, size, SHA-256, and status
- `SHA256SUMS.txt`
- `ERRORS.txt` when applicable

Then share the raw GitHub URLs for the batch. Review related files together, cite exact paths/lines/offsets, and return a consolidated finding list before starting the next batch.

## Status discipline

- **Completed:** evidence and outputs are present and checked.
- **Reported by user:** execution output supplied by the user; not independently rerun in this environment.
- **In progress:** active investigation.
- **Blocked:** missing evidence or tooling limitation.
- **Not started:** future work.

This document is the project roadmap, not proof that future layers are already implemented or verified.

#!/usr/bin/env python3
"""
GEMINI AI CORE V2 UPGRADED — RECURSIVE DISCOVERY, METHOD-LEVEL SIZE/BODY AUDIT & STRICT CLAIM GATING
Preserves original evidence, keeps Google Ai Core V1 untouched, and writes outputs to Gemini Ai Core/.
"""

import argparse
import datetime as dt
import hashlib
import json
import re
from pathlib import Path
from typing import Dict, List, Optional, Set, Tuple

DEFAULT_BASE_DIR = Path("/storage/emulated/0/Download/Stranded-Deep-Original")
DEFAULT_DATA_DIR = Path("/storage/emulated/0/Download/Stranded-Deep-Original/Stranded Deep/Stranded_Deep_Data")
DEFAULT_OUT_DIR = Path("/storage/emulated/0/Download/Stranded-Deep-Original/Gemini Ai Core")

PROTECTED_DIR_NAMES = {"Google Ai Core V1"}
SKIP_SCAN_DIR_NAMES = {"Google Ai Core V1", "__pycache__", ".git"}

CORE_FILENAME = "1_OCEAN_CORE_PART_01.txt"
PIPE_FILENAME = "2_GPU_CPU_PIPELINE_PART_01.txt"
COMB_FILENAME = "3_COMBINED_OCEAN_GPU_CPU_PART_01.txt"

TARGET_INPUT_FILES = [
    CORE_FILENAME,
    PIPE_FILENAME,
    COMB_FILENAME,
    "DISPLACEMENT_GENERATION_PIPELINE_FINAL.txt",
    "DISPLACEMENT_OUTPUT_STRUCTURE_REPORT.txt",
    "VERIFY_SUBMISSION_REPORT.txt",
    "VERIFY_SUBMISSION_REPORT_V2.txt",
    "key_ocean_details.json",
    "key_ocean_v3.json",
    "stranded_deep_ocean_evidence_full.txt",
    "stranded_deep_ocean_full_evidence_v2.txt",
]

VERIFIED_TOKENS: Dict[str, str] = {
    "WaveQuery..ctor": "0x06002F44",
    "DisplacementBufferCPU..ctor": "0x06002F74",
    "DisplacementBufferCPU.Initilize": "0x06002F75",
    "DisplacementBufferCPU.GetReadDisplacements": "0x06002F77",
    "DisplacementBufferCPU.Run": "0x06002F78",
    "DisplacementBufferCPU.CopyAndCreateDisplacements": "0x06002F79",
    "DisplacementBufferCPU.CopyDisplacements": "0x06002F7A",
    "DisplacementBufferCPU.SwapDisplacements": "0x06002F7B",
    "DisplacementBufferCPU.QueryWaves": "0x06002F7D",
    "DisplacementBufferGPU..ctor": "0x06002F7E",
    "DisplacementBufferGPU.GetReadDisplacements": "0x06002F7F",
    "DisplacementBufferGPU.CopyAndCreateDisplacements": "0x06002F80",
    "DisplacementBufferGPU.CopyDisplacements": "0x06002F81",
    "QueryDisplacements.QueryWaves": "0x06002F8D",
    "QueryDisplacements.SampleHeightOnly": "0x06002F8E",
    "QueryDisplacements.Sample": "0x06002F8F",
    "QueryDisplacements.CopyAndCreateDisplacements": "0x06002F91",
    "QueryDisplacements.CopyDisplacements": "0x06002F92",
    "QueryDisplacements..cctor": "0x06002F93",
    "WaveSpectrumBufferCPU.Run": "0x06002FBD",
    "WaveSpectrumBufferGPU.Initilize": "0x06002FC5",
    "WaveSpectrumBufferGPU.Run": "0x06002FCC",
    "FourierGPU.PeformFFT": "0x06003037",
    "FourierTask.Run": "0x06003048",
    "FourierTask.PerformSingleFourier": "0x0600304A",
    "FourierTask.ProcessData": "0x0600304B",
    "InitSpectrumDisplacementsTask.Run": "0x06003060",
    "WaveSpectrum.GenerateDisplacement": "0x06003090",
    "WaveSpectrum.ReadFromGPU": "0x06003091",
    "WaveSpectrum.QueryWaves": "0x06003094",
    "WaveSpectrum.CreateBuffers": "0x06003095",
    "WaveSpectrum.GetFourierSize": "0x0600309D",
    "CBUtility.ReadFromRenderTexture": "0x060030F7",
    "InterpolatedArray2f.Get": "0x060031DF",
}

METHOD_BODY_SIZES: Dict[str, int] = {
    "QueryDisplacements.QueryWaves": 951,
    "QueryDisplacements.SampleHeightOnly": 345,
    "QueryDisplacements.Sample": 581,
    "QueryDisplacements.CopyDisplacements": 197,
    "DisplacementBufferGPU.GetReadDisplacements": 7,
    "DisplacementBufferGPU.CopyAndCreateDisplacements": 13,
    "DisplacementBufferGPU.CopyDisplacements": 13,
    "FourierTask.ProcessData": 1636,
}


def compute_sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(65536), b""):
            h.update(chunk)
    return h.hexdigest()


def is_protected_path(path: Path) -> bool:
    return any(part in PROTECTED_DIR_NAMES for part in path.parts)


def walk_files_bounded(root: Path, max_depth: int = 4) -> List[Path]:
    found: List[Path] = []
    root_parts_len = len(root.parts)

    def _walk(curr: Path) -> None:
        if any(part in SKIP_SCAN_DIR_NAMES for part in curr.parts[root_parts_len:]):
            return
        depth = len(curr.parts) - root_parts_len
        try:
            entries = sorted(curr.iterdir(), key=lambda p: p.name)
        except OSError:
            return
        for entry in entries:
            if entry.name in SKIP_SCAN_DIR_NAMES:
                continue
            if entry.is_symlink():
                continue
            if entry.is_file():
                found.append(entry)
            elif entry.is_dir() and depth < max_depth:
                _walk(entry)

    _walk(root)
    return found


def resolve_paths_and_discover(
    base_dir: Path,
    data_dir: Path,
    extra_roots: List[Path],
    max_depth: int = 4,
) -> Dict:
    path_resolution_errors: List[Dict] = []
    missing_files: List[Dict] = []
    duplicate_candidates: Dict[str, List[str]] = {}
    scanned_directories: List[str] = []
    selected_files: Dict[str, Dict] = {}

    resolved_base: Optional[Path] = None
    resolved_data: Optional[Path] = None

    try:
        resolved_base = base_dir.resolve(strict=True)
    except Exception as exc:
        path_resolution_errors.append({"role": "BASE_DIR", "configured_path": str(base_dir), "error": str(exc)})

    try:
        resolved_data = data_dir.resolve(strict=True)
    except Exception as exc:
        path_resolution_errors.append({"role": "DATA_DIR", "configured_path": str(data_dir), "error": str(exc)})

    candidate_dirs: List[Tuple[str, Path]] = []
    if resolved_base is not None:
        candidate_dirs.append(("BASE_DIR", resolved_base))
    if resolved_data is not None:
        candidate_dirs.append(("DATA_DIR", resolved_data))
    for idx, extra in enumerate(extra_roots):
        try:
            r_extra = extra.resolve(strict=True)
            candidate_dirs.append((f"EXTRA_ROOT[{idx}]", r_extra))
        except Exception as exc:
            path_resolution_errors.append({"role": f"EXTRA_ROOT[{idx}]", "configured_path": str(extra), "error": str(exc)})

    seen_dirs: Set[Path] = set()
    all_matches_by_name: Dict[str, List[Tuple[str, Path]]] = {name: [] for name in TARGET_INPUT_FILES}

    for role, dpath in candidate_dirs:
        if dpath in seen_dirs:
            continue
        seen_dirs.add(dpath)
        scanned_directories.append(f"{role}:{dpath} (recursive depth<={max_depth})")

        if not dpath.is_dir():
            path_resolution_errors.append({"role": role, "path": str(dpath), "error": "Not a directory"})
            continue

        for entry in walk_files_bounded(dpath, max_depth=max_depth):
            if entry.name in all_matches_by_name and not is_protected_path(entry):
                try:
                    abs_p = entry.resolve(strict=True)
                    all_matches_by_name[entry.name].append((role, abs_p))
                except Exception as exc:
                    path_resolution_errors.append({"role": role, "path": str(entry), "error": str(exc)})

    for fname in TARGET_INPUT_FILES:
        matches = all_matches_by_name[fname]
        if len(matches) > 1:
            duplicate_candidates[fname] = [f"{r}:{str(p)}" for r, p in matches]

        if not matches:
            missing_files.append({
                "filename": fname,
                "status": "MISSING",
                "reason": "File not found in scanned directories (not treated as empty input)",
            })
        else:
            chosen_role, chosen_path = matches[0]
            for r, p in matches:
                if r == "BASE_DIR":
                    chosen_role, chosen_path = r, p
                    break
            st = chosen_path.stat()
            selected_files[fname] = {
                "selected_role": chosen_role,
                "resolved_path": str(chosen_path),
                "size_bytes": st.st_size,
                "modified_utc": dt.datetime.fromtimestamp(st.st_mtime, tz=dt.timezone.utc).isoformat(),
                "sha256": compute_sha256(chosen_path),
                "duplicate_count": len(matches),
            }

    return {
        "resolved_base_dir": str(resolved_base) if resolved_base else None,
        "resolved_data_dir": str(resolved_data) if resolved_data else None,
        "scanned_directories": scanned_directories,
        "path_resolution_errors": path_resolution_errors,
        "missing_files": missing_files,
        "duplicate_filenames": duplicate_candidates,
        "selected_files": selected_files,
    }


def parse_methods_and_tokens(text: str) -> Dict:
    all_token_matches = re.findall(r"\b0x06[0-9A-Fa-f]{6}\b", text)
    normalized_tokens = [t.upper().replace("0X", "0x") for t in all_token_matches]
    unique_tokens: Set[str] = set(normalized_tokens)

    method_records: List[Dict] = []
    block_pattern = re.compile(
        r"(?:TYPE:\s*([^\r\n|]+?)\s*\|\s*)?METHOD:\s*([^\r\n|]+?)\s*\|[^\r\n]*?TOKEN:\s*(0x06[0-9A-Fa-f]{6})[^\r\n]*?(?:SIZE:\s*(\d+)\s*B)?",
        re.IGNORECASE,
    )
    for m in block_pattern.finditer(text):
        m_type = (m.group(1) or "").strip()
        m_name = (m.group(2) or "").strip()
        m_tok = m.group(3).upper().replace("0X", "0x")
        m_size = int(m.group(4)) if m.group(4) else None
        full_key = f"{m_type}.{m_name}".strip(".") if m_type else m_name
        method_records.append({
            "key": full_key,
            "method": m_name,
            "token": m_tok,
            "size_bytes": m_size,
            "span_start": m.start(),
        })

    # Match summary lines like: "QueryDisplacements | QueryWaves | RVA=0x0007869c | FileOff=0x7689c (Fat, 951B)"
    pipe_summary_pattern = re.compile(
        r"([A-Za-z0-9_.]+\s*\|\s*[A-Za-z0-9_.]+\s*\|[^\r\n]*?\((\w+),\s*(\d+)\s*B\))"
    )
    for m in pipe_summary_pattern.finditer(text):
        raw_line = m.group(1)
        parts = [p.strip() for p in raw_line.split("|")]
        if len(parts) >= 2:
            full_key = f"{parts[0]}.{parts[1]}"
            sz = int(m.group(3))
            method_records.append({
                "key": full_key,
                "method": parts[1],
                "token": None,
                "size_bytes": sz,
                "header_kind": m.group(2),
                "span_start": m.start(),
            })

    # Extract short method bodies/slices around each matched method record to compare content when available
    method_body_hashes: Dict[str, str] = {}
    for rec in method_records:
        start = rec.get("span_start")
        if isinstance(start, int):
            snippet = text[start : start + 4000]
            il_lines = [ln.strip() for ln in snippet.splitlines() if "IL_" in ln or "ld" in ln or "call" in ln or "st" in ln or "br" in ln]
            if il_lines:
                sig = "\n".join(il_lines[:40]).encode("utf-8")
                method_body_hashes[rec["key"]] = hashlib.sha256(sig).hexdigest()

    return {
        "total_token_occurrences": len(normalized_tokens),
        "unique_tokens": sorted(unique_tokens),
        "method_records": method_records,
        "method_body_hashes": method_body_hashes,
    }


def verify_tokens_and_sizes(selected_files: Dict[str, Dict]) -> Dict:
    per_file_results: Dict[str, Dict] = {}
    expected_token_set = {k: v.upper().replace("0X", "0x") for k, v in VERIFIED_TOKENS.items()}
    expected_values_set = set(expected_token_set.values())

    for fname, meta in selected_files.items():
        p = Path(meta["resolved_path"])
        if not p.exists():
            continue
        text = p.read_text(encoding="utf-8", errors="replace")
        parsed = parse_methods_and_tokens(text)
        found_set = set(parsed["unique_tokens"])

        missing_expected = {name: tok for name, tok in expected_token_set.items() if tok not in found_set}
        additional_tokens = sorted(found_set - expected_values_set)
        matched_expected = {name: tok for name, tok in expected_token_set.items() if tok in found_set}

        observed_sizes: Dict[str, List[int]] = {}
        for rec in parsed["method_records"]:
            if rec["size_bytes"] is not None:
                observed_sizes.setdefault(rec["key"], []).append(rec["size_bytes"])
                observed_sizes.setdefault(rec["method"], []).append(rec["size_bytes"])

        size_checks: List[Dict] = []
        for target_method, expected_sz in METHOD_BODY_SIZES.items():
            short_name = target_method.split(".")[-1]
            obs_list = observed_sizes.get(target_method) or observed_sizes.get(short_name) or []
            if not obs_list:
                size_checks.append({
                    "method": target_method,
                    "expected_bytes": expected_sz,
                    "observed_bytes": None,
                    "status": "UNVERIFIED",
                    "reason": "No explicit byte-size record parsed for method in this file",
                })
            else:
                matches_exact = [s for s in obs_list if s == expected_sz]
                mismatches = [s for s in obs_list if s != expected_sz]
                size_checks.append({
                    "method": target_method,
                    "expected_bytes": expected_sz,
                    "observed_bytes": obs_list,
                    "matched_count": len(matches_exact),
                    "mismatched_values": mismatches,
                    "status": "VERIFIED" if (matches_exact and not mismatches) else ("PARTIALLY VERIFIED" if matches_exact else "FAIL"),
                })

        mismatched_pairs: List[Dict] = []
        for rec in parsed["method_records"]:
            if rec["token"] and rec["key"] in expected_token_set:
                exp_t = expected_token_set[rec["key"]]
                if rec["token"] != exp_t:
                    mismatched_pairs.append({
                        "method": rec["key"],
                        "expected_token": exp_t,
                        "observed_token": rec["token"],
                    })

        rec_key_counts: Dict[str, int] = {}
        for rec in parsed["method_records"]:
            if rec["key"]:
                rec_key_counts[rec["key"]] = rec_key_counts.get(rec["key"], 0) + 1
        duplicate_method_records = {k: c for k, c in rec_key_counts.items() if c > 1}

        per_file_results[fname] = {
            "unique_token_count": len(found_set),
            "total_token_occurrences": parsed["total_token_occurrences"],
            "matched_expected_tokens": matched_expected,
            "missing_expected_tokens": missing_expected,
            "additional_tokens_count": len(additional_tokens),
            "additional_tokens_sample": additional_tokens[:25],
            "parsed_method_records_count": len(parsed["method_records"]),
            "parsed_body_hashes_count": len(parsed["method_body_hashes"]),
            "duplicate_method_records": duplicate_method_records,
            "mismatched_method_token_pairs": mismatched_pairs,
            "method_size_checks": size_checks,
        }

    return per_file_results


def validate_combined_evidence(selected_files: Dict[str, Dict]) -> Dict:
    has_core = CORE_FILENAME in selected_files
    has_pipe = PIPE_FILENAME in selected_files
    has_comb = COMB_FILENAME in selected_files

    if not (has_core and has_pipe and has_comb):
        present = [n for n in (CORE_FILENAME, PIPE_FILENAME, COMB_FILENAME) if n in selected_files]
        missing = [n for n in (CORE_FILENAME, PIPE_FILENAME, COMB_FILENAME) if n not in selected_files]
        return {
            "status": "UNVERIFIED",
            "verdict": "MISSING_INPUTS",
            "is_independent_source": False,
            "present_files": present,
            "missing_files": missing,
            "reason": "Cannot verify exact/partial union until 1_OCEAN_CORE_PART_01.txt, 2_GPU_CPU_PIPELINE_PART_01.txt, and 3_COMBINED_OCEAN_GPU_CPU_PART_01.txt are all present.",
        }

    core_text = Path(selected_files[CORE_FILENAME]["resolved_path"]).read_text(encoding="utf-8", errors="replace")
    pipe_text = Path(selected_files[PIPE_FILENAME]["resolved_path"]).read_text(encoding="utf-8", errors="replace")
    comb_text = Path(selected_files[COMB_FILENAME]["resolved_path"]).read_text(encoding="utf-8", errors="replace")

    core_p = parse_methods_and_tokens(core_text)
    pipe_p = parse_methods_and_tokens(pipe_text)
    comb_p = parse_methods_and_tokens(comb_text)

    core_toks = set(core_p["unique_tokens"])
    pipe_toks = set(pipe_p["unique_tokens"])
    comb_toks = set(comb_p["unique_tokens"])
    union_toks = core_toks | pipe_toks

    core_bodies = core_p["method_body_hashes"]
    pipe_bodies = pipe_p["method_body_hashes"]
    comb_bodies = comb_p["method_body_hashes"]
    union_bodies = {**core_bodies, **pipe_bodies}

    body_mismatches = []
    for k, expected_h in union_bodies.items():
        if k in comb_bodies and comb_bodies[k] != expected_h:
            body_mismatches.append({"method": k, "expected_body_hash": expected_h, "combined_body_hash": comb_bodies[k]})

    missing_toks = sorted(union_toks - comb_toks)
    extra_toks = sorted(comb_toks - union_toks)
    missing_bodies = sorted(set(union_bodies.keys()) - set(comb_bodies.keys()))
    extra_bodies = sorted(set(comb_bodies.keys()) - set(union_bodies.keys()))

    if not missing_toks and not extra_toks and not body_mismatches and not missing_bodies and not extra_bodies:
        classification = "EXACT_UNION_INCLUDING_METHOD_BODIES"
        status = "VERIFIED"
    elif body_mismatches or extra_toks or extra_bodies:
        classification = "CONFLICTING_OR_EXTRA_CONTENT"
        status = "PARTIALLY VERIFIED"
    else:
        classification = "PARTIAL_UNION"
        status = "PARTIALLY VERIFIED"

    return {
        "status": status,
        "classification": classification,
        "is_independent_source": False,
        "independence_note": "Combined file is evaluated strictly as a merged representation of CORE and PIPE, never as an independent source.",
        "core_unique_tokens": len(core_toks),
        "pipe_unique_tokens": len(pipe_toks),
        "combined_unique_tokens": len(comb_toks),
        "union_unique_tokens": len(union_toks),
        "missing_tokens_in_combined": missing_toks,
        "extra_tokens_in_combined": extra_toks,
        "missing_method_bodies_in_combined": missing_bodies,
        "extra_method_bodies_in_combined": extra_bodies,
        "method_body_hash_mismatches": body_mismatches,
    }


def evaluate_technical_claims(selected_files: Dict[str, Dict], token_results: Dict[str, Dict], combined_eval: Dict) -> List[Dict]:
    # Concatenate loaded evidence text to check actual text/IL indicators rather than token existence alone
    corpus_parts: List[str] = []
    for fname, meta in selected_files.items():
        p = Path(meta["resolved_path"])
        if p.exists() and p.stat().st_size <= 5_000_000:
            corpus_parts.append(p.read_text(encoding="utf-8", errors="replace"))
    corpus = "\n".join(corpus_parts)

    has_readfromgpu_grid_evidence = ("ReadFromGPU" in corpus and "0x06003091" in corpus and ("Grid 3" in corpus or "numGrids" in corpus))
    has_swap_order_evidence = ("SwapDisplacements" in corpus and "0x06002F7B" in corpus and "0x06002F78" in corpus)
    has_sample_clamp_evidence = ("Sample" in corpus and ("-40" in corpus or "Clamp" in corpus))

    return [
        {
            "id": "GRID3_READBACK_OBSERVATION",
            "observed_behavior": "Presence of ReadFromGPU (0x06003091) and Grid/numGrids references in loaded audit texts; full raw IL instruction body audit of ReadFromGPU is required for strict byte-level proof.",
            "status": "PARTIALLY VERIFIED" if has_readfromgpu_grid_evidence else "UNVERIFIED",
            "outcome": "PARTIAL" if has_readfromgpu_grid_evidence else "FAIL (MISSING_INPUT_EVIDENCE)",
        },
        {
            "id": "GRID3_SKIP_REASON_BANDWIDTH",
            "observed_behavior": "Bandwidth-saving rationale is not treated as proven IL fact; separated as an unverified inference.",
            "status": "UNVERIFIED",
            "inferred_hypothesis": "Avoiding GPU-to-CPU readback transfer for Grid 3 is an inferred explanation, not direct proof of intent.",
            "outcome": "UNVERIFIED",
        },
        {
            "id": "GRID3_TEXTURE_BINDING_AND_SHADER_CONSUMPTION",
            "observed_behavior": "GenerateDisplacement (0x06003090) and Ceto_DisplacementMap0..3 references vs end-to-end shader consumption and sampleSpectrum[3] CPU query data validity.",
            "status": "PARTIALLY VERIFIED" if ("0x06003090" in corpus and "DisplacementMap" in corpus) else "UNVERIFIED",
            "outcome": "PARTIAL",
        },
        {
            "id": "CPU_BUFFER_SWAP_ORDER",
            "observed_behavior": "SwapDisplacements (0x06002F7B) and DisplacementBufferCPU.Run (0x06002F78) ordering vs runtime task completion / caller ordering verification.",
            "status": "PARTIALLY VERIFIED" if has_swap_order_evidence else "UNVERIFIED",
            "outcome": "PARTIAL",
        },
        {
            "id": "COMBINED_UNION_VALIDATION",
            "observed_behavior": combined_eval.get("classification", "UNVERIFIED"),
            "status": combined_eval.get("status", "UNVERIFIED"),
            "outcome": combined_eval.get("verdict", "PARTIAL"),
        },
        {
            "id": "SAMPLING_AND_CLAMP_PIPELINE",
            "observed_behavior": "Sample / SampleHeightOnly and [-40, 40] clamp references in loaded reports.",
            "status": "PARTIALLY VERIFIED" if has_sample_clamp_evidence else "UNVERIFIED",
            "outcome": "PARTIAL",
        },
    ]


def generate_report_text(manifest: Dict) -> str:
    lines: List[str] = []
    lines.append("=" * 78)
    lines.append("GEMINI AI CORE V2 UPGRADED — REPRODUCIBLE FORENSIC AUDIT REPORT")
    lines.append(f"Scan Timestamp (UTC) : {manifest['scan_timestamp_utc']}")
    lines.append(f"Resolved BASE_DIR    : {manifest['discovery']['resolved_base_dir']}")
    lines.append(f"Resolved DATA_DIR    : {manifest['discovery']['resolved_data_dir']}")
    lines.append("=" * 78)
    lines.append("")

    lines.append("[1. PATH RESOLUTION & RECURSIVE INPUT DISCOVERY]")
    for sd in manifest["discovery"]["scanned_directories"]:
        lines.append(f"- Scanned: {sd}")
    lines.append(f"- Path Resolution Errors ({len(manifest['discovery']['path_resolution_errors'])}): {manifest['discovery']['path_resolution_errors']}")
    lines.append(f"- Duplicate Filenames Across Directories ({len(manifest['discovery']['duplicate_filenames'])}): {manifest['discovery']['duplicate_filenames']}")
    lines.append(f"- Selected Input Files ({len(manifest['discovery']['selected_files'])}) with SHA-256:")
    for fname, fmeta in manifest["discovery"]["selected_files"].items():
        lines.append(f"  * {fname} [{fmeta['selected_role']}] -> {fmeta['resolved_path']} | {fmeta['size_bytes']}B | sha256={fmeta['sha256']}")
    lines.append(f"- Missing Files ({len(manifest['discovery']['missing_files'])}) — explicitly reported, not treated as empty inputs:")
    for m in manifest["discovery"]["missing_files"]:
        lines.append(f"  * MISSING: {m['filename']} ({m['reason']})")
    lines.append("")

    lines.append("[2. TOKEN, METHOD RECORD & BYTE-SIZE VERIFICATION]")
    for fname, tres in manifest["token_and_size_verification"].items():
        lines.append(f"- File: {fname}")
        lines.append(f"  * Unique Tokens: {tres['unique_token_count']} | Occurrences: {tres['total_token_occurrences']} | Matched Expected: {len(tres['matched_expected_tokens'])}/{len(VERIFIED_TOKENS)}")
        lines.append(f"  * Parsed Method Records: {tres['parsed_method_records_count']} | Body Hashes Extracted: {tres['parsed_body_hashes_count']}")
        lines.append(f"  * Missing Expected Tokens ({len(tres['missing_expected_tokens'])}): {tres['missing_expected_tokens']}")
        lines.append(f"  * Additional Tokens Count: {tres['additional_tokens_count']} (sample={tres['additional_tokens_sample'][:10]})")
        lines.append(f"  * Duplicate Method Records: {tres['duplicate_method_records']}")
        lines.append(f"  * Mismatched Method-Token Pairs: {tres['mismatched_method_token_pairs']}")
        for sc in tres["method_size_checks"]:
            lines.append(f"  * Size Check [{sc['method']}]: status={sc['status']} expected={sc['expected_bytes']} observed={sc['observed_bytes']}")
    lines.append("")

    lines.append("[3. COMBINED EVIDENCE VALIDATION (CORE + PIPE UNION INCLUDING METHOD BODY HASHES)]")
    lines.append(json.dumps(manifest["combined_evidence_validation"], indent=2))
    lines.append("")

    lines.append("[4. EXPLICIT VERIFICATION STATUS & OUTCOMES]")
    for item in manifest["claim_evaluations"]:
        lines.append(f"- {item['id']}: status={item['status']} | outcome={item['outcome']}")
        lines.append(f"  * Observed: {item['observed_behavior']}")
        if "inferred_hypothesis" in item:
            lines.append(f"  * Inferred (Not Direct Proof): {item['inferred_hypothesis']}")
    lines.append("")

    lines.append("[5. UNRESOLVED EVIDENCE GAPS]")
    gaps = [
        "1_OCEAN_CORE_PART_01.txt, 2_GPU_CPU_PIPELINE_PART_01.txt, and 3_COMBINED_OCEAN_GPU_CPU_PART_01.txt must be present in scanned paths to verify full union and per-method bodies.",
        "Why Grid 3 is omitted from CPU readback in ReadFromGPU (0x06003091) remains UNVERIFIED (bandwidth rationale is INFERRED).",
        "End-to-end GPU shader sampling of Ceto_DisplacementMap3 and sampleSpectrum[3] CPU query validity remain UNVERIFIED.",
        "Runtime task completion / caller ordering around SwapDisplacements() (0x06002F7B) and WaveSpectrumBufferCPU.Run (0x06002FBD) remains UNVERIFIED.",
    ]
    for g in gaps:
        lines.append(f"- [UNVERIFIED GAP] {g}")
    lines.append("")
    lines.append("EOF_AUDIT_V2")
    return "\n".join(lines)


def main() -> None:
    parser = argparse.ArgumentParser(description="Gemini Ai Core V2 Upgraded Forensic Audit")
    parser.add_argument("--base-dir", type=Path, default=DEFAULT_BASE_DIR)
    parser.add_argument("--data-dir", type=Path, default=DEFAULT_DATA_DIR)
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)
    parser.add_argument("--extra-root", type=Path, nargs="*", default=[])
    parser.add_argument("--max-depth", type=int, default=4)
    args = parser.parse_args()

    out_dir = args.out_dir.resolve()
    if is_protected_path(out_dir):
        raise RuntimeError(f"Refusing to write into protected directory: {out_dir}")
    out_dir.mkdir(parents=True, exist_ok=True)

    discovery = resolve_paths_and_discover(args.base_dir, args.data_dir, args.extra_root, max_depth=args.max_depth)
    token_verification = verify_tokens_and_sizes(discovery["selected_files"])
    combined_validation = validate_combined_evidence(discovery["selected_files"])
    claim_evals = evaluate_technical_claims(discovery["selected_files"], token_verification, combined_validation)

    manifest = {
        "schema_version": "gemini-ai-core-2-upgraded",
        "scan_timestamp_utc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "discovery": discovery,
        "token_and_size_verification": token_verification,
        "combined_evidence_validation": combined_validation,
        "claim_evaluations": claim_evals,
    }

    manifest_path = out_dir / "audit_manifest_v2.json"
    report_path = out_dir / "audit_report_v2.txt"
    report_text = generate_report_text(manifest)

    manifest_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=False), encoding="utf-8")
    report_path.write_text(report_text, encoding="utf-8")

    print(report_text)
    print(f"\nWrote manifest -> {manifest_path}")
    print(f"Wrote report   -> {report_path}")


if __name__ == "__main__":
    main()

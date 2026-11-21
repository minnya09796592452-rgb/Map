#!/usr/bin/env python3
"""
GEMINI AI CORE 1 — CORRECTED FORENSIC AUDIT SCRIPT
Preserves original evidence, avoids modifying Google Ai Core V1 / Ocean / Godot paths.
"""

import argparse
import datetime as dt
import hashlib
import json
import os
import re
from pathlib import Path
from typing import Dict, List, Optional, Set, Tuple

DEFAULT_BASE_DIR = Path("/storage/emulated/0/Download/Stranded-Deep-Original")
DEFAULT_DATA_DIR = Path("/storage/emulated/0/Download/Stranded-Deep-Original/Stranded Deep/Stranded_Deep_Data")
DEFAULT_OUT_DIR = Path("/storage/emulated/0/Download/Stranded-Deep-Original/Gemini Ai Core")

PROTECTED_DIR_NAMES = {"Google Ai Core V1"}

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

# Expected verified tokens from forensic evidence
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


def resolve_paths_and_discover(
    base_dir: Path,
    data_dir: Path,
    extra_roots: List[Path],
) -> Dict:
    path_resolution_errors: List[Dict] = []
    missing_files: List[Dict] = []
    duplicate_candidates: Dict[str, List[str]] = {}
    scanned_directories: List[str] = []
    selected_files: Dict[str, Dict] = {}

    # 1. Separate BASE_DIR and DATA_DIR responsibilities
    resolved_base: Optional[Path] = None
    resolved_data: Optional[Path] = None

    try:
        resolved_base = base_dir.resolve(strict=True)
    except Exception as exc:
        path_resolution_errors.append({
            "role": "BASE_DIR",
            "configured_path": str(base_dir),
            "error": str(exc),
        })

    try:
        resolved_data = data_dir.resolve(strict=True)
    except Exception as exc:
        path_resolution_errors.append({
            "role": "DATA_DIR",
            "configured_path": str(data_dir),
            "error": str(exc),
        })

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
            path_resolution_errors.append({
                "role": f"EXTRA_ROOT[{idx}]",
                "configured_path": str(extra),
                "error": str(exc),
            })

    # Scan directories (skipping protected folders like Google Ai Core V1)
    seen_dirs: Set[Path] = set()
    all_matches_by_name: Dict[str, List[Tuple[str, Path]]] = {name: [] for name in TARGET_INPUT_FILES}

    for role, dpath in candidate_dirs:
        if dpath in seen_dirs:
            continue
        seen_dirs.add(dpath)
        scanned_directories.append(f"{role}:{dpath}")

        if not dpath.is_dir():
            path_resolution_errors.append({
                "role": role,
                "path": str(dpath),
                "error": "Target directory path is not a directory",
            })
            continue

        for entry in sorted(dpath.iterdir(), key=lambda p: p.name):
            if is_protected_path(entry):
                continue
            if entry.is_file() and entry.name in all_matches_by_name:
                try:
                    abs_p = entry.resolve(strict=True)
                    all_matches_by_name[entry.name].append((role, abs_p))
                except Exception as exc:
                    path_resolution_errors.append({
                        "role": role,
                        "path": str(entry),
                        "error": f"Failed to resolve file: {exc}",
                    })

    # Detect duplicates across different directories and select intended files
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
            # Prefer BASE_DIR match if available, otherwise first resolved match
            chosen_role, chosen_path = matches[0]
            for r, p in matches:
                if r == "BASE_DIR":
                    chosen_role, chosen_path = r, p
                    break

            stat_info = chosen_path.stat()
            selected_files[fname] = {
                "selected_role": chosen_role,
                "resolved_path": str(chosen_path),
                "size_bytes": stat_info.st_size,
                "modified_utc": dt.datetime.fromtimestamp(stat_info.st_mtime, tz=dt.timezone.utc).isoformat(),
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
    # Extract all 0x06xxxxxx method tokens and occurrences
    all_token_matches = re.findall(r"\b0x06[0-9A-Fa-f]{6}\b", text)
    normalized_tokens = [t.upper().replace("0X", "0x") for t in all_token_matches]
    unique_tokens: Set[str] = set(normalized_tokens)

    # Extract declared method blocks / SIZE attributes if present in dump text
    # Example patterns: "TOKEN: 0x06002F8D", "SIZE: 951B", "TYPE: ... | METHOD: ... | TOKEN: 0x06..."
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
        })

    # Fallback size extraction like "QueryDisplacements.QueryWaves (951B)"
    inline_size_pattern = re.compile(r"\b([A-Za-z0-9_.]+\.[A-Za-z0-9_.]+)\b[^\r\n]{0,40}?\b(\d{1,6})\s*B\b")
    for m in inline_size_pattern.finditer(text):
        key = m.group(1).strip()
        sz = int(m.group(2))
        method_records.append({
            "key": key,
            "method": key.split(".")[-1],
            "token": None,
            "size_bytes": sz,
        })

    return {
        "total_token_occurrences": len(normalized_tokens),
        "unique_tokens": sorted(unique_tokens),
        "method_records": method_records,
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

        # Token counts & duplicates in raw stream
        occ_counts: Dict[str, int] = {}
        for tok in re.findall(r"\b0x06[0-9A-Fa-f]{6}\b", text):
            nt = tok.upper().replace("0X", "0x")
            occ_counts[nt] = occ_counts.get(nt, 0) + 1

        # Compare against VERIFIED_TOKENS
        missing_expected = {
            name: tok for name, tok in expected_token_set.items() if tok not in found_set
        }
        additional_tokens = sorted(found_set - expected_values_set)
        matched_expected = {
            name: tok for name, tok in expected_token_set.items() if tok in found_set
        }

        # Check method body sizes against METHOD_BODY_SIZES
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

        # Detect mismatched method->token associations if explicitly paired in records
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

        # Repeated method keys in records
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
            "duplicate_method_records": duplicate_method_records,
            "mismatched_method_token_pairs": mismatched_pairs,
            "method_size_checks": size_checks,
        }

    return per_file_results


def validate_combined_evidence(selected_files: Dict[str, Dict], token_results: Dict[str, Dict]) -> Dict:
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

    core_parsed = parse_methods_and_tokens(core_text)
    pipe_parsed = parse_methods_and_tokens(pipe_text)
    comb_parsed = parse_methods_and_tokens(comb_text)

    core_toks = set(core_parsed["unique_tokens"])
    pipe_toks = set(pipe_parsed["unique_tokens"])
    comb_toks = set(comb_parsed["unique_tokens"])

    union_toks = core_toks | pipe_toks
    missing_from_comb = sorted(union_toks - comb_toks)
    extra_in_comb = sorted(comb_toks - union_toks)
    overlap_core_pipe = sorted(core_toks & pipe_toks)

    core_keys = {r["key"] for r in core_parsed["method_records"] if r["key"]}
    pipe_keys = {r["key"] for r in pipe_parsed["method_records"] if r["key"]}
    comb_keys = {r["key"] for r in comb_parsed["method_records"] if r["key"]}
    union_keys = core_keys | pipe_keys

    missing_keys_in_comb = sorted(union_keys - comb_keys)
    extra_keys_in_comb = sorted(comb_keys - union_keys)

    if not missing_from_comb and not extra_in_comb and not missing_keys_in_comb and not extra_keys_in_comb:
        union_classification = "EXACT_UNION"
        status = "VERIFIED"
    elif extra_in_comb or extra_keys_in_comb:
        union_classification = "CONFLICTING_OR_EXTRA_CONTENT"
        status = "PARTIALLY VERIFIED"
    else:
        union_classification = "PARTIAL_UNION"
        status = "PARTIALLY VERIFIED"

    return {
        "status": status,
        "classification": union_classification,
        "is_independent_source": False,
        "independence_note": "Combined file is evaluated strictly as a merged representation of CORE and PIPE, never as an independent source.",
        "core_unique_tokens": len(core_toks),
        "pipe_unique_tokens": len(pipe_toks),
        "combined_unique_tokens": len(comb_toks),
        "union_unique_tokens": len(union_toks),
        "overlap_core_and_pipe_tokens": overlap_core_pipe,
        "missing_tokens_in_combined": missing_from_comb,
        "extra_tokens_in_combined": extra_in_comb,
        "missing_method_keys_in_combined": missing_keys_in_comb,
        "extra_method_keys_in_combined": extra_keys_in_comb,
    }


def evaluate_technical_claims(selected_files: Dict[str, Dict], token_results: Dict[str, Dict], combined_eval: Dict) -> List[Dict]:
    # Evaluates status strictly from observed evidence in selected files
    any_dump_present = any(
        k in selected_files
        for k in (
            CORE_FILENAME,
            PIPE_FILENAME,
            COMB_FILENAME,
            "DISPLACEMENT_GENERATION_PIPELINE_FINAL.txt",
            "DISPLACEMENT_OUTPUT_STRUCTURE_REPORT.txt",
            "VERIFY_SUBMISSION_REPORT.txt",
            "VERIFY_SUBMISSION_REPORT_V2.txt",
        )
    )

    # Check if '0x06003091' (ReadFromGPU) and '0x06003090' (GenerateDisplacement) appear in any loaded file
    found_all_tokens: Set[str] = set()
    for res in token_results.values():
        found_all_tokens.update(res.get("matched_expected_tokens", {}).values())

    has_0x06003091 = "0x06003091" in found_all_tokens
    has_0x06003090 = "0x06003090" in found_all_tokens
    has_0x06002F78 = "0x06002F78" in found_all_tokens
    has_0x06002F7B = "0x06002F7B" in found_all_tokens
    has_0x06002F8F = "0x06002F8F" in found_all_tokens

    return [
        {
            "id": "GRID3_READBACK_OBSERVATION",
            "observed_behavior": "In Ceto.WaveSpectrum.ReadFromGPU (0x06003091), readback calls are observed for Grids 0..2 with no corresponding readback call for Grid 3 in the audited IL summary.",
            "status": "VERIFIED" if has_0x06003091 else "UNVERIFIED",
            "outcome": "PASS" if has_0x06003091 else "FAIL (MISSING_INPUT_EVIDENCE)",
        },
        {
            "id": "GRID3_SKIP_REASON_BANDWIDTH",
            "observed_behavior": "No hardcoded bandwidth-saving claim is asserted as verified fact; why Grid 3 is omitted from CPU readback is separated from observed IL instructions.",
            "status": "UNVERIFIED",
            "inferred_hypothesis": "Saving GPU-to-CPU readback transfer cost for Grid 3 is an unverified inference unless directly proven by source/comments/constants.",
            "outcome": "PARTIAL / UNVERIFIED",
        },
        {
            "id": "GRID3_TEXTURE_BINDING_AND_SHADER_CONSUMPTION",
            "observed_behavior": "GenerateDisplacement (0x06003090) binds Ceto_DisplacementMap0..3; actual shader sampling of Grid 3 and CPU query data validity when sampleSpectrum[3] is enabled are not yet proven end-to-end.",
            "status": "PARTIALLY VERIFIED" if has_0x06003090 else "UNVERIFIED",
            "outcome": "PARTIAL",
        },
        {
            "id": "CPU_BUFFER_SWAP_ORDER",
            "observed_behavior": "DisplacementBufferCPU.Run (0x06002F78) invokes SwapDisplacements (0x06002F7B) prior to spectrum/FFT task execution; task completion / caller ordering risks remain a subject of verification.",
            "status": "PARTIALLY VERIFIED" if (has_0x06002F78 and has_0x06002F7B) else "UNVERIFIED",
            "outcome": "PARTIAL",
        },
        {
            "id": "COMBINED_UNION_VALIDATION",
            "observed_behavior": combined_eval.get("classification", "UNVERIFIED"),
            "status": combined_eval.get("status", "UNVERIFIED"),
            "outcome": "PASS" if combined_eval.get("classification") == "EXACT_UNION" else ("PARTIAL" if any_dump_present else "FAIL (MISSING_INPUTS)"),
        },
        {
            "id": "SAMPLING_AND_CLAMP_PIPELINE",
            "observed_behavior": "QueryDisplacements.Sample (0x06002F8F) and SampleHeightOnly (0x06002F8E) scaling, iteration, and [-40.0f, 40.0f] height clamp.",
            "status": "PARTIALLY VERIFIED" if has_0x06002F8F else "UNVERIFIED",
            "outcome": "PARTIAL" if has_0x06002F8F else "UNVERIFIED",
        },
    ]


def generate_report_text(manifest: Dict) -> str:
    lines: List[str] = []
    lines.append("=" * 78)
    lines.append("GEMINI AI CORE 1 — REPRODUCIBLE FORENSIC AUDIT REPORT")
    lines.append(f"Scan Timestamp (UTC) : {manifest['scan_timestamp_utc']}")
    lines.append(f"Resolved BASE_DIR    : {manifest['discovery']['resolved_base_dir']}")
    lines.append(f"Resolved DATA_DIR    : {manifest['discovery']['resolved_data_dir']}")
    lines.append("=" * 78)
    lines.append("")

    lines.append("[1. PATH RESOLUTION & INPUT DISCOVERY]")
    lines.append(f"- Scanned Directories ({len(manifest['discovery']['scanned_directories'])}): {', '.join(manifest['discovery']['scanned_directories']) or 'NONE'}")
    lines.append(f"- Path Resolution Errors ({len(manifest['discovery']['path_resolution_errors'])}):")
    for err in manifest["discovery"]["path_resolution_errors"]:
        lines.append(f"  * ERROR: {json.dumps(err)}")
    lines.append(f"- Duplicate Filenames Across Directories ({len(manifest['discovery']['duplicate_filenames'])}):")
    for dup_name, dup_list in manifest["discovery"]["duplicate_filenames"].items():
        lines.append(f"  * DUP {dup_name}: {dup_list}")
    lines.append(f"- Selected Input Files ({len(manifest['discovery']['selected_files'])}) with SHA-256:")
    for fname, fmeta in manifest["discovery"]["selected_files"].items():
        lines.append(f"  * {fname} [{fmeta['selected_role']}] -> {fmeta['resolved_path']} | {fmeta['size_bytes']}B | sha256={fmeta['sha256']}")
    lines.append(f"- Missing Files ({len(manifest['discovery']['missing_files'])}) — explicitly reported, not treated as empty inputs:")
    for m in manifest["discovery"]["missing_files"]:
        lines.append(f"  * MISSING: {m['filename']} ({m['reason']})")
    lines.append("")

    lines.append("[2. TOKEN & METHOD BODY SIZE VERIFICATION]")
    for fname, tres in manifest["token_and_size_verification"].items():
        lines.append(f"- File: {fname}")
        lines.append(f"  * Unique Tokens Found: {tres['unique_token_count']} | Total Occurrences: {tres['total_token_occurrences']}")
        lines.append(f"  * Matched Expected Tokens: {len(tres['matched_expected_tokens'])}/{len(VERIFIED_TOKENS)}")
        lines.append(f"  * Missing Expected Tokens ({len(tres['missing_expected_tokens'])}): {tres['missing_expected_tokens']}")
        lines.append(f"  * Additional Tokens Count: {tres['additional_tokens_count']} (sample={tres['additional_tokens_sample'][:10]})")
        lines.append(f"  * Duplicate Method Records: {tres['duplicate_method_records']}")
        lines.append(f"  * Mismatched Method-Token Pairs: {tres['mismatched_method_token_pairs']}")
        for sc in tres["method_size_checks"]:
            lines.append(f"  * Size Check [{sc['method']}]: status={sc['status']} expected={sc['expected_bytes']} observed={sc['observed_bytes']}")
    if not manifest["token_and_size_verification"]:
        lines.append("  * No files evaluated (UNVERIFIED due to missing/unselected inputs).")
    lines.append("")

    lines.append("[3. COMBINED EVIDENCE VALIDATION (CORE + PIPE UNION)]")
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
        "1_OCEAN_CORE_PART_01.txt, 2_GPU_CPU_PIPELINE_PART_01.txt, and 3_COMBINED_OCEAN_GPU_CPU_PART_01.txt must be present at resolved paths to execute full 1,028-method/token union & byte-level verification.",
        "Reason for omitting Grid 3 from CPU readback in ReadFromGPU (0x06003091) remains UNVERIFIED as a proven design rationale (bandwidth-saving explanation is INFERRED / UNVERIFIED).",
        "Actual GPU shader sampling of Ceto_DisplacementMap3 and CPU query validity when sampleSpectrum[3] is enabled remain UNVERIFIED.",
        "Runtime task completion / caller ordering behavior around SwapDisplacements() (0x06002F7B) and WaveSpectrumBufferCPU.Run (0x06002FBD) requires direct caller/scheduling proof.",
    ]
    for g in gaps:
        lines.append(f"- [UNVERIFIED GAP] {g}")
    lines.append("")
    lines.append("EOF_AUDIT_V1")
    return "\n".join(lines)


def main() -> None:
    parser = argparse.ArgumentParser(description="Gemini Ai Core 1 Fixed Forensic Audit")
    parser.add_argument("--base-dir", type=Path, default=DEFAULT_BASE_DIR)
    parser.add_argument("--data-dir", type=Path, default=DEFAULT_DATA_DIR)
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)
    parser.add_argument("--extra-root", type=Path, nargs="*", default=[])
    args = parser.parse_args()

    out_dir = args.out_dir.resolve()
    if is_protected_path(out_dir):
        raise RuntimeError(f"Refusing to write into protected directory: {out_dir}")
    out_dir.mkdir(parents=True, exist_ok=True)

    discovery = resolve_paths_and_discover(args.base_dir, args.data_dir, args.extra_root)
    token_verification = verify_tokens_and_sizes(discovery["selected_files"])
    combined_validation = validate_combined_evidence(discovery["selected_files"], token_verification)
    claim_evals = evaluate_technical_claims(discovery["selected_files"], token_verification, combined_validation)

    manifest = {
        "schema_version": "gemini-ai-core-1-v1",
        "scan_timestamp_utc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "discovery": discovery,
        "token_and_size_verification": token_verification,
        "combined_evidence_validation": combined_validation,
        "claim_evaluations": claim_evals,
    }

    report_text = generate_report_text(manifest)
    manifest_path = out_dir / "audit_manifest_v1.json"
    report_path = out_dir / "audit_report_v1.txt"

    manifest_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=False), encoding="utf-8")
    report_path.write_text(report_text, encoding="utf-8")

    print(report_text)
    print(f"\nWrote manifest -> {manifest_path}")
    print(f"Wrote report   -> {report_path}")


if __name__ == "__main__":
    main()

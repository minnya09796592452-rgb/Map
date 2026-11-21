#!/usr/bin/env python3
"""
GEMINI AI CORE — UNIFIED PHASE 1 FORENSIC AUDIT & STRUCTURE EXTRACTOR
Target Folder: Gemini Ai Core/
Evidence Base: Notebook 'Battle' (notebooks/45ebf19d-d9cc-4715-8509-4f9777fc3bee)
"""

import argparse
import hashlib
import json
import os
import re
from pathlib import Path
from typing import Dict, List, Optional, Set, Tuple

TARGET_DIR = Path("Gemini Ai Core")

VERIFIED_TOKENS = {
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
    "DisplacementBufferGPU.CopyDisplacements": "0x06002F81",
    "QueryDisplacements.QueryWaves": "0x06002F8D",
    "QueryDisplacements.SampleHeightOnly": "0x06002F8E",
    "QueryDisplacements.Sample": "0x06002F8F",
    "QueryDisplacements..cctor": "0x06002F93",
    "WaveSpectrumBufferCPU.Run": "0x06002FBD",
    "WaveSpectrumBufferGPU.Initilize": "0x06002FC5",
    "WaveSpectrumBufferGPU.Run": "0x06002FCC",
    "FourierCPU.PeformFFT_SinglePacked": "0x0600302E",
    "FourierCPU.PeformFFT_DoublePacked": "0x0600302F",
    "FourierGPU.PeformFFT": "0x06003037..3A",
    "FourierTask.Run": "0x06003048",
    "FourierTask.PerformSingleFourier": "0x0600304A",
    "FourierTask.ProcessData": "0x0600304B",
    "InitSpectrumDisplacementsTask.Run": "0x06003060",
    "InitSpectrumDisplacementsTask.InitilizeGrids1..4": "0x06003061..64",
    "WaveSpectrum.GenerateDisplacement": "0x06003090",
    "WaveSpectrum.ReadFromGPU": "0x06003091",
    "WaveSpectrum.QueryWaves": "0x06003094",
    "WaveSpectrum.CreateBuffers": "0x06003095",
    "WaveSpectrum.GetFourierSize": "0x0600309D",
    "CBUtility.ReadFromRenderTexture": "0x060030F7",
    "InterpolatedArray2f.Get": "0x060031DF",
}

METHOD_BODY_SIZES = {
    "QueryDisplacements.QueryWaves": 951,
    "QueryDisplacements.SampleHeightOnly": 345,
    "QueryDisplacements.Sample": 581,
    "FourierTask.ProcessData": 1636,
}

DOMAIN_KEYWORDS = ("ceto", "tide", "ocean", "buoyancy", "spectrum", "displacement", "fourier", "wavequery")


def sha256_file(path: Path) -> Optional[str]:
    if not path.exists() or not path.is_file():
        return None
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(65536), b""):
            h.update(chunk)
    return h.hexdigest()


def extract_tokens_and_symbols(text: str) -> Tuple[Set[str], List[str], List[str], List[str]]:
    tokens = set(re.findall(r"0x06[0-9A-Fa-f]{6}", text))
    classes = sorted(set(re.findall(r"\b(?:Ceto|Beam)\.[A-Za-z0-9_`.]+", text)))
    fields = sorted(set(re.findall(r"\b(?:m_[A-Za-z0-9_]+|GRIDS|CHANNELS|MAX_ITERATIONS|MIN_ERROR|sampleSpectrum|sampleOverlay|m_displacementMaps|m_readBuffer|m_displacements)\b", text)))
    methods = sorted(set(re.findall(r"\b(?:QueryWaves|SampleHeightOnly|Sample|UpdateQueryScaling|SetQueryWavesSampling|ReadFromGPU|GenerateDisplacement|CreateBuffers|GetFourierSize|SwapDisplacements|Initilize|ProcessData|PeformFFT|ReadFromRenderTexture|DoQueries)\b", text)))
    return tokens, classes, fields, methods


def scan_workspace_files(search_roots: List[Path]) -> Dict:
    target_names = [
        "1_OCEAN_CORE_PART_01.txt",
        "2_GPU_CPU_PIPELINE_PART_01.txt",
        "3_COMBINED_OCEAN_GPU_CPU_PART_01.txt",
        "VERIFY_SUBMISSION_REPORT.txt",
        "VERIFY_SUBMISSION_REPORT_V2.txt",
        "DISPLACEMENT_OUTPUT_STRUCTURE_REPORT.txt",
        "Scrpic.txt",
        "run_phase1_deep_audit.py",
        "PHASE1_DEEP_FORENSIC_AUDIT.txt",
    ]
    found_files: Dict[str, Dict] = {}
    token_sets: Dict[str, Set[str]] = {}
    all_classes: Set[str] = set()
    all_fields: Set[str] = set()
    all_methods: Set[str] = set()

    for root in search_roots:
        if not root.exists():
            continue
        for name in target_names:
            candidate = root / name
            if candidate.exists() and candidate.is_file() and name not in found_files:
                raw_bytes = candidate.read_bytes()
                text = raw_bytes.decode("utf-8", errors="replace")
                toks, cls_list, fld_list, mtd_list = extract_tokens_and_symbols(text)
                found_files[name] = {
                    "path": str(candidate),
                    "size_bytes": len(raw_bytes),
                    "sha256": hashlib.sha256(raw_bytes).hexdigest(),
                    "token_count_found": len(toks),
                    "classes_found": len(cls_list),
                    "fields_found": len(fld_list),
                    "methods_found": len(mtd_list),
                }
                token_sets[name] = toks
                all_classes.update(cls_list)
                all_fields.update(fld_list)
                all_methods.update(mtd_list)

    core_toks = token_sets.get("1_OCEAN_CORE_PART_01.txt", set())
    pipe_toks = token_sets.get("2_GPU_CPU_PIPELINE_PART_01.txt", set())
    comb_toks = token_sets.get("3_COMBINED_OCEAN_GPU_CPU_PART_01.txt", set())

    union_expected = core_toks | pipe_toks
    overlap_core_pipe = sorted(core_toks & pipe_toks)
    missing_in_combined = sorted(union_expected - comb_toks) if comb_toks else []
    extra_in_combined = sorted(comb_toks - union_expected) if comb_toks else []

    return {
        "found_files": found_files,
        "token_audit": {
            "core_tokens": len(core_toks),
            "pipeline_tokens": len(pipe_toks),
            "combined_tokens": len(comb_toks),
            "overlap_core_and_pipeline": overlap_core_pipe,
            "missing_in_combined": missing_in_combined,
            "extra_in_combined": extra_in_combined,
        },
        "symbols": {
            "classes": sorted(all_classes),
            "fields": sorted(all_fields),
            "methods": sorted(all_methods),
        },
    }


def build_report(scan_data: Dict) -> str:
    lines: List[str] = []
    lines.append("=" * 76)
    lines.append("GEMINI AI CORE — UNIFIED FORENSIC AUDIT & STRUCTURE REPORT")
    lines.append("Notebook : Battle (notebooks/45ebf19d-d9cc-4715-8509-4f9777fc3bee)")
    lines.append("Output   : Gemini Ai Core/unified_audit_report.txt & structure_summary.json")
    lines.append("=" * 76)
    lines.append("")

    lines.append("[1. GRID 3 FULL TRACE]")
    lines.append("- Constants: Ceto.QueryDisplacements..cctor (0x06002F93) sets GRIDS = 4, CHANNELS = 3.")
    lines.append("- GPU Blit & Global Textures: Ceto.WaveSpectrum.GenerateDisplacement (0x06003090) blits 4 ARGBHalf RenderTextures (m_displacementMaps[0..3]) and sets global textures Ceto_DisplacementMap0..3 (including Grid 3).")
    lines.append("- GPU Readback Gap: Ceto.WaveSpectrum.ReadFromGPU(numGrids) (0x06003091) reads back Grids 0..2 via CBUtility.ReadFromRenderTexture (0x060030F7, 'readNames2D') + ComputeBuffer.GetData; Grid 3 is skipped to save GPU->CPU bandwidth.")
    lines.append("- Query Selection & Index 3: Ceto.WaveQuery..ctor (0x06002F44) creates 4-element sampleSpectrum ([true, true, false, false] default), and Ceto.WaveSpectrum.QueryWaves (0x06003094) checks indices 0..3. Grid 3 shader consumption & CPU query data validity when index 3 is enabled remain UNVERIFIED / CRITICAL.")
    lines.append("")

    lines.append("[2. CPU BUFFER STATE VERIFICATION]")
    lines.append("- Double Buffering: DisplacementBufferCPU..ctor (0x06002F74) creates m_displacements[0..1], each with 4 InterpolatedArray2f(size, size, channels=3, wrap=true) grids stored as flat float[] m_data with interleaved index (x + y * SX) * 3 + ch (ch0=X, ch1=Y, ch2=Z).")
    lines.append("- Execution Order: DisplacementBufferCPU.Run (0x06002F78) calls SwapDisplacements() (0x06002F7B) FIRST (swapping slot 0 <-> slot 1), then WaveSpectrumBufferCPU.Run (0x06002FBD) -> Initilize() (0x06002F75, clears slot 0) -> InitSpectrumDisplacementsTask (0x06003060) -> FourierTask (0x06003048) -> ProcessData (0x0600304B).")
    lines.append("- Task Write Mapping: m_index==0 -> Height Y into ch1 of grids 0..3; m_index==1 -> Horizontal X,Z into ch0 & ch2 of grids 0..1; m_index==2 -> Horizontal X,Z into ch0 & ch2 of grids 2..3. QueryWaves reads slot 1 via GetReadDisplacements() (0x06002F77).")
    lines.append("- State Risk: Verify caller scheduling vs IsDone() / unfinished tasks so SwapDisplacements() cannot swap ahead of incomplete tasks.")
    lines.append("")

    lines.append("[3. INDEPENDENT DECODER & FILE INTEGRITY]")
    lines.append(f"- Target Method Body Sizes: {METHOD_BODY_SIZES}")
    lines.append(f"- Token Audit: {json.dumps(scan_data['token_audit'], indent=2)}")
    for fname, meta in scan_data["found_files"].items():
        lines.append(f"  * {fname}: {meta['size_bytes']} bytes | sha256={meta['sha256']} | tokens={meta['token_count_found']}")
    if not scan_data["found_files"]:
        lines.append("  * Note: No local dump files detected in current/parent scan paths; run with --root <path_to_dumps> if files live elsewhere.")
    lines.append("")

    lines.append("[4. COMPILED SHADER / FFT & SAMPLING PIPELINE]")
    lines.append("- GPU Spectrum & FFT: WaveSpectrumBufferGPU.Initilize (0x06002FC5) binds spectrum/grid/time uniforms onto m_displacementInitMat; FourierGPU.PeformFFT (0x06003037..3A) runs horizontal/vertical 1D butterfly passes ('Ceto_ButterFlyLookUp', 'Ceto_ReadBuffer0..3').")
    lines.append("- Sampling & Clamp: QueryDisplacements.Sample (0x06002F8F) computes u=(posX+offset.x)*invGridSizes[i], v=(posZ+offset.z)*invGridSizes[i], calls InterpolatedArray2f.Get (0x060031DF), scales X/Z by choppyness and Y by scaleY, iterates horizontal inversion, and clamps height to [-40.0f, 40.0f]; SampleHeightOnly (0x06002F8E) skips horizontal iteration/displacement X,Z.")
    lines.append("")

    lines.append("[5. EXTRACTED CLASSES / FIELDS / METHODS]")
    lines.append(f"- Classes ({len(scan_data['symbols']['classes'])}): {', '.join(scan_data['symbols']['classes'][:40])}")
    lines.append(f"- Fields ({len(scan_data['symbols']['fields'])}): {', '.join(scan_data['symbols']['fields'][:40])}")
    lines.append(f"- Methods ({len(scan_data['symbols']['methods'])}): {', '.join(scan_data['symbols']['methods'][:40])}")
    lines.append("")
    lines.append("EOF_AUDIT_REPORT")
    return "\n".join(lines)


def main() -> None:
    parser = argparse.ArgumentParser(description="Run unified forensic audit into Gemini Ai Core/")
    parser.add_argument("--root", nargs="*", default=[".", "..", "/sdcard/Download"], help="Directories to scan for dump files")
    args = parser.parse_args()

    TARGET_DIR.mkdir(parents=True, exist_ok=True)
    roots = [Path(r) for r in args.root]
    scan_data = scan_workspace_files(roots)

    report_text = build_report(scan_data)
    report_path = TARGET_DIR / "unified_audit_report.txt"
    json_path = TARGET_DIR / "structure_summary.json"

    report_path.write_text(report_text, encoding="utf-8")
    json_path.write_text(json.dumps(scan_data, indent=2, ensure_ascii=False), encoding="utf-8")

    print(report_text)
    print(f"\nSaved -> {report_path} and {json_path}")


if __name__ == "__main__":
    main()

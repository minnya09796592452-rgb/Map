#!/usr/bin/env python3
from pathlib import Path
import hashlib
import json

BASE_DIR = Path("/storage/emulated/0/Download/Stranded-Deep-Original")
DATA_DIR = BASE_DIR / "Stranded Deep/Stranded_Deep_Data"
OUT_DIR = BASE_DIR / "Gemini Ai Core"

CHECK_FILES = [
    "DISPLACEMENT_GENERATION_PIPELINE_FINAL.txt",
    "key_ocean_details.json",
    "key_ocean_v3.json",
    "stranded_deep_ocean_evidence_full.txt",
    "stranded_deep_ocean_full_evidence_v2.txt",
    "1_OCEAN_CORE_PART_01.txt",
    "2_GPU_CPU_PIPELINE_PART_01.txt",
    "3_COMBINED_OCEAN_GPU_CPU_PART_01.txt",
]

def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    summary = {
        "base_dir": str(BASE_DIR),
        "data_dir": str(DATA_DIR),
        "data_dir_exists": DATA_DIR.exists(),
        "files": {},
    }
    for name in CHECK_FILES:
        p = BASE_DIR / name
        if p.exists() and p.is_file():
            data = p.read_bytes()
            summary["files"][name] = {
                "path": str(p),
                "size_bytes": len(data),
                "sha256": hashlib.sha256(data).hexdigest(),
            }
        else:
            summary["files"][name] = {"exists": False}

    out_path = OUT_DIR / "path_audit_manifest.json"
    out_path.write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(json.dumps(summary, indent=2))
    print(f"Saved -> {out_path}")

if __name__ == "__main__":
    main()

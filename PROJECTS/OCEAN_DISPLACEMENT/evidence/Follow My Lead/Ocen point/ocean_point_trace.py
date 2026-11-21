import os
import sys
import re
import json
import hashlib
from pathlib import Path

PYTHON = "/root/.venv_mission3/bin/python"

ROOT = Path("/storage/emulated/0/Download/Stranded-Deep-Original")
STORAGE = Path("/storage/emulated/0")

KEYWORDS = (
    "ocean", "water", "sea", "ceto",
    "level", "height", "position",
    "transform", "origin", "plane"
)

EXTENSIONS = {
    ".cs", ".json", ".xml", ".txt", ".yaml", ".yml",
    ".asset", ".prefab", ".unity", ".mat", ".shader",
    ".bytes", ".cfg", ".ini"
}

OUTPUT = Path.cwd() / "ocean_point_trace_report.txt"

def sha256(path):
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()

def classify(path):
    name = path.name.lower()
    matches = [k for k in KEYWORDS if k in name]
    return matches

def scan_file(path, output):
    try:
        if path.suffix.lower() not in EXTENSIONS:
            return

        size = path.stat().st_size
        if size > 32 * 1024 * 1024:
            return

        raw = path.read_bytes()
        text = raw.decode("utf-8", errors="ignore")

        hits = []
        for number, line in enumerate(text.splitlines(), 1):
            low = line.lower()
            if any(k in low for k in KEYWORDS):
                hits.append((number, line.strip()[:500]))

        if not hits and not classify(path):
            return

        output.write("\n" + "=" * 90 + "\n")
        output.write(f"FILE: {path}\n")
        output.write(f"SIZE: {size}\n")
        output.write(f"SHA256: {hashlib.sha256(raw).hexdigest()}\n")
        output.write(f"NAME MATCHES: {classify(path)}\n")

        for number, line in hits:
            output.write(f"[LINE {number}] {line}\n")

    except (PermissionError, OSError, ValueError) as exc:
        output.write(f"\n[READ ERROR] {path}: {exc}\n")

def walk(root, output, seen):
    if not root.exists():
        output.write(f"\n[NOT FOUND] {root}\n")
        return

    output.write(f"\n\n### SCANNING ROOT: {root}\n")

    for current, dirs, files in os.walk(root):
        dirs[:] = [
            d for d in dirs
            if d.lower() not in {
                "cache", "temp", "tmp", ".git",
                "node_modules", "__pycache__"
            }
        ]

        for filename in files:
            path = Path(current) / filename

            try:
                resolved = str(path.resolve())
                if resolved in seen:
                    continue
                seen.add(resolved)
                scan_file(path, output)
            except OSError:
                continue

def main():
    if not ROOT.exists():
        print("ERROR: Original game folder not found:")
        print(ROOT)
        sys.exit(1)

    seen = set()

    with OUTPUT.open("w", encoding="utf-8") as out:
        out.write("OCEAN POINT TRACE REPORT\n")
        out.write("MODE: READ ONLY\n")
        out.write("TARGET: ORIGINAL OCEAN POINT ONLY\n")
        out.write(f"PYTHON: {sys.executable}\n")
        out.write(f"ORIGINAL GAME: {ROOT}\n")
        out.write(f"STORAGE ROOT: {STORAGE}\n")

        # Original game first: primary evidence
        walk(ROOT, out, seen)

        # Existing extracted research: secondary evidence.
        # Avoid rescanning the original game folder.
        for candidate in (
            STORAGE / "Download",
            STORAGE / "Map",
            STORAGE / "Map Check",
        ):
            if candidate.exists() and candidate != ROOT:
                walk(candidate, out, seen)

    print("SCAN COMPLETE")
    print("Report:", OUTPUT)
    print("Bytes:", OUTPUT.stat().st_size)
    print("Python:", sys.executable)

if __name__ == "__main__":
    main()

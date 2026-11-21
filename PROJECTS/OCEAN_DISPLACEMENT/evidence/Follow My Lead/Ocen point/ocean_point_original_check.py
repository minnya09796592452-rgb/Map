import re
from pathlib import Path

ROOT = Path("/storage/emulated/0/Download/Stranded-Deep-Original")
SOURCE = ROOT / "ocean_point_trace_report.txt"
OUTPUT = ROOT / "ocean_point_original_check.txt"

TERMS = (
    "ocean", "ceto", "water", "sea",
    "m_ocean", "oceanlevel", "ocean_level",
    "localposition", "position", "origin",
    "transform", "m_transform",
    "m_level", "level"
)

if not SOURCE.exists():
    raise SystemExit(f"REPORT NOT FOUND: {SOURCE}")

current_file = None
original_sections = []
current_lines = []

def save_section():
    if current_file and current_lines:
        original_sections.append((current_file, current_lines[:]))

with SOURCE.open("r", encoding="utf-8", errors="ignore") as f:
    for line in f:
        if line.startswith("FILE: "):
            save_section()
            current_file = line[6:].strip()
            current_lines = []
        elif current_file:
            current_lines.append(line.rstrip())

save_section()

matched = 0

with OUTPUT.open("w", encoding="utf-8") as out:
    out.write("ORIGINAL OCEAN POINT VERIFICATION\n")
    out.write("MODE: READ ONLY\n")
    out.write(f"ORIGINAL ROOT: {ROOT}\n")

    for filename, lines in original_sections:
        try:
            path = Path(filename)
            path.relative_to(ROOT)
        except ValueError:
            continue

        hits = [
            line for line in lines
            if line.startswith("[LINE ")
            and any(term in line.lower() for term in TERMS)
        ]

        if not hits:
            continue

        matched += 1
        out.write("\n" + "=" * 80 + "\n")
        out.write(f"FILE: {filename}\n")

        for line in hits:
            out.write(line + "\n")

    out.write("\n" + "=" * 80 + "\n")
    out.write(f"ORIGINAL FILES WITH MATCHES: {matched}\n")

print("CHECK COMPLETE")
print("Original matching files:", matched)
print("Report:", OUTPUT)
print("Bytes:", OUTPUT.stat().st_size)

import os, re
from pathlib import Path

UNITY_SRC = Path("/storage/emulated/0/Download/Stranded-Deep-Original/Stranded Deep").resolve()
OUT = Path("/storage/emulated/0/Download/CASTAWAY_DEPTHS_WORK/SKY_LIGHTING").resolve()

KEYS = (
    "sky", "sun", "cubemap", "panorama", "horizon",
    "zenith", "atmosphere", "cloud", "fog", "ambient",
    "light", "exposure", "tonemap", "bloom", "scatter",
    "rayleigh", "mie", "tod", "timeofday", "weather",
    "reflection", "probe", "equirect", "hdri", "exr"
)

def fast_scan():
    if not UNITY_SRC.exists():
        print(f"MISSING_DIR: {UNITY_SRC}")
        return

    all_files = []
    named_hits = []
    archives = []

    for root, dirs, files in os.walk(UNITY_SRC):
        dirs.sort()
        for fn in sorted(files):
            fp = Path(root) / fn
            rel = fp.relative_to(UNITY_SRC).as_posix()
            sz = fp.stat().st_size
            ext = fp.suffix.lower()
            low = rel.lower()
            all_files.append((rel, sz, ext))
            if any(k in low for k in KEYS):
                named_hits.append((rel, sz))
            if ext in (".assets", ".ress", ".resource", ".unity3d", ".bundle") or fn.startswith(("globalgamemanagers", "level", "sharedassets")):
                archives.append((rel, sz))

    # Fast targeted byte scan on globalgamemanagers & level0..level5 only (small metadata files)
    pat = re.compile(rb"[A-Za-z0-9_./-]{4,80}")
    meta_symbols = {}
    for rel, sz in archives:
        fn = Path(rel).name
        if fn in ("globalgamemanagers", "globalgamemanagers.assets") or re.fullmatch(r"level\d+", fn):
            fp = UNITY_SRC / rel
            raw = fp.read_bytes()
            syms = sorted({
                m.group(0).decode("ascii")
                for m in pat.finditer(raw)
                if any(k in m.group(0).lower().decode("ascii") for k in ("sky", "sun", "cubemap", "horizon", "atmosphere", "cloud", "fog", "tod", "weather", "light", "exposure"))
            })
            meta_symbols[rel] = (sz, syms[:80])

    rep = OUT / "UNITY_ONLY_SKY_LIGHTING.txt"
    with open(rep, "w", encoding="utf-8") as f:
        f.write("CASTAWAY-DEPTHS — ORIGINAL UNITY ONLY SKY/LIGHTING\n")
        f.write("=" * 60 + "\n")
        f.write(f"Unity Source (READ-ONLY): {UNITY_SRC}\n")
        f.write(f"Total Unity Files: {len(all_files)}\n")
        f.write(f"Sky/Light Named Files: {len(named_hits)}\n")
        f.write(f"Unity Data Archives: {len(archives)}\n\n")

        f.write("1. ALL UNITY FILES & ARCHIVES\n" + "-" * 45 + "\n")
        for rel, sz, ext in all_files[:120]:
            f.write(f"  {rel} ({sz} B)\n")

        f.write("\n2. SKY / LIGHTING NAMED FILES\n" + "-" * 45 + "\n")
        for rel, sz in named_hits:
            f.write(f"  {rel} ({sz} B)\n")

        f.write("\n3. METADATA SYMBOLS (globalgamemanagers & level*)\n" + "-" * 45 + "\n")
        for rel, (sz, syms) in sorted(meta_symbols.items()):
            f.write(f"  === {rel} ({sz} B) ===\n")
            f.write("    " + ", ".join(syms) + "\n")

    print(f"UNITY_SCAN_OK | TOTAL={len(all_files)} | ARCHIVES={len(archives)} | META_SCANNED={len(meta_symbols)}")

if __name__ == "__main__":
    fast_scan()

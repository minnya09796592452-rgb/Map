import os, json
from pathlib import Path

try:
    import UnityPy
except ImportError:
    print("NEED_UNITYPY")
    raise SystemExit(1)

BASE = Path(
    "/storage/emulated/0/Download/"
    "Stranded-Deep-Original/Stranded Deep"
).resolve()
OUT = (
    BASE / "CASTAWAY_UNITY_EXTRACT/SKY_LIGHTING"
)

KEYS = (
    "sky", "sun", "cubemap", "panorama",
    "horizon", "zenith", "atmosphere",
    "cloud", "fog", "ambient", "light",
    "exposure", "scatter", "rayleigh",
    "mie", "tod", "weather", "kode80",
    "skydome", "moon", "star", "sunshaft",
    "inscatter", "transmittance", "irradiance",
)

def hit(name):
    low = (name or "").lower()
    return any(k in low for k in KEYS)

def safe(s):
    out = "".join(
        c if c.isalnum() or c in "._-" else "_"
        for c in (s or "unnamed")
    )
    return out[:64] or "unnamed"

def run():
    sd = BASE / "Stranded_Deep_Data"
    targets = []
    for fn in (
        "globalgamemanagers",
        "globalgamemanagers.assets",
        "level0", "level1", "level2",
        "level3", "level4",
        "resources.assets",
        "sharedassets0.assets",
        "sharedassets1.assets",
        "sharedassets2.assets",
        "sharedassets3.assets",
        "sharedassets4.assets",
    ):
        p = sd / fn
        if p.exists():
            targets.append(p)
    b_car = sd / "Data/Bundles/missions/carrier.unity3d"
    if b_car.exists():
        targets.append(b_car)

    log = []
    counts = {
        "Tex": 0, "Cube": 0, "Mat": 0,
        "Mesh": 0, "Shd": 0, "Data": 0,
    }

    for fp in targets:
        rel = fp.relative_to(BASE).as_posix()
        print(f"SCANNING: {rel}")
        env = UnityPy.load(str(fp))
        for obj in env.objects:
            tp = obj.type.name
            if tp not in (
                "Texture2D", "Cubemap",
                "Material", "Mesh", "Shader",
                "MonoBehaviour", "Light",
                "RenderSettings", "LightmapSettings",
            ):
                continue
            try:
                d = obj.read()
            except Exception:
                continue
            nm = getattr(d, "m_Name", "") or ""
            if tp in ("RenderSettings", "LightmapSettings"):
                is_rel = True
            elif tp == "MonoBehaviour":
                sc = ""
                try:
                    sp = d.m_Script.read()
                    sc = getattr(sp, "m_Name", "")
                except Exception:
                    pass
                nm = f"{nm}_{sc}".strip("_")
                is_rel = hit(nm)
            else:
                is_rel = hit(nm)

            if not is_rel:
                continue

            pid = obj.path_id
            tag = f"{safe(nm)}_{pid}"
            status = "SKIP"

            try:
                if tp == "Texture2D":
                    img = d.image
                    if img:
                        op = OUT / "Textures" / f"{tag}.png"
                        img.save(op)
                        counts["Tex"] += 1
                        status = f"OK:{op.name}"
                elif tp == "Cubemap":
                    img = d.image
                    if img:
                        op = OUT / "Cubemaps" / f"{tag}.png"
                        img.save(op)
                        counts["Cube"] += 1
                        status = f"OK:{op.name}"
                elif tp == "Mesh":
                    ex = d.export()
                    op = OUT / "Meshes" / f"{tag}.obj"
                    op.write_text(ex, encoding="utf-8")
                    counts["Mesh"] += 1
                    status = f"OK:{op.name}"
                elif tp == "Shader":
                    ex = d.export()
                    op = OUT / "Shaders" / f"{tag}.shader"
                    op.write_text(ex, encoding="utf-8")
                    counts["Shd"] += 1
                    status = f"OK:{op.name}"
                elif tp in (
                    "Material", "MonoBehaviour",
                    "Light", "RenderSettings",
                    "LightmapSettings",
                ):
                    tt = obj.read_typetree()
                    sub = (
                        "Materials" if tp == "Material"
                        else "Lighting_Data"
                    )
                    op = OUT / sub / f"{tag}.json"
                    op.write_text(
                        json.dumps(tt, indent=2, default=str),
                        encoding="utf-8",
                    )
                    if tp == "Material":
                        counts["Mat"] += 1
                    else:
                        counts["Data"] += 1
                    status = f"OK:{op.name}"
            except Exception as ex:
                status = f"ERR:{ex}"

            log.append(
                f"[{tp}] {nm} | {rel} | id={pid} -> {status}"
            )

    rep = OUT / "Reports/PHASE2_EXTRACTION_LOG.txt"
    rep.write_text(
        f"COUNTS={counts}\n" + "\n".join(log),
        encoding="utf-8",
    )
    print(f"PHASE2_DONE | {counts}")

if __name__ == "__main__":
    run()

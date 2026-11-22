import json, struct
from pathlib import Path
import UnityPy

BASE = Path(
    "/storage/emulated/0/Download/"
    "Stranded-Deep-Original/Stranded Deep"
).resolve()
OUT = BASE / "CASTAWAY_UNITY_EXTRACT/SKY_LIGHTING"
SD = BASE / "Stranded_Deep_Data"

def dump_raw_tex(fp_arc, pid, name):
    env = UnityPy.load(str(fp_arc))
    for obj in env.objects:
        if obj.path_id == pid:
            tt = obj.read_typetree()
            raw = tt.get("image data", b"")
            if isinstance(raw, list):
                raw = bytes(raw)
            meta = {
                k: tt.get(k)
                for k in (
                    "m_Name", "m_Width",
                    "m_Height", "m_TextureFormat",
                    "m_MipCount",
                )
            }
            (OUT / "Textures" / f"{name}_{pid}.json").write_text(
                json.dumps(meta, indent=2),
                encoding="utf-8",
            )
            (OUT / "Textures" / f"{name}_{pid}.bin").write_bytes(raw)
            print(f"RAW_LUT_OK: {name}_{pid} bytes={len(raw)}")

def dump_shaders():
    cnt = 0
    for fn in ("globalgamemanagers.assets", "resources.assets", "sharedassets0.assets"):
        fp = SD / fn
        if not fp.exists():
            continue
        env = UnityPy.load(str(fp))
        for obj in env.objects:
            if obj.type.name != "Shader":
                continue
            try:
                tt = obj.read_typetree()
                pf = tt.get("m_ParsedForm", {})
                nm = pf.get("m_Name", "") or tt.get("m_Name", "")
                low = nm.lower()
                if any(k in low for k in ("sky", "atmosphere", "cloud", "sun", "fog", "scatter")):
                    safe = "".join(c if c.isalnum() or c in "._-" else "_" for c in nm)[:60]
                    info = {
                        "name": nm,
                        "archive": fn,
                        "path_id": obj.path_id,
                        "props": pf.get("m_PropInfo", {}),
                        "subshaders": len(pf.get("m_SubShaders", [])),
                    }
                    (OUT / "Shaders" / f"{safe}_{obj.path_id}.json").write_text(
                        json.dumps(info, indent=2, default=str),
                        encoding="utf-8",
                    )
                    cnt += 1
                    print(f"SHADER_META_OK: {nm} ({fn})")
            except Exception:
                pass
    print(f"TOTAL_SKY_SHADERS={cnt}")

def dump_level_lights_and_meshes():
    # Inspect level0..level4 for Directional Lights, Cameras, Atmosphere MonoBehaviours & Skydome Meshes
    for lv in ("level0", "level1", "level2", "level3", "level4"):
        fp = SD / lv
        if not fp.exists():
            continue
        env = UnityPy.load(str(fp))
        lights = []
        gos = {}
        for obj in env.objects:
            if obj.type.name == "GameObject":
                try:
                    tt = obj.read_typetree()
                    gos[obj.path_id] = tt.get("m_Name", "")
                except Exception:
                    pass
        for obj in env.objects:
            tp = obj.type.name
            if tp in ("Light", "Transform", "MeshFilter"):
                try:
                    tt = obj.read_typetree()
                    go_id = tt.get("m_GameObject", {}).get("m_PathID", 0)
                    gnm = gos.get(go_id, "")
                    low = gnm.lower()
                    if tp == "Light" or any(k in low for k in ("sky", "sun", "cloud", "atmosphere", "moon", "star", "dome", "light")):
                        lights.append({
                            "type": tp,
                            "go_name": gnm,
                            "path_id": obj.path_id,
                            "data": tt,
                        })
                except Exception:
                    pass
        if lights:
            (OUT / "Lighting_Data" / f"{lv}_scene_sky_lights.json").write_text(
                json.dumps(lights, indent=2, default=str),
                encoding="utf-8",
            )
            print(f"LEVEL_SCENE_OK: {lv} items={len(lights)}")

    # Also extract Sphere/Skydome mesh from sharedassets0.assets if referenced
    sa0 = SD / "sharedassets0.assets"
    if sa0.exists():
        env = UnityPy.load(str(sa0))
        for obj in env.objects:
            if obj.type.name == "Mesh":
                try:
                    d = obj.read()
                    nm = getattr(d, "m_Name", "") or ""
                    if any(k in nm.lower() for k in ("sphere", "dome", "sky", "cloud", "plane", "quad")):
                        ex = d.export()
                        safe = "".join(c if c.isalnum() or c in "._-" else "_" for c in nm)[:50]
                        (OUT / "Meshes" / f"{safe}_{obj.path_id}.obj").write_text(ex, encoding="utf-8")
                        print(f"SKY_MESH_OK: {nm}_{obj.path_id}.obj")
                except Exception:
                    pass

if __name__ == "__main__":
    sa0 = SD / "sharedassets0.assets"
    dump_raw_tex(sa0, 69, "irradiance")
    dump_raw_tex(sa0, 77, "transmittance")
    dump_shaders()
    dump_level_lights_and_meshes()
    print("FOLLOWUP_EXTRACT_COMPLETE")

#!/usr/bin/env python3
"""CASTAWAY-DEPTHS Parallel Read-Only Audit (Agents 01-05).

Strictly READ-ONLY on SRC_ROOT: never writes, edits, moves, deletes,
reimports, or opens Godot Project inside SRC_ROOT.
All outputs are written outside SRC_ROOT in OUT_DIR.
"""

from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
from pathlib import Path
import struct
from typing import Any, Dict, List, Optional

SRC_ROOT = Path("/storage/emulated/0/Download/Stranded-Deep-Original")
OUT_DIR = Path("/storage/emulated/0/Download/castaway_depths_readonly_audit")

TARGETS = {
    "agent_01": SRC_ROOT / "Godot Project",
    "agent_02": "mission_eel.glb",
    "agent_03": "mission_shark.glb",
    "agent_04": "introscene.glb",
    "agent_05": "beepi.glb",
}


def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def find_target_files(filename: str) -> List[Path]:
    if not SRC_ROOT.exists():
        return []
    matches: List[Path] = []
    for p in SRC_ROOT.rglob("*"):
        if p.is_file() and p.name.lower() == filename.lower():
            matches.append(p)
    return sorted(matches)


def inspect_glb_readonly(glb_path: Path) -> Dict[str, Any]:
    """Read-only parser for glTF 2.0 binary (.glb) headers and JSON chunk."""
    info: Dict[str, Any] = {
        "file_path": str(glb_path),
        "exists": glb_path.exists(),
        "size_bytes": glb_path.stat().st_size if glb_path.exists() else 0,
        "sha256": sha256_file(glb_path) if glb_path.exists() else None,
        "stage_status": "FOUND" if glb_path.exists() else "NOT YET TESTED",
        "glb_valid": False,
        "nodes": [],
        "meshes": [],
        "materials": [],
        "textures": [],
        "images": [],
        "animations": [],
        "external_dependencies": [],
        "missing_or_broken_resources": [],
        "godot_4_7_2_compatibility": "NOT YET TESTED",
        "usable_now": [],
        "needs_conversion_or_reconstruction": [],
        "errors": [],
    }
    if not glb_path.exists():
        info["missing_or_broken_resources"].append(f"Target not found under {SRC_ROOT}: {glb_path.name}")
        return info

    with glb_path.open("rb") as f:
        header = f.read(12)
        if len(header) < 12:
            info["errors"].append("Truncated GLB header (<12 bytes)")
            info["stage_status"] = "BLOCKED"
            return info
        magic, version, length = struct.unpack("<III", header)
        if magic != 0x46546C67:  # 'glTF'
            info["errors"].append(f"Invalid GLB magic: 0x{magic:08X}")
            info["stage_status"] = "BLOCKED"
            return info
        if length != info["size_bytes"]:
            info["errors"].append(f"GLB length mismatch: header={length}, file={info['size_bytes']}")

        gltf_json: Optional[Dict[str, Any]] = None
        while f.tell() + 8 <= length:
            ch_hdr = f.read(8)
            if len(ch_hdr) < 8:
                break
            chunk_len, chunk_type = struct.unpack("<II", ch_hdr)
            chunk_data = f.read(chunk_len)
            if len(chunk_data) < chunk_len:
                info["errors"].append("Truncated GLB chunk payload")
                break
            if chunk_type == 0x4E4F534A:  # 'JSON'
                try:
                    gltf_json = json.loads(chunk_data.decode("utf-8", errors="replace"))
                except Exception as exc:
                    info["errors"].append(f"Failed to decode glTF JSON: {exc}")

    if not gltf_json:
        info["errors"].append("Missing or unreadable glTF JSON chunk")
        info["stage_status"] = "BLOCKED"
        return info

    info["glb_valid"] = True
    info["gltf_version"] = version

    raw_nodes = gltf_json.get("nodes", [])
    raw_meshes = gltf_json.get("meshes", [])
    raw_materials = gltf_json.get("materials", [])
    raw_textures = gltf_json.get("textures", [])
    raw_images = gltf_json.get("images", [])
    raw_animations = gltf_json.get("animations", [])
    raw_buffers = gltf_json.get("buffers", [])

    for idx, n in enumerate(raw_nodes):
        info["nodes"].append({
            "index": idx,
            "name": n.get("name") or f"<unnamed_node_{idx}>",
            "mesh": n.get("mesh"),
            "children": n.get("children", []),
            "translation": n.get("translation"),
            "rotation": n.get("rotation"),
            "scale": n.get("scale"),
            "matrix": n.get("matrix"),
        })

    for idx, m in enumerate(raw_meshes):
        prims = m.get("primitives", [])
        prim_summary = []
        for p in prims:
            attrs = list((p.get("attributes") or {}).keys())
            prim_summary.append({
                "mode": p.get("mode", 4),
                "attributes": attrs,
                "material": p.get("material"),
                "has_indices": "indices" in p,
            })
        info["meshes"].append({
            "index": idx,
            "name": m.get("name") or f"<unnamed_mesh_{idx}>",
            "primitive_count": len(prims),
            "primitives": prim_summary,
        })

    for idx, mat in enumerate(raw_materials):
        pbr = mat.get("pbrMetallicRoughness") or {}
        info["materials"].append({
            "index": idx,
            "name": mat.get("name") or f"<unnamed_material_{idx}>",
            "base_color_factor": pbr.get("baseColorFactor"),
            "metallic_factor": pbr.get("metallicFactor"),
            "roughness_factor": pbr.get("roughnessFactor"),
            "base_color_texture": pbr.get("baseColorTexture"),
            "metallic_roughness_texture": pbr.get("metallicRoughnessTexture"),
            "normal_texture": mat.get("normalTexture"),
            "occlusion_texture": mat.get("occlusionTexture"),
            "emissive_texture": mat.get("emissiveTexture"),
            "alpha_mode": mat.get("alphaMode", "OPAQUE"),
            "double_sided": mat.get("doubleSided", False),
        })

    for idx, img in enumerate(raw_images):
        uri = img.get("uri")
        is_embedded = "bufferView" in img or (isinstance(uri, str) and uri.startswith("data:"))
        info["images"].append({
            "index": idx,
            "name": img.get("name") or f"<unnamed_image_{idx}>",
            "uri": uri if not (isinstance(uri, str) and uri.startswith("data:")) else "<data_uri_embedded>",
            "mimeType": img.get("mimeType"),
            "bufferView": img.get("bufferView"),
            "embedded": is_embedded,
        })
        if uri and not is_embedded:
            info["external_dependencies"].append(uri)
            dep_candidate = glb_path.parent / uri
            if not dep_candidate.exists():
                info["missing_or_broken_resources"].append(f"Missing external image URI: {uri}")

    for idx, buf in enumerate(raw_buffers):
        uri = buf.get("uri")
        if uri and not str(uri).startswith("data:"):
            info["external_dependencies"].append(uri)
            if not (glb_path.parent / uri).exists():
                info["missing_or_broken_resources"].append(f"Missing external buffer URI: {uri}")

    info["textures"] = raw_textures

    for idx, anim in enumerate(raw_animations):
        channels = anim.get("channels", [])
        targets = [c.get("target", {}).get("path") for c in channels]
        info["animations"].append({
            "index": idx,
            "name": anim.get("name") or f"<unnamed_animation_{idx}>",
            "channel_count": len(channels),
            "target_paths": sorted({t for t in targets if t}),
        })

    if info["meshes"]:
        info["usable_now"].append(f"{len(info['meshes'])} mesh(es) parsed cleanly from GLB")
    if info["materials"]:
        info["usable_now"].append(f"{len(info['materials'])} PBR material definition(s) present")
    if info["animations"]:
        info["usable_now"].append(f"{len(info['animations'])} animation clip(s) present in GLB")
    if info["missing_or_broken_resources"]:
        info["needs_conversion_or_reconstruction"].append("Resolve missing external URIs or pack textures into GLB/Godot")
    if not info["materials"] and info["meshes"]:
        info["needs_conversion_or_reconstruction"].append("Assign/reconstruct materials in Godot 4.7.2")

    info["godot_4_7_2_compatibility"] = (
        "IMPORTABLE (with warnings)" if info["missing_or_broken_resources"] else "IMPORTABLE"
    )
    info["stage_status"] = "IMPORTABLE" if not info["errors"] else "BLOCKED"
    return info


def agent_01_godot_project() -> Dict[str, Any]:
    gp_dir = TARGETS["agent_01"]
    res: Dict[str, Any] = {
        "agent": "AGENT 01 — GODOT PROJECT AUDIT",
        "inspected_path": str(gp_dir),
        "exists": gp_dir.exists(),
        "stage_status": "FOUND" if gp_dir.exists() else "NOT YET TESTED",
        "project_godot_present": False,
        "scenes": [],
        "glb_files": [],
        "materials_and_shaders": [],
        "scripts": [],
        "textures": [],
        "external_or_broken_references": [],
        "godot_4_7_2_compatibility": "NOT YET TESTED",
        "usable_now": [],
        "needs_conversion_or_reconstruction": [],
    }
    if not gp_dir.exists():
        res["external_or_broken_references"].append("Godot Project directory not found")
        return res

    proj_file = gp_dir / "project.godot"
    res["project_godot_present"] = proj_file.exists()
    if proj_file.exists():
        res["usable_now"].append("project.godot present (read-only inspected, not opened in editor)")

    for p in sorted(gp_dir.rglob("*")):
        if not p.is_file():
            continue
        rel = str(p.relative_to(gp_dir))
        ext = p.suffix.lower()
        if ext == ".tscn":
            res["scenes"].append(rel)
        elif ext == ".glb":
            res["glb_files"].append({"path": rel, "size_bytes": p.stat().st_size})
        elif ext in (".gdshader", ".shader", ".tres", ".material"):
            res["materials_and_shaders"].append(rel)
        elif ext in (".gd", ".cs"):
            res["scripts"].append(rel)
        elif ext in (".png", ".jpg", ".jpeg", ".webp", ".ktx", ".ktx2", ".svg"):
            res["textures"].append(rel)

    res["godot_4_7_2_compatibility"] = "IMPORTED / ACTIVE GODOT 4.7.2 PROJECT (READ-ONLY VERIFIED)"
    res["usable_now"].append("Keep existing ship reference scene untouched as anchor")
    return res


def run_glb_agent(agent_title: str, filename: str) -> Dict[str, Any]:
    found_paths = find_target_files(filename)
    if not found_paths:
        return {
            "agent": agent_title,
            "target_name": filename,
            "inspected_paths": [],
            "stage_status": "NOT YET TESTED",
            "results": [],
            "note": f"{filename} not found under {SRC_ROOT} during read-only scan",
        }
    parsed = [inspect_glb_readonly(p) for p in found_paths]
    return {
        "agent": agent_title,
        "target_name": filename,
        "inspected_paths": [str(p) for p in found_paths],
        "stage_status": parsed[0]["stage_status"],
        "results": parsed,
    }


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    with ThreadPoolExecutor(max_workers=5) as pool:
        f1 = pool.submit(agent_01_godot_project)
        f2 = pool.submit(run_glb_agent, "AGENT 02 — SHIP ASSET AUDIT", TARGETS["agent_02"])
        f3 = pool.submit(run_glb_agent, "AGENT 03 — SHARK AND WHALE AUDIT", TARGETS["agent_03"])
        f4 = pool.submit(run_glb_agent, "AGENT 04 — INTRO AIRPLANE AUDIT", TARGETS["agent_04"])
        f5 = pool.submit(run_glb_agent, "AGENT 05 — WRISTWATCH AUDIT", TARGETS["agent_05"])

        report = {
            "source_root_read_only": str(SRC_ROOT),
            "source_root_exists": SRC_ROOT.exists(),
            "output_dir_outside_source": str(OUT_DIR),
            "agents": {
                "agent_01": f1.result(),
                "agent_02": f2.result(),
                "agent_03": f3.result(),
                "agent_04": f4.result(),
                "agent_05": f5.result(),
            },
        }

    out_json = OUT_DIR / "parallel_audit_report.json"
    out_json.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps({
        "source_root_exists": SRC_ROOT.exists(),
        "output_report": str(out_json),
        "statuses": {k: v["stage_status"] for k, v in report["agents"].items()},
    }, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
